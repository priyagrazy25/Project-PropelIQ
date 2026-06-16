using Notification.Domain.Entities;

namespace Notification.Application.Abstractions;

public interface ICalendarSyncRecordRepository
{
    Task<CalendarSyncRecord?> GetByAppointmentAndProviderAsync(
        Guid appointmentId,
        string provider,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalendarSyncRecord>> GetByAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task AddAsync(CalendarSyncRecord record, CancellationToken cancellationToken = default);
    Task UpdateAsync(CalendarSyncRecord record, CancellationToken cancellationToken = default);
}
