using Microsoft.EntityFrameworkCore;
using Scheduling.Domain.Entities;
using SharedKernel.Domain;

namespace Scheduling.Application.Abstractions;

public interface ISchedulingDbContext
{
    DbSet<Provider> Providers { get; }
    DbSet<AppointmentSlot> AppointmentSlots { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<PreferredSlotSwap> PreferredSlotSwaps { get; }
    DbSet<Waitlist> Waitlists { get; }
    DbSet<NoShowRiskScore> NoShowRiskScores { get; }
    DbSet<AuditLog> AuditLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    void SetRowVersion(Appointment appointment, byte[] rowVersion);
}
