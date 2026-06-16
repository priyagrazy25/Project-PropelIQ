using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Services;
using Scheduling.Domain.Enums;
using SharedKernel.Caching;
using SharedKernel.Domain;

namespace Scheduling.Application.Commands.CancelAppointment;

public sealed class CancelAppointmentCommandHandler
{
    private readonly ISchedulingDbContext _dbContext;
    private readonly ISwapEngineService _swapEngine;
    private readonly ISlotNotificationService _notificationService;
    private readonly ICalendarSyncService _calendarSync;
    private readonly SlotReleasedChannel _slotReleasedChannel;
    private readonly WaitlistSlotReleasedChannel _waitlistSlotReleasedChannel;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CancelAppointmentCommandHandler> _logger;

    public CancelAppointmentCommandHandler(
        ISchedulingDbContext dbContext,
        ISwapEngineService swapEngine,
        ISlotNotificationService notificationService,
        ICalendarSyncService calendarSync,
        SlotReleasedChannel slotReleasedChannel,
        WaitlistSlotReleasedChannel waitlistSlotReleasedChannel,
        ICacheService cacheService,
        ILogger<CancelAppointmentCommandHandler> logger)
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

    public async Task<Result<bool>> HandleAsync(
        CancelAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _dbContext.Appointments
            .Include(a => a.Slot)
            .FirstOrDefaultAsync(
                a => a.Id == command.AppointmentId
                     && !a.IsDeleted,
                cancellationToken);

        if (appointment is null)
        {
            return Result<bool>.Failure("Appointment not found.");
        }

        // Staff can cancel any appointment; patients can only cancel their own
        if (!command.IsStaffAction && appointment.PatientId != command.PatientId)
        {
            return Result<bool>.Failure("Appointment not found or does not belong to patient.");
        }

        // Walk-in appointments can only be cancelled by staff (Edge Case)
        if (appointment.Type == AppointmentType.WalkIn && !command.IsStaffAction)
        {
            return Result<bool>.Failure("Walk-in appointments can only be cancelled by staff.");
        }

        // AC-5: Idempotent — return success if already cancelled
        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            _logger.LogInformation(
                "Idempotent cancel: appointment {AppointmentId} already cancelled",
                command.AppointmentId);
            return Result<bool>.Success(true);
        }

        if (appointment.Status == AppointmentStatus.Completed || appointment.Status == AppointmentStatus.InProgress)
        {
            return Result<bool>.Failure("Cannot cancel a completed or in-progress appointment.");
        }

        // 1. Cancel the appointment
        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancellationReason = command.CancellationReason;

        // 2. Release the slot
        Guid? releasedSlotId = null;
        if (appointment.Slot is not null)
        {
            appointment.Slot.Status = SlotStatus.Available;
            releasedSlotId = appointment.Slot.Id;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // 3. Expire any pending swap preferences for this appointment
        await _swapEngine.ExpireSwapsForAppointmentAsync(command.AppointmentId, cancellationToken);

        // 4. Notify slot released via SignalR
        if (releasedSlotId.HasValue)
        {
            await _notificationService.NotifySlotReleasedAsync(
                appointment.ProviderId, releasedSlotId.Value, cancellationToken);

            // 5. Publish to swap channel so background worker checks for waiting swap requests
            await _slotReleasedChannel.Writer.WriteAsync(
                new SlotReleasedEvent(releasedSlotId.Value, appointment.ProviderId),
                cancellationToken);

            // 6. Publish to waitlist channel so background worker notifies waitlisted patients
            await _waitlistSlotReleasedChannel.Writer.WriteAsync(
                new SlotReleasedEvent(releasedSlotId.Value, appointment.ProviderId),
                cancellationToken);
        }

        // 7. Invalidate caches
        await _cacheService.RemoveByPrefixAsync("provider-search:", cancellationToken);

        // 8. Sync calendar event removal (AC-4)
        await _calendarSync.SyncAppointmentCancelledAsync(command.AppointmentId, cancellationToken);

        _logger.LogInformation(
            "Appointment {AppointmentId} cancelled for patient {PatientId}",
            command.AppointmentId, command.PatientId);

        return Result<bool>.Success(true);
    }
}
