namespace Scheduling.Application.Commands.RegisterSwapPreference;

public sealed record RegisterSwapPreferenceCommand(
    Guid PatientId,
    Guid AppointmentId,
    Guid DesiredSlotId);
