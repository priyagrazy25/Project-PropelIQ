using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Enums;
using SharedKernel.Caching;
using SharedKernel.Domain;

namespace Scheduling.Application.Commands.BookAppointment;

public sealed class BookAppointmentCommandHandler
{
    private readonly ISchedulingDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ISlotNotificationService _notificationService;
    private readonly ICalendarSyncService _calendarSync;
    private readonly IBookingConfirmationNotifier _confirmationNotifier;
    private readonly INoShowRiskService _riskService;
    private readonly ILogger<BookAppointmentCommandHandler> _logger;

    public BookAppointmentCommandHandler(
        ISchedulingDbContext dbContext,
        ICacheService cacheService,
        ISlotNotificationService notificationService,
        ICalendarSyncService calendarSync,
        IBookingConfirmationNotifier confirmationNotifier,
        INoShowRiskService riskService,
        ILogger<BookAppointmentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _notificationService = notificationService;
        _calendarSync = calendarSync;
        _confirmationNotifier = confirmationNotifier;
        _riskService = riskService;
        _logger = logger;
    }

    /// <summary>
    /// Books an appointment slot. Idempotent: returns existing appointment on duplicate key.
    /// Returns Failure with "CONFLICT" error when slot is already taken.
    /// </summary>
    public async Task<Result<BookAppointmentResult>> HandleAsync(
        BookAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Idempotency check via Redis cache (NFR-019)
        var idempotencyKey = $"booking-idempotency:{command.IdempotencyKey}";
        var cached = await _cacheService.GetAsync<BookAppointmentResult>(idempotencyKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogInformation("Idempotent booking hit for key {Key}", command.IdempotencyKey);
            return Result<BookAppointmentResult>.Success(cached);
        }

        // 2. Validate provider exists and is active
        var provider = await _dbContext.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == command.ProviderId && p.IsActive && !p.IsDeleted, cancellationToken);

        if (provider is null)
        {
            return Result<BookAppointmentResult>.Failure("Provider not found or inactive.");
        }

        // 3. Load slot with tracking for concurrency update
        var slot = await _dbContext.AppointmentSlots
            .FirstOrDefaultAsync(s => s.Id == command.SlotId && s.ProviderId == command.ProviderId, cancellationToken);

        if (slot is null)
        {
            return Result<BookAppointmentResult>.Failure("Slot not found.");
        }

        // 4. Check slot availability — return CONFLICT if already taken (AC-5)
        if (slot.Status != SlotStatus.Available)
        {
            return Result<BookAppointmentResult>.Failure("CONFLICT");
        }

        // 5. Book the slot — update status and create appointment
        slot.Status = SlotStatus.Booked;

        var appointment = new Scheduling.Domain.Entities.Appointment
        {
            PatientId = command.PatientId,
            ProviderId = command.ProviderId,
            SlotId = command.SlotId,
            AppointmentDateTime = slot.StartTime,
            DurationMinutes = slot.DurationMinutes,
            Status = AppointmentStatus.Confirmed,
        };

        _dbContext.Appointments.Add(appointment);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request booked this slot between our read and write (DR-009)
            _logger.LogWarning("Concurrency conflict booking slot {SlotId}", command.SlotId);
            return Result<BookAppointmentResult>.Failure("CONFLICT");
        }

        var result = new BookAppointmentResult(
            appointment.Id,
            provider.Id,
            provider.Name,
            provider.Specialty,
            provider.Location ?? string.Empty,
            slot.StartTime,
            slot.StartTime.AddMinutes(slot.DurationMinutes),
            appointment.Status.ToString());

        // 6. Cache the result for idempotency (L2 = 5 min TTL)
        await _cacheService.SetAsync(idempotencyKey, result, CacheTier.L2, cancellationToken);

        // 7. Invalidate provider search cache
        await _cacheService.RemoveByPrefixAsync("provider-search:", cancellationToken);

        // 8. Broadcast slot-booked event via SignalR
        await _notificationService.NotifySlotBookedAsync(command.ProviderId, command.SlotId, cancellationToken);

        // 9. Sync calendar events for pre-connected providers (AC-4)
        try
        {
            await _calendarSync.SyncAppointmentCreatedAsync(appointment.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            // Calendar sync failures must not break the booking flow
            _logger.LogWarning(ex,
                "Calendar auto-sync failed for appointment {AppointmentId} — event booked successfully",
                appointment.Id);
        }

        // 10. Send confirmation email with PDF attachment immediately
        try
        {
            await _confirmationNotifier.SendConfirmationAsync(appointment.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            // Email failures must not break the booking flow
            _logger.LogWarning(ex,
                "Confirmation email failed for appointment {AppointmentId} — booking still successful",
                appointment.Id);
        }

        // 11. Calculate and store no-show risk score (AC-1: on booking confirmation)
        try
        {
            var riskScore = await _riskService.CalculateAndStoreRiskAsync(
                appointment.Id, command.PatientId, cancellationToken);
            
            _logger.LogInformation(
                "Risk score {Score} calculated for appointment {AppointmentId}",
                riskScore, appointment.Id);
        }
        catch (Exception ex)
        {
            // Risk calculation failures must not break the booking flow
            _logger.LogWarning(ex,
                "Risk score calculation failed for appointment {AppointmentId} — booking still successful",
                appointment.Id);
        }

        _logger.LogInformation(
            "Appointment {AppointmentId} booked for patient {PatientId} with provider {ProviderId} at {Time}",
            appointment.Id, command.PatientId, command.ProviderId, slot.StartTime);

        return Result<BookAppointmentResult>.Success(result);
    }
}
