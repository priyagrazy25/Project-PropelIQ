using Clinical.Application.Abstractions;
using Clinical.Application.DTOs;
using Clinical.Domain.Entities;
using Clinical.Domain.Enums;
using Clinical.Infrastructure.AI;
using Clinical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Caching;
using SharedKernel.Domain;

namespace Clinical.Infrastructure.Services;

/// <summary>
/// Service for generating 360-degree patient views (SCR-016, AIR-002).
/// Aggregates extracted data with semantic de-duplication, conflict detection,
/// and caches in Redis (NFR-017).
/// </summary>
public sealed class PatientView360Service : IPatientView360Service
{
    private readonly ClinicalDbContext _dbContext;
    private readonly IPatientDemographicsService _patientDemographics;
    private readonly SemanticDeduplicator _deduplicator;
    private readonly IConflictDetectionService _conflictDetection;
    private readonly ICacheService _cacheService;
    private readonly ILogger<PatientView360Service> _logger;

    private const string CacheKeyPrefix = "patient360:";

    public PatientView360Service(
        ClinicalDbContext dbContext,
        IPatientDemographicsService patientDemographics,
        SemanticDeduplicator deduplicator,
        IConflictDetectionService conflictDetection,
        ICacheService cacheService,
        ILogger<PatientView360Service> logger)
    {
        _dbContext = dbContext;
        _patientDemographics = patientDemographics;
        _deduplicator = deduplicator;
        _conflictDetection = conflictDetection;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<PatientView360Response>> GetPatientView360Async(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{patientId}";

        // Try cache first (NFR-004: 3s response with cache)
        var cached = await _cacheService.GetAsync<PatientView360Response>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for patient 360 view: {PatientId}", patientId);
            return Result<PatientView360Response>.Success(cached);
        }

        // Get patient demographics
        var patient = await _patientDemographics.GetByIdAsync(patientId, cancellationToken);
        if (patient is null)
        {
            return Result<PatientView360Response>.Failure("Patient not found.");
        }

        // Get all extracted data for patient
        var extractedData = await _dbContext.ExtractedData
            .AsNoTracking()
            .Include(e => e.Document)
            .Where(e => e.PatientId == patientId)
            .ToListAsync(cancellationToken);

        // Detect conflicts across documents (FR-026, AIR-006)
        var detectedConflicts = await _conflictDetection.DetectConflictsAsync(
            patientId, extractedData, cancellationToken);

        // Persist any new conflicts
        if (detectedConflicts.Count > 0)
        {
            await _conflictDetection.PersistConflictsAsync(detectedConflicts, cancellationToken);
        }

        // Apply semantic de-duplication (AIR-002)
        var dedupedData = _deduplicator.Deduplicate(extractedData);

        // Get all open conflicts (including newly persisted ones)
        var conflicts = await _dbContext.DataConflicts
            .AsNoTracking()
            .Where(c => c.PatientId == patientId && c.ResolutionStatus == ConflictResolutionStatus.Open)
            .ToListAsync(cancellationToken);

        // Get document count
        var documentCount = await _dbContext.ClinicalDocuments
            .AsNoTracking()
            .CountAsync(d => d.PatientId == patientId, cancellationToken);

        // Map to response DTOs (insurance info comes from patient demographics)
        var response = BuildResponse(patient, dedupedData, conflicts, documentCount);

        // Cache with L3 tier (15-min TTL per AD-009)
        await _cacheService.SetAsync(cacheKey, response, CacheTier.L3, cancellationToken);

        _logger.LogInformation(
            "Generated 360 view for patient {PatientId}: {DataCount} entries, {ConflictCount} conflicts",
            patientId, dedupedData.Count, conflicts.Count);

        return Result<PatientView360Response>.Success(response);
    }

