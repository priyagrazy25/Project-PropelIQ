using Clinical.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinical.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for ICD-10-CM lookup table (AIR-004).
/// </summary>
public sealed class Icd10CodeConfiguration : IEntityTypeConfiguration<Icd10Code>
{
    public void Configure(EntityTypeBuilder<Icd10Code> builder)
    {
        builder.ToTable("Icd10Codes");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Code)
            .HasMaxLength(10)
            .IsRequired();

        builder.HasIndex(i => i.Code)
            .IsUnique()
            .HasDatabaseName("IX_Icd10Code_Code");

        builder.Property(i => i.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(i => i.ShortDescription)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(i => i.Category)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.Keywords)
            .HasMaxLength(1000);

        builder.Property(i => i.IsBillable)
            .IsRequired()
            .HasDefaultValue(true);

        // Full-text search index for keywords
        builder.HasIndex(i => i.Keywords)
            .HasDatabaseName("IX_Icd10Code_Keywords");
    }
}
