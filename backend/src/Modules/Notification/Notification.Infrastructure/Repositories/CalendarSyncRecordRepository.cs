using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions;
using Notification.Domain.Entities;
using Notification.Infrastructure.Data;

namespace Notification.Infrastructure.Repositories;

public sealed class CalendarSyncRecordRepository : ICalendarSyncRecordRepository
{
    private readonly NotificationDbContext _dbContext;

    public CalendarSyncRecordRepository(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CalendarSyncRecord?> GetByAppointmentAndProviderAsync(
        Guid appointmentId,
        string provider,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CalendarSyncRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.AppointmentId == appointmentId
                     && r.Provider == provider
                     && !r.IsDeleted,
                cancellationToken);
    }

    public async Task<IReadOnlyList<CalendarSyncRecord>> GetByAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CalendarSyncRecords
            .AsNoTracking()
            .Where(r => r.AppointmentId == appointmentId && !r.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CalendarSyncRecord record, CancellationToken cancellationToken = default)
    {
        _dbContext.CalendarSyncRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CalendarSyncRecord record, CancellationToken cancellationToken = default)
    {
        _dbContext.CalendarSyncRecords.Update(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
