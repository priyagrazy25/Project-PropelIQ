using Clinical.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinical.Infrastructure.Data.Configurations;

public sealed class PatientView360Configuration : IEntityTypeConfiguration<PatientView360>
{
    public void Configure(EntityTypeBuilder<PatientView360> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PatientId)
            .IsRequired();

        builder.HasIndex(p => p.PatientId)
            .IsUnique()
            .HasDatabaseName("IX_PatientView360_PatientId");

        // JSON column for structured vitals (AC-3, DR-006)
        builder.OwnsOne(p => p.Vitals, vitals =>
        {
            vitals.ToJson();
        });

        // JSON-stored clinical sections
        builder.Property(p => p.MedicalHistory)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.Medications)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.Allergies)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.LabResults)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.Diagnoses)
            .HasColumnType("nvarchar(max)");
    }
}
