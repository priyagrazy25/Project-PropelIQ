using System.Collections.Concurrent;
using Clinical.Application.AI;
using Microsoft.Extensions.Logging;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Extraction job queue with SemaphoreSlim(2) concurrency limit (AIR-O04).
/// Manages sequential document processing with max 2 concurrent pipelines.
/// </summary>
public sealed class ExtractionJobQueue : IExtractionJobQueue, IDisposable
{
    private readonly ConcurrentDictionary<Guid, ExtractionJob> _jobs = new();
    private readonly PriorityQueue<Guid, (int Priority, DateTime EnqueuedAt)> _pendingQueue = new();
    private readonly SemaphoreSlim _queueSemaphore = new(0);
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly object _queueLock = new();
    private readonly ILogger<ExtractionJobQueue> _logger;

    private int _completedCount;
    private int _failedCount;
    private bool _disposed;

    private const int MaxConcurrency = 2;

    public ExtractionJobQueue(ILogger<ExtractionJobQueue> logger)
    {
        _logger = logger;
        _concurrencySemaphore = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);
    }

    public int PendingCount
    {
        get
        {
            lock (_queueLock)
            {
                return _pendingQueue.Count;
            }
        }
    }

    public int ProcessingCount => MaxConcurrency - _concurrencySemaphore.CurrentCount;

    public Task<Guid> EnqueueAsync(Guid documentId, int priority = 0)
    {
        var job = new ExtractionJob
        {
            JobId = Guid.NewGuid(),
            DocumentId = documentId,
            Priority = priority,
            EnqueuedAt = DateTime.UtcNow,
            Status = ExtractionJobStatus.Pending
        };

        _jobs[job.JobId] = job;

        lock (_queueLock)
        {
            // Priority queue: higher priority first, then earlier enqueue time
            _pendingQueue.Enqueue(job.JobId, (-priority, job.EnqueuedAt));
        }

        _queueSemaphore.Release();

        _logger.LogInformation(
            "Enqueued extraction job {JobId} for document {DocumentId} (priority: {Priority}, queue depth: {Depth})",
            job.JobId, documentId, priority, PendingCount);

        return Task.FromResult(job.JobId);
    }

    public async Task<ExtractionJob?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        // Wait for item in queue
        await _queueSemaphore.WaitAsync(cancellationToken);

        // Acquire concurrency slot (max 2 concurrent)
        await _concurrencySemaphore.WaitAsync(cancellationToken);

        Guid jobId;
        lock (_queueLock)
        {
            if (!_pendingQueue.TryDequeue(out jobId, out _))
            {
                _concurrencySemaphore.Release();
                return null;
            }
        }

        if (!_jobs.TryGetValue(jobId, out var job))
        {
            _concurrencySemaphore.Release();
            return null;
        }

        job.Status = ExtractionJobStatus.Processing;
        job.StartedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Dequeued extraction job {JobId} for document {DocumentId} (processing: {Processing}/{Max})",
            job.JobId, job.DocumentId, ProcessingCount, MaxConcurrency);

        return job;
    }

    public Task CompleteJobAsync(Guid jobId, bool success, string? error = null)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            _logger.LogWarning("Attempted to complete unknown job {JobId}", jobId);
            return Task.CompletedTask;
        }

        job.CompletedAt = DateTime.UtcNow;
        job.Status = success ? ExtractionJobStatus.Completed : ExtractionJobStatus.Failed;
        job.Error = error;

        if (success)
        {
            Interlocked.Increment(ref _completedCount);
            _logger.LogInformation(
                "Extraction job {JobId} completed successfully for document {DocumentId} (duration: {Duration}ms)",
                jobId, job.DocumentId, (job.CompletedAt - job.StartedAt)?.TotalMilliseconds ?? 0);
        }
        else
        {
            Interlocked.Increment(ref _failedCount);
            _logger.LogWarning(
                "Extraction job {JobId} failed for document {DocumentId}: {Error}",
                jobId, job.DocumentId, error);
        }

        // Release concurrency slot
        _concurrencySemaphore.Release();

        // Clean up completed jobs after a delay (keep recent ones for status queries)
        _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ =>
        {
            _jobs.TryRemove(jobId, out var _);
        });

        return Task.CompletedTask;
    }

    public Task<QueueStatus> GetStatusAsync()
    {
        var status = new QueueStatus(
            PendingCount: PendingCount,
            ProcessingCount: ProcessingCount,
            CompletedCount: _completedCount,
            FailedCount: _failedCount,
            MaxConcurrency: MaxConcurrency
        );

        return Task.FromResult(status);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _queueSemaphore.Dispose();
        _concurrencySemaphore.Dispose();
        _disposed = true;
    }
}
