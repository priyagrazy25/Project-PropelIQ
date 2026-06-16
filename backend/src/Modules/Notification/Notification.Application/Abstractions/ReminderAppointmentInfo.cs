namespace Notification.Application.Abstractions;

public sealed record ReminderAppointmentInfo(
    Guid AppointmentId,
    Guid PatientId,
    DateTime AppointmentDateTime,
    string PatientFullName,
    string? PatientEmail,
    string? PatientPhone,
    string ProviderName,
    int NoShowRiskScore);
