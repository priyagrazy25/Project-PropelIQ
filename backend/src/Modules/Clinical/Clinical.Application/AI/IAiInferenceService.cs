namespace Clinical.Application.AI;

/// <summary>
/// Abstraction for LLM inference via Ollama (AIR-S01: local inference only, no PHI transmission).
/// </summary>
public interface IAiInferenceService
{
    Task<AiInferenceResult> GenerateAsync(string prompt, AiRequestType requestType, CancellationToken cancellationToken = default);

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

public sealed record AiInferenceResult(string Text, int TokensUsed, bool Success, string? Error = null);

public enum AiRequestType
{
    ConversationalIntake,
    DocumentExtraction,
    MedicalCoding
}