    /// <inheritdoc />
    public async Task InvalidateCacheAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{patientId}";
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
        _logger.LogDebug("Invalidated 360 view cache for patient {PatientId}", patientId);
    }

    /// <summary>
    /// Builds the 360 view response from domain entities.
    /// </summary>
    private static PatientView360Response BuildResponse(
        PatientDemographicsResult patient,
        IReadOnlyList<ExtractedData> extractedData,
        List<DataConflict> conflicts,
        int documentCount)
    {
        var demographics = new PatientDemographicsDto(
            PatientId: patient.PatientId,
            FullName: patient.FullName,
            DateOfBirth: patient.DateOfBirth?.ToString("MMMM d, yyyy") ?? "Unknown",
            Gender: "Not specified", // Would come from extended patient profile
            Mrn: $"PAT-{patient.PatientId.ToString()[..8].ToUpperInvariant()}",
            Phone: patient.ContactNumber,
            Email: patient.Email,
            Address: patient.Address,
            InsuranceProvider: patient.InsuranceProvider,
            MemberId: patient.InsurancePolicyNumber);

        // Build conflict lookup by document ID and field for linking to data points
        var conflictLookup = conflicts
            .SelectMany(c => new[]
            {
                (DocId: c.SourceDocumentId, Field: ExtractFieldKey(c.FieldName), ConflictId: c.Id),
                (DocId: c.ConflictingDocumentId, Field: ExtractFieldKey(c.FieldName), ConflictId: c.Id)
            })
            .Where(x => x.DocId.HasValue)
            .GroupBy(x => (x.DocId!.Value, x.Field))
            .ToDictionary(g => g.Key, g => g.First().ConflictId);

        var extractedDataDtos = extractedData.Select(e =>
        {
            // Check if this data point is involved in a conflict
            Guid? conflictId = null;
            var key = (e.DocumentId, ExtractFieldKey(e.Key));
            if (conflictLookup.TryGetValue(key, out var cId))
            {
                conflictId = cId;
            }

            return new ExtractedDataPointDto(
                Id: e.Id,
                Category: MapCategoryToString(e.Category),
                Field: e.Key,
                Value: e.Value,
                SourceDocument: e.Document?.FileName ?? "Unknown",
                Confidence: e.ConfidenceScore,
                ExtractedAt: e.CreatedAt,
                ConflictId: conflictId);
        }).ToList();

        var conflictDtos = conflicts.Select(c => new ConflictSummaryDto(
            ConflictId: c.Id,
            Field: c.FieldName,
            Category: ExtractCategoryFromFieldName(c.FieldName),
            Values: [c.SourceValue, c.ConflictingValue],
            Severity: c.Severity == ConflictSeverity.Critical ? "high" : "medium"
        )).ToList();

        return new PatientView360Response(
            Demographics: demographics,
            ExtractedData: extractedDataDtos,
            Conflicts: conflictDtos,
            DocumentCount: documentCount,
            LastUpdated: DateTime.UtcNow);
    }

    /// <summary>
    /// Extracts the field key from a conflict field name.
    /// Field name format: "[Same Source] Category: FieldKey" or "Category: FieldKey"
    /// </summary>
    private static string ExtractFieldKey(string fieldName)
    {
        // Remove "[Same Source] " prefix if present
        var name = fieldName.Replace("[Same Source] ", "");
        
        // Extract the part after the colon
        var colonIndex = name.IndexOf(':');
        if (colonIndex >= 0 && colonIndex < name.Length - 1)
        {
            return name[(colonIndex + 1)..].Trim().ToLowerInvariant();
        }
        
        return name.ToLowerInvariant();
    }

    /// <summary>
    /// Extracts category string from conflict field name for frontend display.
    /// </summary>
    private static string ExtractCategoryFromFieldName(string fieldName)
    {
        // Remove "[Same Source] " prefix if present
        var name = fieldName.Replace("[Same Source] ", "");
        
        // Extract the part before the colon (category name)
        var colonIndex = name.IndexOf(':');
        if (colonIndex > 0)
        {
            var category = name[..colonIndex].Trim();
            return category.ToLowerInvariant() switch
            {
                "medication" => "medication",
                "allergy" => "allergy",
                "diagnosis" => "diagnosis",
                "labresult" => "lab",
                "vitalsign" => "vital",
                "procedure" => "history",
                "familyhistory" => "history",
                "symptom" => "history",
                _ => "history"
            };
        }
        
        return "history";
    }

    /// <summary>
    /// Maps DataCategory enum to frontend-friendly string.
    /// </summary>
    private static string MapCategoryToString(DataCategory category) => category switch
    {
        DataCategory.Diagnosis => "diagnosis",
        DataCategory.Medication => "medication",
        DataCategory.Allergy => "allergy",
        DataCategory.Procedure => "history",
        DataCategory.LabResult => "lab",
        DataCategory.VitalSign => "vital",
        DataCategory.Symptom => "history",
        DataCategory.FamilyHistory => "history",
        _ => "history"
    };
}
