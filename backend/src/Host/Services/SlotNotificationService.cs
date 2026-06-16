using Host.Hubs;
using Microsoft.AspNetCore.SignalR;
using Scheduling.Application.Abstractions;
using SharedKernel.Caching;

namespace Host.Services;

/// <summary>
/// Broadcasts slot availability changes via SignalR and invalidates search cache (AC-2, AC-5).
/// </summary>
public sealed class SlotNotificationService : ISlotNotificationService
{
    private readonly IHubContext<AppointmentHub> _hubContext;
    private readonly ICacheService _cacheService;

    public SlotNotificationService(IHubContext<AppointmentHub> hubContext, ICacheService cacheService)
    {
        _hubContext = hubContext;
        _cacheService = cacheService;
    }

    public async Task NotifySlotBookedAsync(Guid providerId, Guid slotId, CancellationToken cancellationToken)
    {
        var group = $"provider:{providerId}";

        await _hubContext.Clients.Group(group).SendAsync(
            "SlotBooked",
            new { ProviderId = providerId.ToString(), SlotId = slotId.ToString() },
            cancellationToken);

        // Invalidate search cache entries for this provider
        await _cacheService.RemoveByPrefixAsync("provider-search:", cancellationToken);
    }

    public async Task NotifySlotReleasedAsync(Guid providerId, Guid slotId, CancellationToken cancellationToken)
    {
        var group = $"provider:{providerId}";

        await _hubContext.Clients.Group(group).SendAsync(
            "SlotReleased",
            new { ProviderId = providerId.ToString(), SlotId = slotId.ToString() },
            cancellationToken);

        // Invalidate search cache entries for this provider
        await _cacheService.RemoveByPrefixAsync("provider-search:", cancellationToken);
    }

    public async Task NotifySwapExecutedAsync(Guid patientId, Guid providerId, string providerName, DateTime newSlotStart, DateTime newSlotEnd, CancellationToken cancellationToken)
    {
        // Notify the specific patient via their user group
        var patientGroup = $"user:{patientId}";

        await _hubContext.Clients.Group(patientGroup).SendAsync(
            "SwapExecuted",
            new
            {
                PatientId = patientId.ToString(),
                ProviderId = providerId.ToString(),
                ProviderName = providerName,
                NewSlotStart = newSlotStart,
                NewSlotEnd = newSlotEnd,
            },
            cancellationToken);
    }

    public async Task NotifyWaitlistAvailableAsync(Guid patientId, Guid waitlistId, Guid providerId, string providerName, Guid slotId, DateTime slotStartTime, DateTime slotEndTime, CancellationToken cancellationToken)
    {
        var patientGroup = $"user:{patientId}";

        await _hubContext.Clients.Group(patientGroup).SendAsync(
            "WaitlistAvailable",
            new
            {
                WaitlistId = waitlistId.ToString(),
                ProviderId = providerId.ToString(),
                ProviderName = providerName,
                SlotId = slotId.ToString(),
                SlotStartTime = slotStartTime,
                SlotEndTime = slotEndTime,
            },
            cancellationToken);
    }

    public async Task NotifyQueueUpdatedAsync(Guid providerId, int queuePosition, int estimatedWaitMinutes, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group("queue:status").SendAsync(
            "QueueUpdated",
            new
            {
                ProviderId = providerId.ToString(),
                QueuePosition = queuePosition,
                EstimatedWaitMinutes = estimatedWaitMinutes,
                UpdatedAt = DateTime.UtcNow,
            },
            cancellationToken);
    }

    public async Task NotifyQueueStatusChangedAsync(Guid appointmentId, string newStatus, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group("queue:status").SendAsync(
            "QueueStatusChanged",
            new
            {
                AppointmentId = appointmentId.ToString(),
                NewStatus = newStatus,
                UpdatedAt = DateTime.UtcNow,
            },
            cancellationToken);
    }

    public async Task NotifyPatientArrivedAsync(Guid appointmentId, Guid patientId, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group("queue:status").SendAsync(
            "PatientArrived",
            new
            {
                AppointmentId = appointmentId.ToString(),
                PatientId = patientId.ToString(),
                ArrivedAt = DateTime.UtcNow,
            },
            cancellationToken);
    }
}
