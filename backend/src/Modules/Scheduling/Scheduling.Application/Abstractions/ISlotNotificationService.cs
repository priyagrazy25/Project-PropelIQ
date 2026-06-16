namespace Scheduling.Application.Abstractions;

/// <summary>
/// Broadcasts real-time slot availability changes to connected SignalR clients (AC-2).
/// </summary>
public interface ISlotNotificationService
{
    Task NotifySlotBookedAsync(Guid providerId, Guid slotId, CancellationToken cancellationToken = default);
    Task NotifySlotReleasedAsync(Guid providerId, Guid slotId, CancellationToken cancellationToken = default);
    Task NotifySwapExecutedAsync(Guid patientId, Guid providerId, string providerName, DateTime newSlotStart, DateTime newSlotEnd, CancellationToken cancellationToken = default);
    Task NotifyWaitlistAvailableAsync(Guid patientId, Guid waitlistId, Guid providerId, string providerName, Guid slotId, DateTime slotStartTime, DateTime slotEndTime, CancellationToken cancellationToken = default);
    Task NotifyQueueUpdatedAsync(Guid providerId, int queuePosition, int estimatedWaitMinutes, CancellationToken cancellationToken = default);
    Task NotifyQueueStatusChangedAsync(Guid appointmentId, string newStatus, CancellationToken cancellationToken = default);
    Task NotifyPatientArrivedAsync(Guid appointmentId, Guid patientId, CancellationToken cancellationToken = default);
}
