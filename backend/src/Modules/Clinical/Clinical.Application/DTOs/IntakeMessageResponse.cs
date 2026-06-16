namespace Clinical.Application.DTOs;

/// <summary>
/// Response DTO returned after processing a patient message during intake.
/// </summary>
public sealed record IntakeMessageResponse(
    Guid SessionId,
    string AiMessage,
    IReadOnlyList<ParsedFieldDto> ParsedFields,
    bool SuggestManualFallback,
    bool TokenBudgetExhausted);
