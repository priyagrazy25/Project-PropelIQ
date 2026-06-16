using Clinical.Domain.Entities;
using SharedKernel.Domain;

namespace Clinical.Application.AI;

/// <summary>
/// Orchestrates NER extraction pipeline for clinical documents (AIR-001).
/// Processes OCR chunks → NER → confidence scoring → ExtractedData storage.
/// </summary>
public interface INerExtractionPipeline
{
    /// <summary>
    /// Processes document chunks through NER extraction pipeline.
    /// </summary>
    /// <param name="documentId">Document to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pipeline result with extracted entities or error.</returns>
    Task<Result<NerPipelineResult>> ProcessDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of NER pipeline processing.
/// </summary>
/// <param name="DocumentId">Processed document ID.</param>
/// <param name="EntitiesExtracted">Total entities extracted.</param>
/// <param name="LowConfidenceCount">Entities below 0.7 confidence.</param>
/// <param name="SchemaValidityPercent">Output schema validity percentage.</param>
/// <param name="ProcessingTimeMs">Total processing time in milliseconds.</param>
public sealed record NerPipelineResult(
    Guid DocumentId,
    int EntitiesExtracted,
    int LowConfidenceCount,
    float SchemaValidityPercent,
    long ProcessingTimeMs
);

/// <summary>
/// Extended NER result with clinical mapping (AIR-001, DR-005).
/// </summary>
public sealed record ClinicalNerResult(
    IReadOnlyList<ClinicalNerEntity> Entities,
    int TotalCount,
    int LowConfidenceCount,
    float SchemaValidityPercent,
    bool Success,
    string? Error = null
);

/// <summary>
/// Clinical NER entity with category/key/value mapping (DR-005).
/// </summary>
public sealed record ClinicalNerEntity(
    string Text,
    string Label,
    int Start,
    int End,
    float Confidence,
    string Category,
    string Key,
    string Value,
    string? Unit,
    bool IsLowConfidence,
    int? SourcePage
);
