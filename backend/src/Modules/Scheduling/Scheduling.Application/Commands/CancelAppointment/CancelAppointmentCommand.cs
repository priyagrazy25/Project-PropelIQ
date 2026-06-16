namespace Scheduling.Application.Commands.CancelAppointment;

public sealed record CancelAppointmentCommand(
    Guid PatientId,
    Guid AppointmentId,
    string? CancellationReason,
    bool IsStaffAction = false);
