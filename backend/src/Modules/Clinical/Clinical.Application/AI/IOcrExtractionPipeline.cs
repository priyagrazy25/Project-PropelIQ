using Clinical.Domain.Entities;
using SharedKernel.Domain;

namespace Clinical.Application.AI;

/// <summary>
/// Orchestrates OCR extraction pipeline for clinical documents (AIR-001).
/// Processes PDF pages → OCR → PII redaction → chunking → storage.
/// </summary>
public interface IOcrExtractionPipeline
{
    /// <summary>
    /// Processes a clinical document through the full OCR pipeline.
    /// </summary>
    /// <param name="documentId">Document to process.</param>
    /// <param name="cancellationToken">Cancellation token (5-min timeout for ≤20 pages per NFR-003).</param>
    /// <returns>Pipeline result with extracted chunks or error.</returns>
    Task<Result<OcrPipelineResult>> ProcessDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of OCR pipeline processing.
/// </summary>
/// <param name="DocumentId">Processed document ID.</param>
/// <param name="ChunksCreated">Number of text chunks created.</param>
/// <param name="TotalPages">Total pages processed.</param>
/// <param name="AverageConfidence">Average OCR confidence across pages.</param>
/// <param name="PiiRedactionsCount">Total PII redactions made.</param>
/// <param name="ProcessingTimeMs">Total processing time in milliseconds.</param>
public sealed record OcrPipelineResult(
    Guid DocumentId,
    int ChunksCreated,
    int TotalPages,
    float AverageConfidence,
    int PiiRedactionsCount,
    long ProcessingTimeMs
);
