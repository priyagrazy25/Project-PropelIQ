using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;

namespace Scheduling.Infrastructure.Data.Configurations;

public sealed class AppointmentSlotConfiguration : IEntityTypeConfiguration<AppointmentSlot>
{
    public void Configure(EntityTypeBuilder<AppointmentSlot> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.StartTime)
            .IsRequired();

        builder.Property(s => s.DurationMinutes)
            .HasDefaultValue(30);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Unique constraint: ProviderID + DateTime prevents double-booking (DR-003, DR-009)
        builder.HasIndex(s => new { s.ProviderId, s.StartTime })
            .IsUnique()
            .HasDatabaseName("IX_AppointmentSlot_Provider_StartTime");

        // Row-level locking via concurrency token (DR-009)
        builder.Property(s => s.RowVersion)
            .IsRowVersion();

        builder.HasOne(s => s.Appointment)
            .WithOne(a => a.Slot)
            .HasForeignKey<Appointment>(a => a.SlotId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
