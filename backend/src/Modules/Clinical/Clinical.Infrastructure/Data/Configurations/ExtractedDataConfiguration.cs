using Clinical.Domain.Entities;
using Clinical.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinical.Infrastructure.Data.Configurations;

public sealed class ExtractedDataConfiguration : IEntityTypeConfiguration<ExtractedData>
{
    public void Configure(EntityTypeBuilder<ExtractedData> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.PatientId)
            .IsRequired();

        builder.HasIndex(e => e.PatientId)
            .HasDatabaseName("IX_ExtractedData_PatientId");

        builder.Property(e => e.Category)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Key)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Value)
            .HasMaxLength(4000)
            .IsRequired();

        // CHECK constraint: 0.0 <= ConfidenceScore <= 1.0 (AC-2, DR-005)
        builder.Property(e => e.ConfidenceScore)
            .IsRequired();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ExtractedData_ConfidenceScore",
            "[ConfidenceScore] >= 0.0 AND [ConfidenceScore] <= 1.0"));

        builder.Property(e => e.SourceText)
            .HasMaxLength(2000);

        builder.HasIndex(e => new { e.DocumentId, e.Category })
            .HasDatabaseName("IX_ExtractedData_Document_Category");
    }
}
