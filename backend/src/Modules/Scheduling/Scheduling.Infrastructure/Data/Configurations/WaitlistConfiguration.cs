using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;

namespace Scheduling.Infrastructure.Data.Configurations;

public sealed class WaitlistConfiguration : IEntityTypeConfiguration<Waitlist>
{
    public void Configure(EntityTypeBuilder<Waitlist> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.PatientId)
            .IsRequired();

        builder.HasIndex(w => w.PatientId)
            .HasDatabaseName("IX_Waitlist_PatientId");

        builder.Property(w => w.PreferredDateStart)
            .IsRequired();

        builder.Property(w => w.PreferredDateEnd)
            .IsRequired();

        builder.Property(w => w.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(w => new { w.ProviderId, w.Status, w.Position })
            .HasDatabaseName("IX_Waitlist_Provider_Status_Position");

        builder.HasOne(w => w.Provider)
            .WithMany()
            .HasForeignKey(w => w.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Soft-delete query filter (DR-008)
        builder.HasQueryFilter(w => !w.IsDeleted);
    }
}
