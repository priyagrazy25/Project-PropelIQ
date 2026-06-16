using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Enums;
using SharedKernel.Caching;

namespace Scheduling.Application.Services;

public sealed class SwapEngineService : ISwapEngineService
{
    private readonly ISchedulingDbContext _dbContext;
    private readonly ISlotNotificationService _notificationService;
    private readonly WaitlistSlotReleasedChannel _waitlistChannel;
    private readonly ICacheService _cacheService;
    private readonly ILogger<SwapEngineService> _logger;

    public SwapEngineService(
        ISchedulingDbContext dbContext,
        ISlotNotificationService notificationService,
        WaitlistSlotReleasedChannel waitlistChannel,
        ICacheService cacheService,
        ILogger<SwapEngineService> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _waitlistChannel = waitlistChannel;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<SwapExecutionResult?> ProcessSlotReleaseAsync(
        Guid releasedSlotId,
        CancellationToken cancellationToken = default)
    {
        // 1. Find FIFO-first pending swap for this released slot
        var swap = await _dbContext.PreferredSlotSwaps
            .Include(s => s.OriginalAppointment)
            .Include(s => s.DesiredSlot)
                .ThenInclude(slot => slot.Provider)
            .Where(s => s.DesiredSlotId == releasedSlotId
                        && s.Status == SwapStatus.Pending
                        && !s.IsDeleted)
            .OrderBy(s => s.Priority)
            .FirstOrDefaultAsync(cancellationToken);

        if (swap is null)
        {
            _logger.LogDebug("No pending swap requests for released slot {SlotId}", releasedSlotId);
            return null;
        }

        // 2. Validate the swap is still eligible
        var appointment = swap.OriginalAppointment;
        if (appointment.Status == AppointmentStatus.Cancelled || appointment.IsDeleted)
        {
            swap.Status = SwapStatus.Expired;
            swap.ProcessedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Swap {SwapId} expired — appointment {AppointmentId} is cancelled", swap.Id, appointment.Id);
            return null;
        }

        var desiredSlot = swap.DesiredSlot;

        // 3. Verify the desired slot is now available (released)
        if (desiredSlot.Status != SlotStatus.Available && desiredSlot.Status != SlotStatus.Released)
        {
            _logger.LogWarning("Desired slot {SlotId} is not available (status: {Status}), skipping swap {SwapId}",
                desiredSlot.Id, desiredSlot.Status, swap.Id);
            return null;
        }

        // 4. Verify provider hasn't changed (incompatibility check per UC-004 extension 4a)
        if (desiredSlot.ProviderId != appointment.ProviderId)
        {
            swap.Status = SwapStatus.Expired;
            swap.ProcessedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Swap {SwapId} expired — provider mismatch", swap.Id);
            return null;
        }

        // 5. Execute the swap in a transaction
        var originalSlotId = appointment.SlotId;
        var provider = desiredSlot.Provider;

        // Move appointment to the desired slot
        appointment.SlotId = desiredSlot.Id;
        appointment.AppointmentDateTime = desiredSlot.StartTime;
        appointment.DurationMinutes = desiredSlot.DurationMinutes;

        // Mark desired slot as booked
        desiredSlot.Status = SlotStatus.Booked;

        // Release original slot
        if (originalSlotId.HasValue)
        {
            var originalSlot = await _dbContext.AppointmentSlots
                .FirstOrDefaultAsync(s => s.Id == originalSlotId.Value, cancellationToken);

            if (originalSlot is not null)
            {
                originalSlot.Status = SlotStatus.Available;
            }
        }

        // Mark swap as executed
        swap.Status = SwapStatus.Executed;
        swap.ProcessedAt = DateTime.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict during swap {SwapId} execution", swap.Id);
            return null;
        }

        var result = new SwapExecutionResult(
            swap.Id,
            swap.RequestingPatientId,
            provider.Id,
            provider.Name,
            originalSlotId ?? Guid.Empty,
            desiredSlot.Id,
            desiredSlot.StartTime,
            desiredSlot.StartTime.AddMinutes(desiredSlot.DurationMinutes));

        // 6. Notify via SignalR
        await _notificationService.NotifySwapExecutedAsync(
            swap.RequestingPatientId,
            provider.Id,
            provider.Name,
            result.NewSlotStart,
            result.NewSlotEnd,
            cancellationToken);

        // 7. Notify original slot released (for real-time UI and waitlist)
        if (originalSlotId.HasValue)
        {
            await _notificationService.NotifySlotReleasedAsync(provider.Id, originalSlotId.Value, cancellationToken);

            // Publish to waitlist channel so released original slot triggers waitlist notifications
            await _waitlistChannel.Writer.WriteAsync(
                new SlotReleasedEvent(originalSlotId.Value, provider.Id),
                cancellationToken);
        }

        // 8. Invalidate caches
        await _cacheService.RemoveByPrefixAsync("provider-search:", cancellationToken);

        _logger.LogInformation(
            "Swap {SwapId} executed: patient {PatientId} moved from slot {OriginalSlotId} to {NewSlotId}",
            swap.Id, swap.RequestingPatientId, originalSlotId, desiredSlot.Id);

        return result;
    }

    public async Task ExpireSwapsForAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var pendingSwaps = await _dbContext.PreferredSlotSwaps
            .Where(s => s.OriginalAppointmentId == appointmentId
                        && s.Status == SwapStatus.Pending
                        && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        if (pendingSwaps.Count == 0) return;

        foreach (var swap in pendingSwaps)
        {
            swap.Status = SwapStatus.Expired;
            swap.ProcessedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Expired {Count} swap preferences for cancelled appointment {AppointmentId}",
            pendingSwaps.Count, appointmentId);
    }
}
