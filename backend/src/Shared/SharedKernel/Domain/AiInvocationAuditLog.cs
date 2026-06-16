namespace SharedKernel.Domain;

/// <summary>
/// AI model invocation audit record per AIR-S03.
/// Captures token counts, model version, confidence scores, and processing duration.
/// Append-only: no UPDATE or DELETE permitted at database level.
/// </summary>
public sealed class AiInvocationAuditLog
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// User who triggered the AI invocation (null for system/background jobs).
    /// </summary>
    public Guid? ActorId { get; init; }

    /// <summary>
    /// Actor display name.
    /// </summary>
    public string ActorName { get; init; } = string.Empty;

    /// <summary>
    /// AI operation type (e.g., "ICD10Mapping", "CPTMapping", "ConversationalIntake", "Embedding").
    /// </summary>
    public string OperationType { get; init; } = string.Empty;

    /// <summary>
    /// Target resource type (e.g., "ClinicalDocument", "EncounterNote").
    /// </summary>
    public string ResourceType { get; init; } = string.Empty;

    /// <summary>
    /// Target resource identifier.
    /// </summary>
    public string? ResourceId { get; init; }

    /// <summary>
    /// Model version identifier (e.g., "llama3.2:latest", "nonembed:latest").
    /// </summary>
    public string ModelVersion { get; init; } = string.Empty;

    /// <summary>
    /// Number of input tokens processed.
    /// </summary>
    public int InputTokenCount { get; init; }

    /// <summary>
    /// Number of output tokens generated.
    /// </summary>
    public int OutputTokenCount { get; init; }

    /// <summary>
    /// AI model confidence score (0.0 - 1.0), if applicable.
    /// </summary>
    public decimal? ConfidenceScore { get; init; }

    /// <summary>
    /// Processing duration in milliseconds.
    /// </summary>
    public long ProcessingDurationMs { get; init; }

    /// <summary>
    /// Whether the AI invocation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if the invocation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Request correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// UTC timestamp of the invocation.
    /// </summary>
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
