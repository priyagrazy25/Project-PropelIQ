using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Tracks processing metrics for extraction pipeline scalability monitoring (NFR-016).
/// Supports 10,000 documents/month target.
/// </summary>
public sealed class ExtractionMetricsTracker
{
    private readonly ConcurrentDictionary<string, MetricCounter> _counters = new();
    private readonly ILogger<ExtractionMetricsTracker> _logger;

    private DateTime _periodStart = DateTime.UtcNow.Date.AddDays(-DateTime.UtcNow.Day + 1); // Start of month

    public ExtractionMetricsTracker(ILogger<ExtractionMetricsTracker> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Records a document processed event.
    /// </summary>
    public void RecordDocumentProcessed(Guid documentId, long processingTimeMs, bool success)
    {
        ResetPeriodIfNeeded();

        var counter = _counters.GetOrAdd("documents_processed", _ => new MetricCounter());
        counter.Increment();

        if (success)
        {
            var successCounter = _counters.GetOrAdd("documents_success", _ => new MetricCounter());
            successCounter.Increment();
        }
        else
        {
            var failedCounter = _counters.GetOrAdd("documents_failed", _ => new MetricCounter());
            failedCounter.Increment();
        }

        var timeCounter = _counters.GetOrAdd("total_processing_time_ms", _ => new MetricCounter());
        timeCounter.Add(processingTimeMs);

        // Log warning if approaching monthly limit
        var total = GetCount("documents_processed");
        if (total == 8000)
        {
            _logger.LogWarning("Approaching monthly document limit: {Count}/10,000 processed", total);
        }
        else if (total == 10000)
        {
            _logger.LogWarning("Monthly document limit reached: {Count}/10,000", total);
        }
    }

    /// <summary>
    /// Records an embedding generated event.
    /// </summary>
    public void RecordEmbeddingGenerated(int tokensUsed)
    {
        ResetPeriodIfNeeded();

        var counter = _counters.GetOrAdd("embeddings_generated", _ => new MetricCounter());
        counter.Increment();

        var tokenCounter = _counters.GetOrAdd("total_tokens_used", _ => new MetricCounter());
        tokenCounter.Add(tokensUsed);
    }

    /// <summary>
    /// Records an OCR extraction event.
    /// </summary>
    public void RecordOcrExtraction(int chunksCreated, int pagesProcessed)
    {
        ResetPeriodIfNeeded();

        var chunkCounter = _counters.GetOrAdd("chunks_created", _ => new MetricCounter());
        chunkCounter.Add(chunksCreated);

        var pageCounter = _counters.GetOrAdd("pages_processed", _ => new MetricCounter());
        pageCounter.Add(pagesProcessed);
    }

    /// <summary>
    /// Records a NER extraction event.
    /// </summary>
    public void RecordNerExtraction(int entitiesExtracted, int lowConfidenceCount)
    {
        ResetPeriodIfNeeded();

        var entityCounter = _counters.GetOrAdd("entities_extracted", _ => new MetricCounter());
        entityCounter.Add(entitiesExtracted);

        var lowConfCounter = _counters.GetOrAdd("low_confidence_entities", _ => new MetricCounter());
        lowConfCounter.Add(lowConfidenceCount);
    }

    /// <summary>
    /// Records a circuit breaker event.
    /// </summary>
    public void RecordCircuitBreakerEvent(string service, bool opened)
    {
        var key = opened ? $"circuit_breaker_opened_{service}" : $"circuit_breaker_closed_{service}";
        var counter = _counters.GetOrAdd(key, _ => new MetricCounter());
        counter.Increment();
    }

    /// <summary>
    /// Gets current metric value.
    /// </summary>
    public long GetCount(string metric)
    {
        return _counters.TryGetValue(metric, out var counter) ? counter.Value : 0;
    }

    /// <summary>
    /// Gets metrics summary for monitoring.
    /// </summary>
    public MetricsSummary GetSummary()
    {
        ResetPeriodIfNeeded();

        var totalDocs = GetCount("documents_processed");
        var successDocs = GetCount("documents_success");
        var totalTime = GetCount("total_processing_time_ms");

        return new MetricsSummary(
            PeriodStart: _periodStart,
            DocumentsProcessed: totalDocs,
            DocumentsSuccess: successDocs,
            DocumentsFailed: GetCount("documents_failed"),
            SuccessRate: totalDocs > 0 ? (double)successDocs / totalDocs * 100 : 100.0,
            AvgProcessingTimeMs: totalDocs > 0 ? (double)totalTime / totalDocs : 0,
            EmbeddingsGenerated: GetCount("embeddings_generated"),
            TotalTokensUsed: GetCount("total_tokens_used"),
            ChunksCreated: GetCount("chunks_created"),
            PagesProcessed: GetCount("pages_processed"),
            EntitiesExtracted: GetCount("entities_extracted"),
            LowConfidenceEntities: GetCount("low_confidence_entities"),
            MonthlyCapacity: 10000,
            CapacityUsedPercent: totalDocs / 10000.0 * 100
        );
    }

    private void ResetPeriodIfNeeded()
    {
        var currentMonthStart = DateTime.UtcNow.Date.AddDays(-DateTime.UtcNow.Day + 1);

        if (currentMonthStart > _periodStart)
        {
            _logger.LogInformation("Resetting monthly metrics. Previous period: {Count} documents processed", GetCount("documents_processed"));
            _counters.Clear();
            _periodStart = currentMonthStart;
        }
    }

    private sealed class MetricCounter
    {
        private long _value;

        public long Value => Interlocked.Read(ref _value);

        public void Increment() => Interlocked.Increment(ref _value);

        public void Add(long amount) => Interlocked.Add(ref _value, amount);
    }
}

/// <summary>
/// Metrics summary for monitoring dashboard.
/// </summary>
public sealed record MetricsSummary(
    DateTime PeriodStart,
    long DocumentsProcessed,
    long DocumentsSuccess,
    long DocumentsFailed,
    double SuccessRate,
    double AvgProcessingTimeMs,
    long EmbeddingsGenerated,
    long TotalTokensUsed,
    long ChunksCreated,
    long PagesProcessed,
    long EntitiesExtracted,
    long LowConfidenceEntities,
    int MonthlyCapacity,
    double CapacityUsedPercent
);
