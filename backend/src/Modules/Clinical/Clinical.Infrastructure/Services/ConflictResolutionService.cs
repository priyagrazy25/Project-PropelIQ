using Clinical.Application.Abstractions;
using Clinical.Domain.Enums;
using Clinical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Caching;
using SharedKernel.Domain;
using System.Text.Json;

namespace Clinical.Infrastructure.Services;

/// <summary>
/// Implements conflict resolution with audit trail (AC-2, AC-3, AC-4).
/// Supports accept-a, accept-b, or manual-override with optimistic concurrency.
/// Retention: account lifecycle + 7 years per DR-012.
/// </summary>
public sealed class ConflictResolutionService : IConflictResolutionService
{
    private readonly ClinicalDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ConflictResolutionService> _logger;

    private const string Patient360CachePrefix = "patient360:";

    public ConflictResolutionService(
        ClinicalDbContext dbContext,
        ICacheService cacheService,
        ILogger<ConflictResolutionService> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> GetOpenConflictCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.DataConflicts
            .CountAsync(c => c.ResolutionStatus == ConflictResolutionStatus.Open && !c.IsDeleted, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<ConflictDetailResponse>> GetConflictDetailAsync(
        Guid conflictId,
        CancellationToken cancellationToken = default)
    {
        var conflict = await _dbContext.DataConflicts
            .AsNoTracking()
            .Include(c => c.SourceDocument)
            .Include(c => c.ConflictingDocument)
            .FirstOrDefaultAsync(c => c.Id == conflictId, cancellationToken);

        if (conflict is null)
        {
            return Result<ConflictDetailResponse>.Failure("Conflict not found.");
        }

        // Get extracted data to retrieve confidence scores
        var sourceData = conflict.SourceDocumentId.HasValue
            ? await _dbContext.ExtractedData
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.DocumentId == conflict.SourceDocumentId &&
                    e.Value == conflict.SourceValue,
                    cancellationToken)
            : null;

        var conflictingData = conflict.ConflictingDocumentId.HasValue
            ? await _dbContext.ExtractedData
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.DocumentId == conflict.ConflictingDocumentId &&
                    e.Value == conflict.ConflictingValue,
                    cancellationToken)
            : null;

        var response = new ConflictDetailResponse(
            ConflictId: conflict.Id,
            PatientId: conflict.PatientId,
            Field: conflict.FieldName,
            Category: ExtractCategory(conflict.FieldName),
            SourceA: new ConflictSourceInfo(
                DocumentId: conflict.SourceDocumentId,
                DocumentName: conflict.SourceDocument?.FileName ?? "Unknown Source",
                Value: conflict.SourceValue,
                Confidence: sourceData?.ConfidenceScore ?? 0.8,
                ExtractedAt: sourceData?.CreatedAt ?? conflict.CreatedAt),
            SourceB: new ConflictSourceInfo(
                DocumentId: conflict.ConflictingDocumentId,
                DocumentName: conflict.ConflictingDocument?.FileName ?? "Unknown Source",
                Value: conflict.ConflictingValue,
                Confidence: conflictingData?.ConfidenceScore ?? 0.7,
                ExtractedAt: conflictingData?.CreatedAt ?? conflict.CreatedAt),
            Severity: conflict.Severity == ConflictSeverity.Critical ? "high" : "medium",
            ResolutionStatus: conflict.ResolutionStatus.ToString(),
            ResolvedValue: null, // Would need a ResolvedValue field on DataConflict
            ResolvedAt: conflict.ResolvedAt,
            ResolvedBy: null, // Would need to join with Users table
            ResolutionNotes: conflict.StaffNotes);

        return Result<ConflictDetailResponse>.Success(response);
    }

    /// <inheritdoc />
    public async Task<Result<ConflictResolutionResponse>> ResolveConflictAsync(
        ResolveConflictRequest request,
        Guid userId,
        string userName,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        // Load conflict with tracking for update
        var conflict = await _dbContext.DataConflicts
            .FirstOrDefaultAsync(c => c.Id == request.ConflictId, cancellationToken);

        if (conflict is null)
        {
            return Result<ConflictResolutionResponse>.Failure("Conflict not found.");
        }

        // Optimistic concurrency: check if already resolved
        if (conflict.ResolutionStatus != ConflictResolutionStatus.Open)
        {
            _logger.LogWarning(
                "Conflict {ConflictId} already resolved with status {Status}",
                request.ConflictId, conflict.ResolutionStatus);

            return Result<ConflictResolutionResponse>.Failure(
                "CONFLICT_ALREADY_RESOLVED: This conflict was already resolved by another user.");
        }

        // Determine resolved value based on action
        var resolvedValue = request.Action switch
        {
            ResolutionAction.AcceptA => conflict.SourceValue,
            ResolutionAction.AcceptB => conflict.ConflictingValue,
            ResolutionAction.ManualOverride => request.ManualValue
                ?? throw new ArgumentException("Manual value required for manual override."),
            _ => throw new ArgumentException($"Unknown resolution action: {request.Action}")
        };

        // Capture before state for audit
        var beforeState = new
        {
            conflict.Id,
            conflict.FieldName,
            conflict.SourceValue,
            conflict.ConflictingValue,
            conflict.ResolutionStatus,
            conflict.Severity
        };

        // Update conflict status
        conflict.ResolutionStatus = ConflictResolutionStatus.Resolved;
        conflict.ResolvedByUserId = userId;
        conflict.ResolvedAt = DateTime.UtcNow;
        conflict.StaffNotes = request.Notes;

        // Capture after state for audit
        var afterState = new
        {
            conflict.Id,
            conflict.FieldName,
            ResolvedValue = resolvedValue,
            ResolutionAction = request.Action.ToString(),
            conflict.ResolutionStatus,
            conflict.ResolvedByUserId,
            conflict.ResolvedAt
        };

        // Create audit log (AC-3: staff identity, action, before/after, timestamp)
        var auditLog = new AuditLog
        {
            ActorId = userId,
            ActorName = userName,
            Action = $"ConflictResolution:{request.Action}",
            Resource = "DataConflict",
            ResourceId = conflict.Id.ToString(),
            BeforeState = JsonSerializer.Serialize(beforeState),
            AfterState = JsonSerializer.Serialize(afterState),
            IpAddress = ipAddress,
            CorrelationId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow
        };

        _dbContext.AuditLogs.Add(auditLog);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(
                ex,
                "Concurrency conflict resolving DataConflict {ConflictId}",
                request.ConflictId);

            return Result<ConflictResolutionResponse>.Failure(
                "CONFLICT_ALREADY_RESOLVED: This conflict was modified by another user. Please refresh.");
        }

        // Invalidate patient 360 cache
        await _cacheService.RemoveAsync(
            $"{Patient360CachePrefix}{conflict.PatientId}",
            cancellationToken);

        _logger.LogInformation(
            "Conflict {ConflictId} resolved by user {UserId} with action {Action}. Value: {ResolvedValue}",
            conflict.Id, userId, request.Action, resolvedValue);

        return Result<ConflictResolutionResponse>.Success(
            new ConflictResolutionResponse(
                ConflictId: conflict.Id,
                ResolvedValue: resolvedValue,
                ResolvedAt: conflict.ResolvedAt!.Value,
                Message: $"Conflict resolved successfully with value: {resolvedValue}"));
    }

    /// <summary>
    /// Extracts category string from conflict field name.
    /// </summary>
    private static string ExtractCategory(string fieldName)
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
}
