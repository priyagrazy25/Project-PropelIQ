using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;
using SharedKernel.Data;

namespace Notification.Infrastructure.Data;

public sealed class NotificationDbContext : BaseDbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<ReminderDeliveryLog> ReminderDeliveryLogs => Set<ReminderDeliveryLog>();
    public DbSet<CalendarSyncRecord> CalendarSyncRecords => Set<CalendarSyncRecord>();
    public DbSet<CalendarOAuthToken> CalendarOAuthTokens => Set<CalendarOAuthToken>();
    public DbSet<PdfConfirmationRecord> PdfConfirmationRecords => Set<PdfConfirmationRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("notification");

        modelBuilder.Entity<ReminderDeliveryLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Channel).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ReminderWindow).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.FailureReason).HasMaxLength(500);

            entity.HasIndex(e => new { e.AppointmentId, e.Channel, e.ReminderWindow })
                  .HasDatabaseName("IX_ReminderDeliveryLog_Unique");
        });

        modelBuilder.Entity<CalendarSyncRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Provider).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ExternalEventId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.FailureReason).HasMaxLength(500);

            entity.HasIndex(e => new { e.AppointmentId, e.Provider })
                  .HasDatabaseName("IX_CalendarSyncRecord_AppointmentProvider");
        });

        modelBuilder.Entity<CalendarOAuthToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Provider).HasMaxLength(20).IsRequired();
            entity.Property(e => e.EncryptedAccessToken).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.EncryptedRefreshToken).HasMaxLength(2000).IsRequired();

            entity.HasIndex(e => new { e.PatientId, e.Provider })
                  .IsUnique()
                  .HasDatabaseName("IX_CalendarOAuthToken_PatientProvider");
        });

        modelBuilder.Entity<PdfConfirmationRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.DeliveryStatus).HasMaxLength(30);
            entity.Property(e => e.FailureReason).HasMaxLength(500);
            entity.Property(e => e.PdfContent).IsRequired();

            entity.HasIndex(e => e.AppointmentId)
                  .HasDatabaseName("IX_PdfConfirmationRecord_AppointmentId");
        });
    }
}
