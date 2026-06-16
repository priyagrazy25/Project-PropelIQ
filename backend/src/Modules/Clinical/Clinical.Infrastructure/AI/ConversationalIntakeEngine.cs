using Clinical.Application.AI;
using Clinical.Application.DTOs;
using Clinical.Application.Models;
using Microsoft.Extensions.Logging;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Conversational intake engine using Semantic Kernel + Ollama (Phi-3-mini).
/// Implements AIR-003 (AI guided flow), AIR-Q02 (5s p95 latency),
/// AIR-008 (low-confidence fallback), and AIR-O01 (token budget).
/// </summary>
public sealed class ConversationalIntakeEngine : IConversationalIntakeEngine
{
    private readonly IAiInferenceService _aiService;
    private readonly StructuredFieldParser _fieldParser;
    private readonly ConfidenceScorer _confidenceScorer;
    private readonly TokenBudgetGuard _tokenBudgetGuard;
    private readonly ILogger<ConversationalIntakeEngine> _logger;

    private const int MaxConsecutiveLowConfidence = 3;

    public ConversationalIntakeEngine(
        IAiInferenceService aiService,
        StructuredFieldParser fieldParser,
        ConfidenceScorer confidenceScorer,
        TokenBudgetGuard tokenBudgetGuard,
        ILogger<ConversationalIntakeEngine> logger)
    {
        _aiService = aiService;
        _fieldParser = fieldParser;
        _confidenceScorer = confidenceScorer;
        _tokenBudgetGuard = tokenBudgetGuard;
        _logger = logger;
    }

    public async Task<ConversationalIntakeResult> ProcessMessageAsync(
        IntakeSessionState session,
        string patientMessage,
        CancellationToken cancellationToken = default)
    {
        // Build prompt from template
        var prompt = IntakePromptTemplate.Build(session, patientMessage);

        // Validate token budget before sending (AIR-O01)
        var budgetResult = _tokenBudgetGuard.Validate(prompt, AiRequestType.ConversationalIntake);
        if (!budgetResult.WithinBudget)
        {
            _logger.LogWarning(
                "Token budget exhausted for session {SessionId}. Estimated: {Estimated}, Budget: {Budget}.",
                session.SessionId, budgetResult.EstimatedTokens, budgetResult.Budget);

            // Build a summary prompt instead
            var summaryPrompt = IntakePromptTemplate.BuildSummaryPrompt(session);
            var summaryResult = await _aiService.GenerateAsync(
                summaryPrompt, AiRequestType.ConversationalIntake, cancellationToken);

            var summaryMessage = summaryResult.Success
                ? _fieldParser.Parse(summaryResult.Text).Message
                : "I've collected the information so far. Please review the fields below and make any corrections before submitting.";

            return new ConversationalIntakeResult(
                AiMessage: summaryMessage,
                ParsedFields: session.ParsedFields,
                OverallConfidence: _confidenceScorer.ComputeOverallConfidence(session.ParsedFields),
                TokensUsed: summaryResult.TokensUsed,
                SuggestManualFallback: false,
                TokenBudgetExhausted: true);
        }

        // Execute AI inference via circuit-breaker-protected service
        var aiResult = await _aiService.GenerateAsync(
            prompt, AiRequestType.ConversationalIntake, cancellationToken);

        if (!aiResult.Success)
        {
            _logger.LogWarning(
                "AI inference failed for session {SessionId}: {Error}",
                session.SessionId, aiResult.Error);

            return new ConversationalIntakeResult(
                AiMessage: string.Empty,
                ParsedFields: [],
                OverallConfidence: 0.0,
                TokensUsed: 0,
                SuggestManualFallback: false,
                TokenBudgetExhausted: false,
                Error: aiResult.Error);
        }

        // Parse AI response into structured fields
        var parseResult = _fieldParser.Parse(aiResult.Text);

        // Score confidence for new fields
        var isLowConfidence = _confidenceScorer.IsLowConfidence(parseResult.Fields);
        var consecutiveLow = session.ConsecutiveLowConfidenceCount;

        if (isLowConfidence)
        {
            consecutiveLow++;
        }
        else if (parseResult.Fields.Count > 0)
        {
            consecutiveLow = 0;
        }

        // Update session state with new consecutive count
        session.ConsecutiveLowConfidenceCount = consecutiveLow;

        // Compute overall confidence across all fields (existing + new)
        var allFields = MergeFields(session.ParsedFields, parseResult.Fields);
        var overallConfidence = _confidenceScorer.ComputeOverallConfidence(allFields);

        // AIR-008: 3 consecutive low-confidence → suggest manual form
        var suggestManual = consecutiveLow >= MaxConsecutiveLowConfidence;
        if (suggestManual)
        {
            _logger.LogInformation(
                "Session {SessionId}: {Count} consecutive low-confidence exchanges. Suggesting manual fallback.",
                session.SessionId, consecutiveLow);
        }

        return new ConversationalIntakeResult(
            AiMessage: parseResult.Message,
            ParsedFields: parseResult.Fields,
            OverallConfidence: overallConfidence,
            TokensUsed: aiResult.TokensUsed,
            SuggestManualFallback: suggestManual,
            TokenBudgetExhausted: false);
    }

    /// <summary>
    /// Merges new fields into existing fields list for overall confidence computation.
    /// New fields replace existing fields with the same name.
    /// </summary>
    private static List<ParsedFieldDto> MergeFields(
        List<ParsedFieldDto> existing,
        List<ParsedFieldDto> newFields)
    {
        var merged = new List<ParsedFieldDto>(existing);

        foreach (var field in newFields)
        {
            var existingIndex = merged.FindIndex(
                f => f.FieldName.Equals(field.FieldName, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                merged[existingIndex] = field;
            }
            else
            {
                merged.Add(field);
            }
        }

        return merged;
    }
}
