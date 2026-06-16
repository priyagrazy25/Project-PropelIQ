namespace SharedKernel.Domain;

/// <summary>
/// Immutable append-only audit record per DR-011.
/// No UPDATE or DELETE operations permitted at the database level.
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? ActorId { get; init; }
    public string ActorName { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Resource { get; init; } = string.Empty;
    public string? ResourceId { get; init; }
    public string? BeforeState { get; init; }
    public string? AfterState { get; init; }
    public string? IpAddress { get; init; }
    public string? CorrelationId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if this record has been archived to cold storage.
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// UTC timestamp when the record was archived.
    /// </summary>
    public DateTime? ArchivedAt { get; set; }
}
