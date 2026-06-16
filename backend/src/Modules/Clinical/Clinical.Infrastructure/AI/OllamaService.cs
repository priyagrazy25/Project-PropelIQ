using System.Text.Json;
using Clinical.Application.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Polly.CircuitBreaker;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Ollama LLM inference service with token budget enforcement (AIR-O01) and circuit breaker (AIR-O02).
/// All inference is local — zero PHI transmission (AIR-S01).
/// </summary>
public sealed class OllamaService : IAiInferenceService
{
    private readonly Kernel _kernel;
    private readonly OllamaOptions _options;
    private readonly AiCircuitBreaker _circuitBreaker;
    private readonly ILogger<OllamaService> _logger;

    public OllamaService(
        Kernel kernel,
        IOptions<OllamaOptions> options,
        AiCircuitBreaker circuitBreaker,
        ILogger<OllamaService> logger)
    {
        _kernel = kernel;
        _options = options.Value;
        _circuitBreaker = circuitBreaker;
        _logger = logger;
    }

    public async Task<AiInferenceResult> GenerateAsync(
        string prompt,
        AiRequestType requestType,
        CancellationToken cancellationToken = default)
    {
        var tokenBudget = requestType == AiRequestType.ConversationalIntake
            ? _options.ConversationalTokenBudget
            : _options.DocumentExtractionTokenBudget;

        // Token budget enforcement (AIR-O01): estimate input tokens
        var estimatedInputTokens = EstimateTokenCount(prompt);
        if (estimatedInputTokens > tokenBudget)
        {
            _logger.LogWarning(
                "Token budget exceeded: estimated {Estimated} > budget {Budget} for {RequestType}.",
                estimatedInputTokens, tokenBudget, requestType);

            return new AiInferenceResult(
                string.Empty,
                estimatedInputTokens,
                Success: false,
                Error: $"Token budget exceeded: {estimatedInputTokens} estimated tokens > {tokenBudget} limit.");
        }

        try
        {
            var result = await _circuitBreaker.ExecuteAsync(async ct =>
            {
                var response = await _kernel.InvokePromptAsync(
                    prompt,
                    cancellationToken: ct);

                return response.GetValue<string>() ?? string.Empty;
            }, cancellationToken);

            var responseTokens = EstimateTokenCount(result);
            var totalTokens = estimatedInputTokens + responseTokens;

            return new AiInferenceResult(result, totalTokens, Success: true);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Ollama inference timed out for {RequestType}.", requestType);
            throw; // Propagate so caller can return 408
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "Ollama circuit breaker is open. AI features degraded.");
            return new AiInferenceResult(string.Empty, 0, Success: false, Error: "AI service temporarily unavailable (circuit breaker open).");
        }
        catch (Exception ex) when (ex.InnerException is OperationCanceledException or TaskCanceledException)
        {
            _logger.LogWarning("Ollama inference timed out for {RequestType}.", requestType);
            throw new OperationCanceledException("Ollama inference timed out.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama inference failed for {RequestType}.", requestType);
            return new AiInferenceResult(string.Empty, 0, Success: false, Error: ex.Message);
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await httpClient.GetAsync(
                $"{_options.Endpoint}/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Rough token estimation (~4 chars per token for English text).
    /// </summary>
    private static int EstimateTokenCount(string text) => (text.Length + 3) / 4;
}
