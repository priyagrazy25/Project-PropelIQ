using Notification.Domain.Entities;

namespace Notification.Application.Abstractions;

public interface IReminderDeliveryLogRepository
{
    Task AddAsync(ReminderDeliveryLog log, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid appointmentId, string channel, string reminderWindow, CancellationToken cancellationToken = default);
    Task<int> GetTodayEmailCountAsync(CancellationToken cancellationToken = default);
}
