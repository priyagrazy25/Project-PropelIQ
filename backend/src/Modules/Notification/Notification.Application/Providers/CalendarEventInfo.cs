namespace Notification.Application.Providers;

/// <summary>
/// Data required to create or update a calendar event with an external provider.
/// </summary>
public sealed record CalendarEventInfo(
    Guid AppointmentId,
    string PatientName,
    string ProviderName,
    DateTime StartTime,
    DateTime EndTime,
    string? Location,
    string? Notes);
