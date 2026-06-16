using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Host.Services;

/// <summary>
/// Tracks Mean Time Between Failures (MTBF) for core scheduling operations per NFR-018.
/// Alerts when MTBF drops below 720 hours threshold.
/// </summary>
public sealed class MtbfTracker
{
    private readonly ILogger<MtbfTracker> _logger;
    private readonly ConcurrentDictionary<string, OperationMetrics> _metrics = new();
    private static readonly TimeSpan MtbfThreshold = TimeSpan.FromHours(720);

    public MtbfTracker(ILogger<MtbfTracker> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Records a successful operation execution.
    /// </summary>
    public void RecordSuccess(string operationName)
    {
        var metrics = _metrics.GetOrAdd(operationName, _ => new OperationMetrics());
        metrics.RecordSuccess();
    }

    /// <summary>
    /// Records an operation failure. Triggers MTBF recalculation and alerting.
    /// </summary>
    public void RecordFailure(string operationName, Exception? exception = null)
    {
        var metrics = _metrics.GetOrAdd(operationName, _ => new OperationMetrics());
        metrics.RecordFailure();

        var mtbf = metrics.GetMtbf();

        _logger.LogWarning(
            "Operation '{Operation}' failed. MTBF: {Mtbf:F1} hours. Failures: {Failures}, Successes: {Successes}",
            operationName,
            mtbf.TotalHours,
            metrics.FailureCount,
            metrics.SuccessCount);

        if (mtbf < MtbfThreshold)
        {
            _logger.LogError(
                "MTBF ALERT: Operation '{Operation}' MTBF is {Mtbf:F1} hours, " +
                "below 720-hour threshold (NFR-018). Investigate stability issues.",
                operationName,
                mtbf.TotalHours);
        }
    }

    /// <summary>
    /// Gets MTBF statistics for all tracked operations.
    /// </summary>
    public IReadOnlyDictionary<string, MtbfStats> GetAllStats()
    {
        return _metrics.ToDictionary(
            kvp => kvp.Key,
            kvp => new MtbfStats(
                kvp.Value.SuccessCount,
                kvp.Value.FailureCount,
                kvp.Value.GetMtbf(),
                kvp.Value.TrackingStart));
    }

    /// <summary>
    /// Gets MTBF for a specific operation.
    /// </summary>
    public MtbfStats? GetStats(string operationName)
    {
        if (!_metrics.TryGetValue(operationName, out var metrics))
            return null;

        return new MtbfStats(
            metrics.SuccessCount,
            metrics.FailureCount,
            metrics.GetMtbf(),
            metrics.TrackingStart);
    }

    private sealed class OperationMetrics
    {
        private int _successCount;
        private int _failureCount;
        public DateTime TrackingStart { get; } = DateTime.UtcNow;

        public int SuccessCount => _successCount;
        public int FailureCount => _failureCount;

        public void RecordSuccess()
        {
            Interlocked.Increment(ref _successCount);
        }

        public void RecordFailure()
        {
            Interlocked.Increment(ref _failureCount);
        }

        /// <summary>
        /// MTBF = Total operational time / Number of failures.
        /// If no failures, returns time since tracking started.
        /// </summary>
        public TimeSpan GetMtbf()
        {
            var elapsed = DateTime.UtcNow - TrackingStart;

            if (_failureCount == 0)
                return elapsed; // No failures = MTBF equals total uptime

            return TimeSpan.FromTicks(elapsed.Ticks / _failureCount);
        }
    }
}

/// <summary>
/// MTBF statistics for a tracked operation.
/// </summary>
public sealed record MtbfStats(
    int SuccessCount,
    int FailureCount,
    TimeSpan Mtbf,
    DateTime TrackingStart);
