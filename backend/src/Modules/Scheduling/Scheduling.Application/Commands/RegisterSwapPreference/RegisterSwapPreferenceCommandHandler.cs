using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using SharedKernel.Domain;

namespace Scheduling.Application.Commands.RegisterSwapPreference;

public sealed class RegisterSwapPreferenceCommandHandler
{
    private readonly ISchedulingDbContext _dbContext;
    private readonly ILogger<RegisterSwapPreferenceCommandHandler> _logger;

    public RegisterSwapPreferenceCommandHandler(
        ISchedulingDbContext dbContext,
        ILogger<RegisterSwapPreferenceCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<RegisterSwapPreferenceResult>> HandleAsync(
        RegisterSwapPreferenceCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate appointment belongs to patient and is active
        var appointment = await _dbContext.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.Id == command.AppointmentId
                     && a.PatientId == command.PatientId
                     && !a.IsDeleted,
                cancellationToken);

        if (appointment is null)
        {
            return Result<RegisterSwapPreferenceResult>.Failure("Appointment not found or does not belong to patient.");
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            return Result<RegisterSwapPreferenceResult>.Failure("Cannot register swap for a cancelled appointment.");
        }

        // 2. Validate desired slot exists and is currently unavailable (booked)
        var desiredSlot = await _dbContext.AppointmentSlots
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Id == command.DesiredSlotId && s.ProviderId == appointment.ProviderId,
                cancellationToken);

        if (desiredSlot is null)
        {
            return Result<RegisterSwapPreferenceResult>.Failure("Desired slot not found for this provider.");
        }

        if (desiredSlot.Status == SlotStatus.Available)
        {
            return Result<RegisterSwapPreferenceResult>.Failure("Desired slot is already available. Book it directly instead.");
        }

        // 3. Check for duplicate pending swap for same appointment
        var existingSwap = await _dbContext.PreferredSlotSwaps
            .AnyAsync(
                s => s.OriginalAppointmentId == command.AppointmentId
                     && s.Status == SwapStatus.Pending
                     && !s.IsDeleted,
                cancellationToken);

        if (existingSwap)
        {
            return Result<RegisterSwapPreferenceResult>.Failure("A pending swap preference already exists for this appointment.");
        }

        // 4. Determine FIFO priority (max priority + 1 for the desired slot)
        var maxPriority = await _dbContext.PreferredSlotSwaps
            .Where(s => s.DesiredSlotId == command.DesiredSlotId && s.Status == SwapStatus.Pending)
            .Select(s => (int?)s.Priority)
            .MaxAsync(cancellationToken) ?? 0;

        var swap = new PreferredSlotSwap
        {
            RequestingPatientId = command.PatientId,
            OriginalAppointmentId = command.AppointmentId,
            DesiredSlotId = command.DesiredSlotId,
            Status = SwapStatus.Pending,
            Priority = maxPriority + 1,
            RequestedAt = DateTime.UtcNow,
        };

        _dbContext.PreferredSlotSwaps.Add(swap);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Swap preference {SwapId} registered for patient {PatientId}, appointment {AppointmentId}, desired slot {SlotId}, priority {Priority}",
            swap.Id, command.PatientId, command.AppointmentId, command.DesiredSlotId, swap.Priority);

        return Result<RegisterSwapPreferenceResult>.Success(
            new RegisterSwapPreferenceResult(
                swap.Id,
                swap.OriginalAppointmentId,
                swap.DesiredSlotId,
                swap.Priority,
                swap.Status.ToString()));
    }
}
