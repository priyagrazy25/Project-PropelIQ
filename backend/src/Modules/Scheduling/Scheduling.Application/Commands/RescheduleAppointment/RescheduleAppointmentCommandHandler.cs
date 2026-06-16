using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Commands.BookAppointment;
using Scheduling.Application.Services;
using Scheduling.Domain.Enums;
using SharedKernel.Caching;
using SharedKernel.Domain;

namespace Scheduling.Application.Commands.RescheduleAppointment;

public sealed class RescheduleAppointmentCommandHandler
{
    private readonly ISchedulingDbContext _dbContext;
    private readonly ISwapEngineService _swapEngine;
    private readonly ISlotNotificationService _notificationService;
    private readonly ICalendarSyncService _calendarSync;
    private readonly SlotReleasedChannel _slotReleasedChannel;
    private readonly WaitlistSlotReleasedChannel _waitlistSlotReleasedChannel;
    private readonly ICacheService _cacheService;
    private readonly ILogger<RescheduleAppointmentCommandHandler> _logger;

    public RescheduleAppointmentCommandHandler(
        ISchedulingDbContext dbContext,
        ISwapEngineService swapEngine,
        ISlotNotificationService notificationService,
        ICalendarSyncService calendarSync,
        SlotReleasedChannel slotReleasedChannel,
        WaitlistSlotReleasedChannel waitlistSlotReleasedChannel,
        ICacheService cacheService,
        ILogger<RescheduleAppointmentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _swapEngine = swapEngine;
        _notificationService = notificationService;
        _calendarSync = calendarSync;
        _slotReleasedChannel = slotReleasedChannel;
        _waitlistSlotReleasedChannel = waitlistSlotReleasedChannel;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Reschedules an appointment: books new slot, cancels old, releases original slot.
    /// Idempotent via idempotency key. Returns CONFLICT if new slot is taken.
    /// </summary>
    public async Task<Result<BookAppointmentResult>> HandleAsync(
        RescheduleAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Idempotency check
        var idempotencyKey = $"reschedule-idempotency:{command.IdempotencyKey}";
        var cached = await _cacheService.GetAsync<BookAppointmentResult>(idempotencyKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogInformation("Idempotent reschedule hit for key {Key}", command.IdempotencyKey);
            return Result<BookAppointmentResult>.Success(cached);
        }

        // 2. Find and validate old appointment
        var oldAppointment = await _dbContext.Appointments
            .Include(a => a.Slot)
            .FirstOrDefaultAsync(
                a => a.Id == command.OldAppointmentId
                     && a.PatientId == command.PatientId
                     && !a.IsDeleted,
                cancellationToken);

        if (oldAppointment is null)
        {
            return Result<BookAppointmentResult>.Failure("Original appointment not found or does not belong to patient.");
        }

        if (oldAppointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed or AppointmentStatus.InProgress)
        {
            return Result<BookAppointmentResult>.Failure($"Cannot reschedule appointment with status '{oldAppointment.Status}'.");
        }

        // 3. Find the new slot and validate availability
        var newSlot = await _dbContext.AppointmentSlots
            .FirstOrDefaultAsync(s => s.Id == command.NewSlotId, cancellationToken);

        if (newSlot is null)
        {
            return Result<BookAppointmentResult>.Failure("New slot not found.");
        }

        if (newSlot.Status != SlotStatus.Available)
        {
            return Result<BookAppointmentResult>.Failure("CONFLICT");
        }

        // 4. Load provider for the result
        var provider = await _dbContext.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == oldAppointment.ProviderId && !p.IsDeleted, cancellationToken);

        if (provider is null)
        {
            return Result<BookAppointmentResult>.Failure("Provider not found.");
        }

        // 5. Book new slot
        newSlot.Status = SlotStatus.Booked;

        var newAppointment = new Scheduling.Domain.Entities.Appointment
        {
            PatientId = command.PatientId,
            ProviderId = oldAppointment.ProviderId,
            SlotId = command.NewSlotId,
            AppointmentDateTime = newSlot.StartTime,
            DurationMinutes = newSlot.DurationMinutes,
            Status = AppointmentStatus.Confirmed,
        };

        _dbContext.Appointments.Add(newAppointment);

        // 6. Cancel old appointment and release original slot
        oldAppointment.Status = AppointmentStatus.Rescheduled;
        oldAppointment.CancellationReason = command.CancellationReason ?? "Rescheduled to new time";

        Guid? releasedSlotId = null;
        if (oldAppointment.Slot is not null)
        {
            oldAppointment.Slot.Status = SlotStatus.Available;
            releasedSlotId = oldAppointment.Slot.Id;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict during reschedule for slot {SlotId}", command.NewSlotId);
            return Result<BookAppointmentResult>.Failure("CONFLICT");
        }

        // 7. Expire swap preferences for the old appointment
        await _swapEngine.ExpireSwapsForAppointmentAsync(command.OldAppointmentId, cancellationToken);

        // 8. Notify and trigger cascade for released slot
        if (releasedSlotId.HasValue)
        {
            await _notificationService.NotifySlotReleasedAsync(
                oldAppointment.ProviderId, releasedSlotId.Value, cancellationToken);

            // Swap queue evaluated first (AC-3), then waitlist
            await _slotReleasedChannel.Writer.WriteAsync(
                new SlotReleasedEvent(releasedSlotId.Value, oldAppointment.ProviderId),
                cancellationToken);

            await _waitlistSlotReleasedChannel.Writer.WriteAsync(
                new SlotReleasedEvent(releasedSlotId.Value, oldAppointment.ProviderId),
                cancellationToken);
        }

        // Notify new slot booked
        await _notificationService.NotifySlotBookedAsync(
            newAppointment.ProviderId, command.NewSlotId, cancellationToken);

        var bookingResult = new BookAppointmentResult(
            newAppointment.Id,
            provider.Id,
            provider.Name,
            provider.Specialty,
            provider.Location ?? string.Empty,
            newSlot.StartTime,
            newSlot.StartTime.AddMinutes(newSlot.DurationMinutes),
            newAppointment.Status.ToString());

        // 9. Cache for idempotency (L2 = 5 min)
        await _cacheService.SetAsync(idempotencyKey, bookingResult, CacheTier.L2, cancellationToken);

        // 10. Invalidate provider search cache
        await _cacheService.RemoveByPrefixAsync("provider-search:", cancellationToken);

        // 11. Sync calendar: remove old event, create new (AC-4)
        await _calendarSync.SyncAppointmentRescheduledAsync(
            command.OldAppointmentId, newAppointment.Id, cancellationToken);

        _logger.LogInformation(
            "Appointment {OldId} rescheduled to {NewId} for patient {PatientId}",
            command.OldAppointmentId, newAppointment.Id, command.PatientId);

        return Result<BookAppointmentResult>.Success(bookingResult);
    }
}
