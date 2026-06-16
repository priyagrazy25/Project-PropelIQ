namespace Scheduling.Application.Commands.RegisterSwapPreference;

public sealed record RegisterSwapPreferenceResult(
    Guid SwapId,
    Guid AppointmentId,
    Guid DesiredSlotId,
    int Priority,
    string Status);
