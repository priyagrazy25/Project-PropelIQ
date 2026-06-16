using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;

namespace Scheduling.Infrastructure.Data.Configurations;

public sealed class PreferredSlotSwapConfiguration : IEntityTypeConfiguration<PreferredSlotSwap>
{
    public void Configure(EntityTypeBuilder<PreferredSlotSwap> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.RequestingPatientId)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // FIFO ordering index (AC-4)
        builder.HasIndex(s => new { s.DesiredSlotId, s.Priority, s.RequestedAt })
            .HasDatabaseName("IX_PreferredSlotSwap_FIFO");

        builder.HasOne(s => s.OriginalAppointment)
            .WithMany()
            .HasForeignKey(s => s.OriginalAppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.DesiredSlot)
            .WithMany()
            .HasForeignKey(s => s.DesiredSlotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
