using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Entities;

namespace Scheduling.Infrastructure.Data.Configurations;

public sealed class IntakeRecordConfiguration : IEntityTypeConfiguration<IntakeRecord>
{
    public void Configure(EntityTypeBuilder<IntakeRecord> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.PatientId)
            .IsRequired();

        builder.HasIndex(i => i.PatientId)
            .HasDatabaseName("IX_IntakeRecord_PatientId");

        builder.Property(i => i.ChiefComplaint)
            .HasMaxLength(1000);

        builder.Property(i => i.CurrentMedications)
            .HasMaxLength(2000);

        builder.Property(i => i.Allergies)
            .HasMaxLength(1000);

        builder.Property(i => i.MedicalHistory)
            .HasMaxLength(4000);

        builder.Property(i => i.IsComplete)
            .HasDefaultValue(false);

        builder.HasOne(i => i.Appointment)
            .WithMany()
            .HasForeignKey(i => i.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Soft-delete query filter (DR-008)
        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}
