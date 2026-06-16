using SharedKernel.Domain;

namespace SharedKernel.Audit;

/// <summary>
/// Audit logging service for HIPAA-compliant PHI access tracking (FR-031, DR-011).
/// Provides immutable audit trail with before/after state snapshots.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs an action audit record asynchronously.
    /// On failure, queues to retry queue for eventual consistency.
    /// </summary>
    /// <param name="entry">Audit entry to log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogActionAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an AI model invocation audit record per AIR-S03.
    /// </summary>
    /// <param name="entry">AI invocation audit entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogAiInvocationAsync(AiInvocationEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit logs with filtering and pagination for SCR-025 viewer.
    /// </summary>
    /// <param name="filter">Filter criteria.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated audit log results.</returns>
    Task<PaginatedAuditResponse> GetAuditLogsAsync(AuditLogFilter filter, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves AI invocation audit logs with filtering.
    /// </summary>
    /// <param name="filter">Filter criteria.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated AI audit log results.</returns>
    Task<PaginatedAiAuditResponse> GetAiAuditLogsAsync(object filter, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
}

/// <summary>
/// Input model for creating an audit entry.
/// </summary>
public sealed record AuditEntry
{
    public Guid? ActorId { get; init; }
    public string ActorName { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Resource { get; init; } = string.Empty;
    public string? ResourceId { get; init; }
    public object? BeforeState { get; init; }
    public object? AfterState { get; init; }
    public string? IpAddress { get; init; }
    public string? CorrelationId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Input model for creating an AI invocation audit entry.
/// </summary>
public sealed record AiInvocationEntry
{
    public Guid? ActorId { get; init; }
    public string ActorName { get; init; } = string.Empty;
    public string ModelId { get; init; } = string.Empty;
    public string ModelVersion { get; init; } = string.Empty;
    public string FunctionName { get; init; } = string.Empty;
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public decimal? ConfidenceScore { get; init; }
    public int DurationMs { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Filter criteria for audit log queries.
/// </summary>
public sealed record AuditLogFilter
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public Guid? ActorId { get; init; }
    public string? ActorName { get; init; }
    public string? Action { get; init; }
    public string? Resource { get; init; }
    public string? ResourceId { get; init; }
    public string? IpAddress { get; init; }
}

/// <summary>
/// Paginated audit log response for SCR-025 viewer.
/// </summary>
public sealed record PaginatedAuditResponse
{
    public IReadOnlyList<AuditEntry> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

/// <summary>
/// Paginated AI audit log response.
/// </summary>
public sealed record PaginatedAiAuditResponse
{
    public IReadOnlyList<AiInvocationEntry> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
