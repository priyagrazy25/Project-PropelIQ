namespace Scheduling.Application.Commands.BookAppointment;

public sealed record BookAppointmentResult(
    Guid AppointmentId,
    Guid ProviderId,
    string ProviderName,
    string Specialty,
    string Location,
    DateTime SlotStartTime,
    DateTime SlotEndTime,
    string Status);
