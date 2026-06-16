using Clinical.Domain.Entities;
using Clinical.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinical.Infrastructure.Data.Configurations;

public sealed class ClinicalDocumentConfiguration : IEntityTypeConfiguration<ClinicalDocument>
{
    public void Configure(EntityTypeBuilder<ClinicalDocument> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.PatientId)
            .IsRequired();

        builder.HasIndex(d => d.PatientId)
            .HasDatabaseName("IX_ClinicalDocument_PatientId");

        builder.Property(d => d.FileName)
            .HasMaxLength(500)
            .IsRequired();

        // Encrypted storage path (DR-004)
        builder.Property(d => d.EncryptedFilePath)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(d => d.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.ProcessingStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.ProcessingError)
            .HasMaxLength(2000);

        builder.HasMany(d => d.ExtractedDataPoints)
            .WithOne(e => e.Document)
            .HasForeignKey(e => e.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Embeddings)
            .WithOne(e => e.Document)
            .HasForeignKey(e => e.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft-delete query filter (DR-008)
        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}
