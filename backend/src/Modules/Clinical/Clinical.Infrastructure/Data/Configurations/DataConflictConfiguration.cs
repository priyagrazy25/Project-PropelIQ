using Clinical.Domain.Entities;
using Clinical.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinical.Infrastructure.Data.Configurations;

public sealed class DataConflictConfiguration : IEntityTypeConfiguration<DataConflict>
{
    public void Configure(EntityTypeBuilder<DataConflict> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.PatientId)
            .IsRequired();

        builder.HasIndex(c => c.PatientId)
            .HasDatabaseName("IX_DataConflict_PatientId");

        builder.Property(c => c.FieldName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.SourceValue)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(c => c.ConflictingValue)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(c => c.Severity)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.ResolutionStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.StaffNotes)
            .HasMaxLength(4000);

        builder.HasIndex(c => new { c.PatientId, c.ResolutionStatus })
            .HasDatabaseName("IX_DataConflict_Patient_Status");

        builder.HasOne(c => c.SourceDocument)
            .WithMany()
            .HasForeignKey(c => c.SourceDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ConflictingDocument)
            .WithMany()
            .HasForeignKey(c => c.ConflictingDocumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
