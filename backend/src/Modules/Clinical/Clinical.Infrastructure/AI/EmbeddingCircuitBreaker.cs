using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Polly circuit breaker protecting embedding service (AIR-O02).
/// Opens after 3 consecutive failures within 60s. Recovery after 120s.
/// </summary>
public sealed class EmbeddingCircuitBreaker
{
    private readonly ResiliencePipeline<float[]?> _pipeline;
    private readonly ILogger<EmbeddingCircuitBreaker> _logger;

    public EmbeddingCircuitBreaker(ILogger<EmbeddingCircuitBreaker> logger)
    {
        _logger = logger;

        _pipeline = new ResiliencePipelineBuilder<float[]?>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<float[]?>
            {
                FailureRatio = 1.0, // 100% failure ratio triggers
                SamplingDuration = TimeSpan.FromSeconds(60),
                MinimumThroughput = 3, // 3 failures in 60s triggers
                BreakDuration = TimeSpan.FromSeconds(120), // 120s recovery
                OnOpened = args =>
                {
                    logger.LogWarning(
                        "Embedding circuit breaker OPENED after 3 failures. Recovery in 120s.");
                    return ValueTask.CompletedTask;
                },
                OnClosed = _ =>
                {
                    logger.LogInformation("Embedding circuit breaker CLOSED. Service recovered.");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = _ =>
                {
                    logger.LogInformation("Embedding circuit breaker HALF-OPEN. Testing recovery...");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<float[]?> ExecuteAsync(
        Func<CancellationToken, Task<float[]?>> action,
        CancellationToken cancellationToken = default)
    {
        return await _pipeline.ExecuteAsync(
            async ct => await action(ct),
            cancellationToken);
    }
}
