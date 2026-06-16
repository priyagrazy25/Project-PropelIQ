using Clinical.Application.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Token budget enforcement per AIR-O01.
/// Conversational intake: 4,096 tokens. Document extraction: 8,192 tokens.
/// </summary>
public sealed class TokenBudgetGuard
{
    private readonly OllamaOptions _options;
    private readonly ILogger<TokenBudgetGuard> _logger;

    public TokenBudgetGuard(IOptions<OllamaOptions> options, ILogger<TokenBudgetGuard> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public int GetBudget(AiRequestType requestType) => requestType switch
    {
        AiRequestType.ConversationalIntake => _options.ConversationalTokenBudget,
        AiRequestType.DocumentExtraction => _options.DocumentExtractionTokenBudget,
        AiRequestType.MedicalCoding => _options.MedicalCodingTokenBudget,
        _ => _options.ConversationalTokenBudget
    };

    public TokenBudgetResult Validate(string input, AiRequestType requestType)
    {
        var budget = GetBudget(requestType);
        var estimated = EstimateTokenCount(input);
        var withinBudget = estimated <= budget;

        if (!withinBudget)
        {
            _logger.LogWarning(
                "Token budget violation: {Estimated} estimated > {Budget} limit for {RequestType}.",
                estimated, budget, requestType);
        }

        return new TokenBudgetResult(estimated, budget, withinBudget);
    }

    /// <summary>Rough token estimation (~4 chars per token for English text).</summary>
    private static int EstimateTokenCount(string text) => (text.Length + 3) / 4;
}

public sealed record TokenBudgetResult(int EstimatedTokens, int Budget, bool WithinBudget);
