using Clinical.Domain.Enums;
using SharedKernel.Domain;

namespace Clinical.Domain.Entities;

public sealed class ClinicalDocument : BaseEntity
{
    public Guid PatientId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string EncryptedFilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public ProcessingStatus ProcessingStatus { get; set; } = ProcessingStatus.Pending;
    public string? ProcessingError { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Guid? UploadedByUserId { get; set; }

    // Navigation
    public ICollection<ExtractedData> ExtractedDataPoints { get; set; } = [];
    public ICollection<DocumentEmbedding> Embeddings { get; set; } = [];
    public ICollection<DocumentChunk> Chunks { get; set; } = [];
}
