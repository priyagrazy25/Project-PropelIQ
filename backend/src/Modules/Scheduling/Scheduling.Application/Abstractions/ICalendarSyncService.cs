namespace Scheduling.Application.Abstractions;

/// <summary>
/// Synchronizes appointment events with external calendar providers
/// (Google Calendar API v3, Microsoft Graph v1.0). AC-4.
/// </summary>
public interface ICalendarSyncService
{
    Task SyncAppointmentCreatedAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task SyncAppointmentCancelledAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task SyncAppointmentRescheduledAsync(Guid oldAppointmentId, Guid newAppointmentId, CancellationToken cancellationToken = default);
}
