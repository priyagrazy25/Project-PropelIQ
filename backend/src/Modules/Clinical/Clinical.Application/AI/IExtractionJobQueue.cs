namespace Clinical.Application.AI;

/// <summary>
/// Abstraction for extraction job queue with concurrency control (AIR-O04).
/// Manages sequential processing with max 2 concurrent pipelines.
/// </summary>
public interface IExtractionJobQueue
{
    /// <summary>
    /// Enqueues a document for extraction processing.
    /// </summary>
    /// <param name="documentId">Document to process.</param>
    /// <param name="priority">Job priority (higher = processed first).</param>
    /// <returns>Job ID for tracking.</returns>
    Task<Guid> EnqueueAsync(Guid documentId, int priority = 0);

    /// <summary>
    /// Dequeues the next job for processing (blocks if none available).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Next job to process or null if queue is empty.</returns>
    Task<ExtractionJob?> DequeueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a job as completed.
    /// </summary>
    /// <param name="jobId">Job ID to complete.</param>
    /// <param name="success">Whether job succeeded.</param>
    /// <param name="error">Error message if failed.</param>
    Task CompleteJobAsync(Guid jobId, bool success, string? error = null);

    /// <summary>
    /// Gets current queue status.
    /// </summary>
    Task<QueueStatus> GetStatusAsync();

    /// <summary>
    /// Gets count of pending jobs.
    /// </summary>
    int PendingCount { get; }

    /// <summary>
    /// Gets count of currently processing jobs.
    /// </summary>
    int ProcessingCount { get; }
}

/// <summary>
/// Extraction job for document processing.
/// </summary>
public sealed class ExtractionJob
{
    public Guid JobId { get; init; }
    public Guid DocumentId { get; init; }
    public int Priority { get; init; }
    public DateTime EnqueuedAt { get; init; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ExtractionJobStatus Status { get; set; } = ExtractionJobStatus.Pending;
    public string? Error { get; set; }
}

/// <summary>
/// Job status enumeration.
/// </summary>
public enum ExtractionJobStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

/// <summary>
/// Queue status summary.
/// </summary>
/// <param name="PendingCount">Jobs waiting to be processed.</param>
/// <param name="ProcessingCount">Jobs currently being processed.</param>
/// <param name="CompletedCount">Jobs completed (since last reset).</param>
/// <param name="FailedCount">Jobs failed (since last reset).</param>
/// <param name="MaxConcurrency">Maximum concurrent jobs allowed.</param>
public sealed record QueueStatus(
    int PendingCount,
    int ProcessingCount,
    int CompletedCount,
    int FailedCount,
    int MaxConcurrency
);
