namespace Scheduling.Application.Commands.BookAppointment;

public sealed record BookAppointmentCommand(
    Guid PatientId,
    Guid ProviderId,
    Guid SlotId,
    string IdempotencyKey);
