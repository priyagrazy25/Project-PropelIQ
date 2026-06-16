using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions;
using Notification.Domain.Entities;
using Notification.Infrastructure.Data;

namespace Notification.Infrastructure.Repositories;

public sealed class ReminderDeliveryLogRepository : IReminderDeliveryLogRepository
{
    private readonly NotificationDbContext _dbContext;

    public ReminderDeliveryLogRepository(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ReminderDeliveryLog log, CancellationToken cancellationToken = default)
    {
        _dbContext.ReminderDeliveryLogs.Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid appointmentId,
        string channel,
        string reminderWindow,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ReminderDeliveryLogs
            .AsNoTracking()
            .AnyAsync(l => l.AppointmentId == appointmentId
                           && l.Channel == channel
                           && l.ReminderWindow == reminderWindow
                           && l.Status == "Delivered",
                      cancellationToken);
    }

    public async Task<int> GetTodayEmailCountAsync(CancellationToken cancellationToken = default)
    {
        var todayUtc = DateTime.UtcNow.Date;

        return await _dbContext.ReminderDeliveryLogs
            .AsNoTracking()
            .CountAsync(l => l.Channel == "Email"
                             && l.Status == "Delivered"
                             && l.SentAt >= todayUtc,
                        cancellationToken);
    }
}
