using SharedKernel.Domain;

namespace Clinical.Domain.Entities;

public sealed class DocumentEmbedding : BaseEntity
{
    public Guid DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public string ChunkText { get; set; } = string.Empty;

    // Vector stored as JSON float array; SQL Server has no native vector type
    public string EmbeddingVector { get; set; } = string.Empty;
    public int VectorDimension { get; set; }

    // Navigation
    public ClinicalDocument Document { get; set; } = null!;
}
