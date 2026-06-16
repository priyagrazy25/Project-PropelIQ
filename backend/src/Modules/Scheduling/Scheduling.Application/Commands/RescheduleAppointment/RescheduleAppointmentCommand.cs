namespace Scheduling.Application.Commands.RescheduleAppointment;

public sealed record RescheduleAppointmentCommand(
    Guid PatientId,
    Guid OldAppointmentId,
    Guid NewSlotId,
    string IdempotencyKey,
    string? CancellationReason = null);
