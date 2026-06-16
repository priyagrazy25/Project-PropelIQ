using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;
using StackExchange.Redis;

namespace SharedKernel.Audit;

/// <summary>
/// Audit logging service with Redis-backed retry queue (FR-031, DR-011).
/// Ensures eventual consistency for audit records even on primary write failure.
/// </summary>
public sealed class AuditService<TContext> : IAuditService where TContext : DbContext
{
    private readonly TContext _dbContext;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<AuditService<TContext>> _logger;
    private const string AuditRetryQueueKey = "audit:retry:queue";
    private const string AiAuditRetryQueueKey = "audit:ai:retry:queue";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public AuditService(
        TContext dbContext,
        ILogger<AuditService<TContext>> logger,
        IConnectionMultiplexer? redis = null)
    {
        _dbContext = dbContext;
        _redis = redis;
        _logger = logger;
    }

    public async Task LogActionAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            ActorId = entry.ActorId,
            ActorName = entry.ActorName,
            Action = entry.Action,
            Resource = entry.Resource,
            ResourceId = entry.ResourceId,
            BeforeState = entry.BeforeState != null ? JsonSerializer.Serialize(entry.BeforeState, JsonOptions) : null,
            AfterState = entry.AfterState != null ? JsonSerializer.Serialize(entry.AfterState, JsonOptions) : null,
            IpAddress = entry.IpAddress,
            CorrelationId = entry.CorrelationId,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            _dbContext.Set<AuditLog>().Add(auditLog);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Audit logged: {Action} on {Resource}/{ResourceId} by {Actor}",
                entry.Action, entry.Resource, entry.ResourceId, entry.ActorName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Audit write failed, queuing for retry: {Action} on {Resource}/{ResourceId}",
                entry.Action, entry.Resource, entry.ResourceId);

            await QueueForRetryAsync(auditLog, cancellationToken);
        }
    }

    public async Task LogAiInvocationAsync(AiInvocationEntry entry, CancellationToken cancellationToken = default)
    {
        var auditLog = new AiInvocationAuditLog
        {
            ActorId = entry.ActorId,
            ActorName = entry.ActorName,
            OperationType = entry.FunctionName,
            ResourceType = entry.ModelId,
            ResourceId = null,
            ModelVersion = entry.ModelVersion,
            InputTokenCount = entry.PromptTokens,
            OutputTokenCount = entry.CompletionTokens,
            ConfidenceScore = entry.ConfidenceScore,
            ProcessingDurationMs = entry.DurationMs,
            Success = entry.Success,
            ErrorMessage = entry.ErrorMessage,
            CorrelationId = entry.CorrelationId,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            _dbContext.Set<AiInvocationAuditLog>().Add(auditLog);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "AI audit logged: {Operation} on {ResourceType}, {InputTokens}/{OutputTokens} tokens, {DurationMs}ms",
                entry.FunctionName, entry.ModelId,
                entry.PromptTokens, entry.CompletionTokens, entry.DurationMs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "AI audit write failed, queuing for retry: {Operation} on {ResourceType}",
                entry.FunctionName, entry.ModelId);

            await QueueAiAuditForRetryAsync(auditLog, cancellationToken);
        }
    }

    public async Task<PaginatedAuditResponse> GetAuditLogsAsync(
        AuditLogFilter filter,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<AuditLog>().AsNoTracking();

        // Apply filters
        if (filter.StartDate.HasValue)
            query = query.Where(a => a.Timestamp >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(a => a.Timestamp <= filter.EndDate.Value);

        if (filter.ActorId.HasValue)
            query = query.Where(a => a.ActorId == filter.ActorId.Value);

        if (!string.IsNullOrWhiteSpace(filter.ActorName))
            query = query.Where(a => a.ActorName.Contains(filter.ActorName));

        if (!string.IsNullOrWhiteSpace(filter.Action))
            query = query.Where(a => a.Action == filter.Action);

        if (!string.IsNullOrWhiteSpace(filter.Resource))
            query = query.Where(a => a.Resource == filter.Resource);

        if (!string.IsNullOrWhiteSpace(filter.ResourceId))
            query = query.Where(a => a.ResourceId == filter.ResourceId);

        if (!string.IsNullOrWhiteSpace(filter.IpAddress))
            query = query.Where(a => a.IpAddress == filter.IpAddress);

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditEntry
            {
                ActorId = a.ActorId,
                ActorName = a.ActorName,
                Action = a.Action,
                Resource = a.Resource,
                ResourceId = a.ResourceId,
                IpAddress = a.IpAddress,
                CorrelationId = a.CorrelationId,
                Timestamp = a.Timestamp
            })
            .ToListAsync(cancellationToken);

        return new PaginatedAuditResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PaginatedAiAuditResponse> GetAiAuditLogsAsync(
        object filter,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<AiInvocationAuditLog>().AsNoTracking();

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AiInvocationEntry
            {
                ActorId = a.ActorId,
                ActorName = a.ActorName,
                FunctionName = a.OperationType,
                ModelId = a.ResourceType,
                ModelVersion = a.ModelVersion,
                PromptTokens = a.InputTokenCount,
                CompletionTokens = a.OutputTokenCount,
                ConfidenceScore = a.ConfidenceScore,
                DurationMs = (int)a.ProcessingDurationMs,
                Success = a.Success,
                ErrorMessage = a.ErrorMessage,
                CorrelationId = a.CorrelationId
            })
            .ToListAsync(cancellationToken);

        return new PaginatedAiAuditResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private async Task QueueForRetryAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        if (_redis == null)
        {
            _logger.LogError("Cannot queue audit for retry: Redis not available. Audit record may be lost.");
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(auditLog, JsonOptions);
            await db.ListRightPushAsync(AuditRetryQueueKey, json);
            _logger.LogInformation("Audit queued for retry: {Action} on {Resource}", auditLog.Action, auditLog.Resource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue audit for retry. Audit record may be lost.");
        }
    }

    private async Task QueueAiAuditForRetryAsync(AiInvocationAuditLog auditLog, CancellationToken cancellationToken)
    {
        if (_redis == null)
        {
            _logger.LogError("Cannot queue AI audit for retry: Redis not available. Audit record may be lost.");
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(auditLog, JsonOptions);
            await db.ListRightPushAsync(AiAuditRetryQueueKey, json);
            _logger.LogInformation("AI audit queued for retry: {Operation} on {ResourceType}", auditLog.OperationType, auditLog.ResourceType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue AI audit for retry. Audit record may be lost.");
        }
    }
}
