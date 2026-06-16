using Clinical.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinical.Infrastructure.Data.Configurations;

public sealed class DocumentEmbeddingConfiguration : IEntityTypeConfiguration<DocumentEmbedding>
{
    public void Configure(EntityTypeBuilder<DocumentEmbedding> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ChunkText)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        // Stored as JSON float array (SQL Server lacks native vector type)
        builder.Property(e => e.EmbeddingVector)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.HasIndex(e => new { e.DocumentId, e.ChunkIndex })
            .IsUnique()
            .HasDatabaseName("IX_DocumentEmbedding_Document_Chunk");
    }
}
