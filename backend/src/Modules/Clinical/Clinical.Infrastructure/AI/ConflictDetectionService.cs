using Clinical.Application.Abstractions;
using Clinical.Domain.Entities;
using Clinical.Domain.Enums;
using Clinical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Detects data conflicts across clinical documents (FR-026, AIR-006).
/// Compares extracted data points of the same category, identifying contradictions
/// (e.g., conflicting medication dosages, inconsistent allergy records).
/// </summary>
public sealed class ConflictDetectionService : IConflictDetectionService
{
    private readonly ClinicalDbContext _dbContext;
    private readonly SeverityClassifier _severityClassifier;
    private readonly ILogger<ConflictDetectionService> _logger;

    public ConflictDetectionService(
        ClinicalDbContext dbContext,
        SeverityClassifier severityClassifier,
        ILogger<ConflictDetectionService> logger)
    {
        _dbContext = dbContext;
        _severityClassifier = severityClassifier;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DataConflict>> DetectConflictsAsync(
        Guid patientId,
        IReadOnlyList<ExtractedData> extractedData,
        CancellationToken cancellationToken = default)
    {
        var conflicts = new List<DataConflict>();

        // Group by Category + Key (normalized to lowercase for comparison)
        var groups = extractedData
            .GroupBy(e => (Category: e.Category, Key: NormalizeKey(e.Key)))
            .Where(g => g.Count() > 1); // Only groups with multiple entries can have conflicts

        foreach (var group in groups)
        {
            // Get distinct values within the group
            var distinctValues = group
                .GroupBy(e => NormalizeValue(e.Value))
                .ToList();

            // If all entries have the same value, no conflict
            if (distinctValues.Count <= 1)
            {
                continue;
            }

            // Multiple distinct values = conflict
            // Create conflicts between the first value and each subsequent distinct value
            var primaryEntry = distinctValues[0].First();
            
            for (int i = 1; i < distinctValues.Count; i++)
            {
                var conflictingEntry = distinctValues[i].First();
                
                var conflict = CreateConflict(
                    patientId,
                    group.Key.Category,
                    group.Key.Key,
                    primaryEntry,
                    conflictingEntry);

                conflicts.Add(conflict);
            }
        }

        _logger.LogInformation(
            "Detected {ConflictCount} conflicts for patient {PatientId} from {EntryCount} extracted entries",
            conflicts.Count, patientId, extractedData.Count);

        return Task.FromResult<IReadOnlyList<DataConflict>>(conflicts);
    }

    /// <inheritdoc />
    public async Task<int> PersistConflictsAsync(
        IReadOnlyList<DataConflict> conflicts,
        CancellationToken cancellationToken = default)
    {
        if (conflicts.Count == 0)
        {
            return 0;
        }

        var patientId = conflicts[0].PatientId;

        // Get existing open conflicts for this patient to avoid duplicates
        var existingConflicts = await _dbContext.DataConflicts
            .AsNoTracking()
            .Where(c => c.PatientId == patientId && c.ResolutionStatus == ConflictResolutionStatus.Open)
            .ToListAsync(cancellationToken);

        var newConflicts = new List<DataConflict>();

        foreach (var conflict in conflicts)
        {
            // Check if this conflict already exists (same field, same values)
            var exists = existingConflicts.Any(e =>
                e.FieldName == conflict.FieldName &&
                e.SourceValue == conflict.SourceValue &&
                e.ConflictingValue == conflict.ConflictingValue);

            if (!exists)
            {
                newConflicts.Add(conflict);
            }
        }

        if (newConflicts.Count > 0)
        {
            _dbContext.DataConflicts.AddRange(newConflicts);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Persisted {NewCount} new conflicts for patient {PatientId} ({SkippedCount} duplicates skipped)",
                newConflicts.Count, patientId, conflicts.Count - newConflicts.Count);
        }

        return newConflicts.Count;
    }

    /// <summary>
    /// Creates a DataConflict entity from two conflicting extracted data entries.
    /// </summary>
    private DataConflict CreateConflict(
        Guid patientId,
        DataCategory category,
        string fieldKey,
        ExtractedData source,
        ExtractedData conflicting)
    {
        var severity = _severityClassifier.Classify(category);
        var isIntraDocument = source.DocumentId == conflicting.DocumentId;

        // Build field name with category context
        var fieldName = $"{category}: {fieldKey}";
        
        // Add "Same Source" indicator for intra-document conflicts
        if (isIntraDocument)
        {
            fieldName = $"[Same Source] {fieldName}";
        }

        return new DataConflict
        {
            PatientId = patientId,
            SourceDocumentId = source.DocumentId,
            ConflictingDocumentId = conflicting.DocumentId,
            FieldName = fieldName,
            SourceValue = source.Value,
            ConflictingValue = conflicting.Value,
            Severity = severity,
            ResolutionStatus = ConflictResolutionStatus.Open
        };
    }

    /// <summary>
    /// Normalizes a key for comparison (lowercase, trimmed).
    /// </summary>
    private static string NormalizeKey(string key)
    {
        return key.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes a value for comparison (lowercase, trimmed, whitespace collapsed).
    /// </summary>
    private static string NormalizeValue(string value)
    {
        // Trim, lowercase, and collapse multiple spaces
        var normalized = value.Trim().ToLowerInvariant();
        return System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");
    }
}
