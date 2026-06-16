using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Polly circuit breaker protecting Ollama inference (AIR-O02).
/// Opens after 3 consecutive failures within 60s. Recovery after 120s.
/// </summary>
public sealed class AiCircuitBreaker
{
    private readonly ResiliencePipeline<string> _pipeline;
    private readonly ILogger<AiCircuitBreaker> _logger;

    public AiCircuitBreaker(ILogger<AiCircuitBreaker> logger)
    {
        _logger = logger;

        _pipeline = new ResiliencePipelineBuilder<string>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<string>
            {
                FailureRatio = 1.0,
                SamplingDuration = TimeSpan.FromSeconds(60),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(120),
                OnOpened = args =>
                {
                    logger.LogWarning(
                        "Ollama circuit breaker OPENED after {Failures} failures. Recovery in {BreakDuration}s.",
                        3, 120);
                    return ValueTask.CompletedTask;
                },
                OnClosed = _ =>
                {
                    logger.LogInformation("Ollama circuit breaker CLOSED. Service recovered.");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = _ =>
                {
                    logger.LogInformation("Ollama circuit breaker HALF-OPEN. Testing recovery...");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<string> ExecuteAsync(Func<CancellationToken, Task<string>> action, CancellationToken cancellationToken = default)
    {
        return await _pipeline.ExecuteAsync(
            async ct => await action(ct),
            cancellationToken);
    }
}
