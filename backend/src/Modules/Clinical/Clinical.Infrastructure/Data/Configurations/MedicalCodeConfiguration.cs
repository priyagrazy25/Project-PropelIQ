using Clinical.Domain.Entities;
using Clinical.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinical.Infrastructure.Data.Configurations;

public sealed class MedicalCodeConfiguration : IEntityTypeConfiguration<MedicalCode>
{
    public void Configure(EntityTypeBuilder<MedicalCode> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.PatientId)
            .IsRequired();

        builder.HasIndex(m => m.PatientId)
            .HasDatabaseName("IX_MedicalCode_PatientId");

        builder.Property(m => m.CodeType)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(m => m.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.VerificationStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(m => new { m.PatientId, m.CodeType, m.Code })
            .HasDatabaseName("IX_MedicalCode_Patient_Type_Code");

        builder.HasOne(m => m.Document)
            .WithMany()
            .HasForeignKey(m => m.DocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.ExtractedData)
            .WithMany()
            .HasForeignKey(m => m.ExtractedDataId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
