namespace Notification.Application.Abstractions;

public interface IReminderAppointmentQuery
{
    Task<IReadOnlyList<ReminderAppointmentInfo>> GetUpcomingAppointmentsAsync(
        DateTime windowStart,
        DateTime windowEnd,
        CancellationToken cancellationToken = default);

    Task<ReminderAppointmentInfo?> GetAppointmentByIdAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<bool> HasReminderBeenSentAsync(
        Guid appointmentId,
        string channel,
        string reminderWindow,
        CancellationToken cancellationToken = default);
}
