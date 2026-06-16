namespace Notification.Application.Abstractions;

public sealed record AppointmentConfirmationInfo(
    Guid AppointmentId,
    Guid PatientId,
    string PatientFullName,
    string? PatientEmail,
    string ProviderName,
    string? ProviderSpecialty,
    DateTime AppointmentDateTime,
    int DurationMinutes,
    string AppointmentType,
    string? Location,
    string? PrepNotes);
