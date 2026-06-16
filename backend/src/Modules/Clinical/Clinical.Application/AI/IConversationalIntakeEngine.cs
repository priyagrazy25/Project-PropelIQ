using Clinical.Application.DTOs;
using Clinical.Application.Models;

namespace Clinical.Application.AI;

/// <summary>
/// AI conversational intake engine abstraction (AIR-003, AIR-Q02, AIR-008).
/// Encapsulates prompt generation, response parsing, confidence scoring,
/// and low-confidence fallback logic for the medical intake flow.
/// </summary>
public interface IConversationalIntakeEngine
{
    /// <summary>
    /// Processes a patient message through the AI engine and returns
    /// the AI response with parsed structured fields and confidence scores.
    /// </summary>
    Task<ConversationalIntakeResult> ProcessMessageAsync(
        IntakeSessionState session,
        string patientMessage,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of processing a patient message through the conversational intake engine.
/// </summary>
public sealed record ConversationalIntakeResult(
    string AiMessage,
    IReadOnlyList<ParsedFieldDto> ParsedFields,
    double OverallConfidence,
    int TokensUsed,
    bool SuggestManualFallback,
    bool TokenBudgetExhausted,
    string? Error = null)
{
    public bool Success => Error is null;
}
