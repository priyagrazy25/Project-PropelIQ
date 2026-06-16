namespace Scheduling.Application.Commands.WalkInBooking;

public sealed record WalkInBookingResult(
    Guid AppointmentId,
    Guid PatientId,
    Guid ProviderId,
    string ProviderName,
    DateTime? SlotStartTime,
    DateTime? SlotEndTime,
    string Status,
    int QueuePosition,
    int EstimatedWaitMinutes,
    bool IsQueued);
