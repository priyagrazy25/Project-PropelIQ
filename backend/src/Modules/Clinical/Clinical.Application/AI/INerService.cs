namespace Clinical.Application.AI;

/// <summary>
/// Abstraction for biomedical Named Entity Recognition via scispaCy (AIR-001).
/// </summary>
public interface INerService
{
    /// <summary>
    /// Extract entities using basic NER (legacy endpoint).
    /// </summary>
    Task<NerResult> ExtractEntitiesAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract entities with full clinical mapping, confidence scoring, and schema validation.
    /// </summary>
    /// <param name="text">Text to extract entities from.</param>
    /// <param name="context">Optional surrounding context for better scoring.</param>
    /// <param name="sourcePage">Optional source page number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Clinical NER result with mapped entities.</returns>
    Task<ClinicalNerResult> ExtractClinicalEntitiesAsync(
        string text,
        string? context = null,
        int? sourcePage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if NER service is available.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

public sealed record NerResult(IReadOnlyList<NerEntity> Entities, bool Success, string? Error = null);

public sealed record NerEntity(string Text, string Label, int Start, int End, float Confidence);
