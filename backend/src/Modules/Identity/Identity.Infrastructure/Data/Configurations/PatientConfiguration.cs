using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.UserId)
            .IsUnique()
            .HasDatabaseName("IX_Patient_UserId");

        builder.Property(p => p.InsuranceProvider)
            .HasMaxLength(200);

        builder.Property(p => p.InsurancePolicyNumber)
            .HasMaxLength(100);

        builder.Property(p => p.EmergencyContactName)
            .HasMaxLength(200);

        builder.Property(p => p.EmergencyContactPhone)
            .HasMaxLength(20);

        builder.Property(p => p.IntakeCompleted)
            .HasDefaultValue(false);

        // Soft-delete query filter (DR-008)
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
