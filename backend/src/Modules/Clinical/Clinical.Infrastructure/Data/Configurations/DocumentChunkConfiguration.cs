using Clinical.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinical.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for DocumentChunk entity (AIR-R01).
/// </summary>
public sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.DocumentId)
            .IsRequired();

        builder.HasIndex(e => e.DocumentId)
            .HasDatabaseName("IX_DocumentChunks_DocumentId");

        builder.Property(e => e.PatientId)
            .IsRequired();

        builder.HasIndex(e => e.PatientId)
            .HasDatabaseName("IX_DocumentChunks_PatientId");

        builder.Property(e => e.ChunkIndex)
            .IsRequired();

        // Composite index for document + chunk ordering
        builder.HasIndex(e => new { e.DocumentId, e.ChunkIndex })
            .HasDatabaseName("IX_DocumentChunks_DocumentId_ChunkIndex")
            .IsUnique();

        builder.Property(e => e.SourcePages)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Content)
            .IsRequired();

        builder.Property(e => e.TokenCount)
            .IsRequired();

        builder.Property(e => e.OcrConfidence)
            .IsRequired();

        // CHECK constraint: 0.0 <= OcrConfidence <= 1.0
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_DocumentChunks_OcrConfidence",
            "[OcrConfidence] >= 0.0 AND [OcrConfidence] <= 1.0"));

        builder.Property(e => e.QualityFlags)
            .HasMaxLength(200);

        // Navigation to parent document
        builder.HasOne(e => e.Document)
            .WithMany(d => d.Chunks)
            .HasForeignKey(e => e.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
