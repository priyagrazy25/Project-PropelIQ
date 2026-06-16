namespace Clinical.Infrastructure.AI;

/// <summary>
/// Configuration for Ollama and Semantic Kernel integration (TR-007).
/// </summary>
public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string Endpoint { get; set; } = "http://localhost:11434";
    public string ModelId { get; set; } = "phi3:mini";
    public string? PreviousModelId { get; set; }
    public int ConversationalTokenBudget { get; set; } = 4096;
    public int DocumentExtractionTokenBudget { get; set; } = 8192;
    public int MedicalCodingTokenBudget { get; set; } = 4096;
}
