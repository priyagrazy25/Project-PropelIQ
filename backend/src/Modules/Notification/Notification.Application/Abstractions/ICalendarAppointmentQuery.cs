namespace Notification.Application.Abstractions;

/// <summary>
/// Provides appointment details needed for calendar sync operations.
/// Implemented in the Host project to bridge Scheduling and Notification modules.
/// </summary>
public interface ICalendarAppointmentQuery
{
    Task<CalendarAppointmentInfo?> GetAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);
}

public sealed record CalendarAppointmentInfo(
    Guid AppointmentId,
    Guid PatientId,
    string PatientName,
    string ProviderName,
    DateTime StartTime,
    DateTime EndTime,
    string? Location,
    string? Notes);
