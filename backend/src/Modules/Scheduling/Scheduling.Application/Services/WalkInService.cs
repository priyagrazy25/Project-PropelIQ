using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Commands.WalkInBooking;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using SharedKernel.Domain;

namespace Scheduling.Application.Services;

/// <summary>
/// Handles walk-in booking: same-day slot assignment or queue enrollment (AC-4).
/// </summary>
public sealed class WalkInService : IWalkInService
{
    private const int DefaultSlotDurationMinutes = 30;

    private readonly ISchedulingDbContext _dbContext;
    private readonly IPatientLookupService _patientLookup;
    private readonly ISlotNotificationService _notificationService;
    private readonly ILogger<WalkInService> _logger;

    public WalkInService(
        ISchedulingDbContext dbContext,
        IPatientLookupService patientLookup,
        ISlotNotificationService notificationService,
        ILogger<WalkInService> logger)
    {
        _dbContext = dbContext;
        _patientLookup = patientLookup;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Result<WalkInBookingResult>> BookWalkInAsync(
        WalkInBookingCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Resolve or create patient
        Guid patientId;
        if (command.ExistingPatientId.HasValue)
        {
            var existing = await _patientLookup.GetByIdAsync(command.ExistingPatientId.Value, cancellationToken);
            if (existing is null)
            {
                return Result<WalkInBookingResult>.Failure("Patient not found.");
            }
            patientId = existing.PatientId;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(command.PatientName))
            {
                return Result<WalkInBookingResult>.Failure("Patient name is required for new walk-in patients.");
            }

            var created = await _patientLookup.CreateWalkInPatientAsync(
                command.PatientName,
                command.PatientEmail,
                command.PatientPhone,
                command.PatientDateOfBirth,
                cancellationToken);
            patientId = created.PatientId;
        }

        // 2. Validate provider exists and is active
        var provider = await _dbContext.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == command.ProviderId && p.IsActive && !p.IsDeleted, cancellationToken);

        if (provider is null)
        {
            return Result<WalkInBookingResult>.Failure("Provider not found or inactive.");
        }

        // 3. Try to find an available same-day slot
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var availableSlot = await _dbContext.AppointmentSlots
            .Where(s => s.ProviderId == command.ProviderId
                        && s.Status == SlotStatus.Available
                        && s.StartTime >= DateTime.UtcNow
                        && s.StartTime < tomorrow
                        && !s.IsDeleted)
            .OrderBy(s => s.StartTime)
            .FirstOrDefaultAsync(cancellationToken);

        // 4. Count existing same-day walk-in appointments for queue position
        var existingWalkInCount = await _dbContext.Appointments
            .CountAsync(a => a.ProviderId == command.ProviderId
                            && a.Type == AppointmentType.WalkIn
                            && a.AppointmentDateTime >= today
                            && a.AppointmentDateTime < tomorrow
                            && a.Status != AppointmentStatus.Cancelled
                            && !a.IsDeleted,
                        cancellationToken);

        var queuePosition = existingWalkInCount + 1;

        // 5. Create the appointment
        var appointment = new Appointment
        {
            PatientId = patientId,
            ProviderId = command.ProviderId,
            Type = AppointmentType.WalkIn,
            Status = AppointmentStatus.Arrived,
            Reason = command.Reason,
        };

        if (availableSlot is not null)
        {
            // Assign the slot
            availableSlot.Status = SlotStatus.Booked;
            appointment.SlotId = availableSlot.Id;
            appointment.AppointmentDateTime = availableSlot.StartTime;
            appointment.DurationMinutes = availableSlot.DurationMinutes;
        }
        else
        {
            // No slot available — queue with estimated time
            appointment.AppointmentDateTime = DateTime.UtcNow;
            appointment.DurationMinutes = DefaultSlotDurationMinutes;
        }

        _dbContext.Appointments.Add(appointment);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict during walk-in booking for provider {ProviderId}", command.ProviderId);
            return Result<WalkInBookingResult>.Failure("CONFLICT");
        }

        var estimatedWaitMinutes = queuePosition * DefaultSlotDurationMinutes;
        var isQueued = availableSlot is null;

        // 6. Broadcast queue update via SignalR
        await _notificationService.NotifyQueueUpdatedAsync(
            command.ProviderId,
            queuePosition,
            estimatedWaitMinutes,
            cancellationToken);

        _logger.LogInformation(
            "Walk-in appointment {AppointmentId} created for patient {PatientId} with provider {ProviderId}. Queue position: {Position}, Queued: {IsQueued}",
            appointment.Id, patientId, command.ProviderId, queuePosition, isQueued);

        var result = new WalkInBookingResult(
            appointment.Id,
            patientId,
            provider.Id,
            provider.Name,
            availableSlot?.StartTime,
            availableSlot?.StartTime.AddMinutes(availableSlot.DurationMinutes),
            appointment.Status.ToString(),
            queuePosition,
            estimatedWaitMinutes,
            isQueued);

        return Result<WalkInBookingResult>.Success(result);
    }
}
