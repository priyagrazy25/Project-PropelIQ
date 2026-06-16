using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Entities;
using SharedKernel.Data;
using SharedKernel.Domain;

namespace Scheduling.Infrastructure.Data;

public sealed class SchedulingDbContext : BaseDbContext, ISchedulingDbContext
{
    public SchedulingDbContext(DbContextOptions<SchedulingDbContext> options) : base(options)
    {
    }

    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<AppointmentSlot> AppointmentSlots => Set<AppointmentSlot>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<PreferredSlotSwap> PreferredSlotSwaps => Set<PreferredSlotSwap>();
    public DbSet<Waitlist> Waitlists => Set<Waitlist>();
    public DbSet<IntakeRecord> IntakeRecords => Set<IntakeRecord>();
    public DbSet<NoShowRiskScore> NoShowRiskScores => Set<NoShowRiskScore>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public void SetRowVersion(Appointment appointment, byte[] rowVersion)
    {
        Entry(appointment).Property(a => a.RowVersion).OriginalValue = rowVersion;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("scheduling");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulingDbContext).Assembly);
    }
}
