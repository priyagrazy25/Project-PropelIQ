using SharedKernel.Domain;

namespace Clinical.Domain.Entities;

/// <summary>
/// Stores text chunks extracted from clinical documents after OCR processing (AIR-R01).
/// Each chunk is 512 tokens with 51-token overlap for RAG retrieval.
/// </summary>
public sealed class DocumentChunk : BaseEntity
{
    /// <summary>
    /// Reference to the parent document.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Patient owning this document (denormalized for access control).
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// Zero-based chunk index within the document.
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Source page number(s) this chunk originated from.
    /// </summary>
    public string SourcePages { get; set; } = string.Empty;

    /// <summary>
    /// PII-redacted text content of this chunk.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Approximate token count for this chunk.
    /// </summary>
    public int TokenCount { get; set; }

    /// <summary>
    /// OCR confidence score (0.0–1.0) for text extraction quality.
    /// </summary>
    public float OcrConfidence { get; set; }

    /// <summary>
    /// Quality flags (e.g., "blank", "low_confidence", "garbled").
    /// </summary>
    public string? QualityFlags { get; set; }

    // Navigation
    public ClinicalDocument Document { get; set; } = null!;
}
