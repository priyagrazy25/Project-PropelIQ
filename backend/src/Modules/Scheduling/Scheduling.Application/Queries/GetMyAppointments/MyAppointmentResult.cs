namespace Scheduling.Application.Queries.GetMyAppointments;

public sealed record MyAppointmentResult(
    Guid AppointmentId,
    Guid ProviderId,
    string ProviderName,
    string Specialty,
    string Location,
    DateTime AppointmentDateTime,
    int DurationMinutes,
    string Status,
    string Type);
