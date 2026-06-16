using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Application.DTOs;
using Scheduling.Domain.Enums;
using SharedKernel.Domain;

namespace Scheduling.Application.Services;

public sealed class QueueService : IQueueService
{
    private readonly ISchedulingDbContext _dbContext;
    private readonly IPatientLookupService _patientLookup;
    private readonly ISlotNotificationService _notificationService;
    private readonly ILogger<QueueService> _logger;

    // Valid status transitions for queue management
    private static readonly Dictionary<AppointmentStatus, HashSet<AppointmentStatus>> ValidTransitions = new()
    {
        [AppointmentStatus.Scheduled] = new() { AppointmentStatus.Arrived, AppointmentStatus.Cancelled, AppointmentStatus.NoShow },
        [AppointmentStatus.Confirmed] = new() { AppointmentStatus.Arrived, AppointmentStatus.Cancelled, AppointmentStatus.NoShow },
        [AppointmentStatus.Arrived] = new() { AppointmentStatus.InProgress, AppointmentStatus.Cancelled, AppointmentStatus.NoShow },
        [AppointmentStatus.InProgress] = new() { AppointmentStatus.Completed, AppointmentStatus.Cancelled, AppointmentStatus.NoShow },
    };

    // Map queue-friendly status names to AppointmentStatus
    private static readonly Dictionary<string, AppointmentStatus> StatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Waiting"] = AppointmentStatus.Arrived,
        ["InProgress"] = AppointmentStatus.InProgress,
        ["Completed"] = AppointmentStatus.Completed,
        ["Left"] = AppointmentStatus.Cancelled,
        ["NoShow"] = AppointmentStatus.NoShow,
    };

    public QueueService(
        ISchedulingDbContext dbContext,
        IPatientLookupService patientLookup,
        ISlotNotificationService notificationService,
        ILogger<QueueService> logger)
    {
        _dbContext = dbContext;
        _patientLookup = patientLookup;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<QueueEntryDto>> GetTodayQueueAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var appointments = await _dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Provider)
            .Where(a => a.AppointmentDateTime >= today
                        && a.AppointmentDateTime < tomorrow
                        && a.Status != AppointmentStatus.Cancelled
                        && a.Status != AppointmentStatus.Rescheduled
                        && !a.IsDeleted)
            .OrderBy(a => a.AppointmentDateTime)
            .Select(a => new
            {
                a.Id,
                a.PatientId,
                a.Type,
                a.Status,
                a.ArrivedAt,
                ProviderName = a.Provider != null ? a.Provider.Name : "Unknown Provider",
                a.RowVersion,
            })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var result = new List<QueueEntryDto>(appointments.Count);
        var position = 0;

        foreach (var a in appointments)
        {
            position++;
            var patient = await _patientLookup.GetByIdAsync(a.PatientId, cancellationToken);
            var patientName = patient?.FullName ?? "Unknown Patient";
            var waitMinutes = a.ArrivedAt.HasValue ? (int)(now - a.ArrivedAt.Value).TotalMinutes : 0;
            if (waitMinutes < 0) waitMinutes = 0;

            result.Add(new QueueEntryDto(
                a.Id,
                a.PatientId,
                patientName,
                a.Type.ToString(),
                a.ProviderName,
                MapToQueueStatus(a.Status),
                a.ArrivedAt,
                waitMinutes,
                position,
                Convert.ToBase64String(a.RowVersion)));
        }

        return result;
    }

    public async Task<Result<QueueEntryDto>> UpdateStatusAsync(
        Guid appointmentId,
        string newStatus,
        byte[] rowVersion,
        Guid staffActorId,
        string staffActorName,
        CancellationToken cancellationToken = default)
    {
        if (!StatusMap.TryGetValue(newStatus, out var targetStatus))
        {
            return Result<QueueEntryDto>.Failure($"Invalid status '{newStatus}'. Valid values: Waiting, InProgress, Completed, Left, NoShow.");
        }

        var appointment = await _dbContext.Appointments
            .Include(a => a.Provider)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && !a.IsDeleted, cancellationToken);

        if (appointment is null)
        {
            return Result<QueueEntryDto>.Failure("Appointment not found.");
        }

        // Validate state transition
        if (!ValidTransitions.TryGetValue(appointment.Status, out var allowed) || !allowed.Contains(targetStatus))
        {
            return Result<QueueEntryDto>.Failure(
                $"Invalid status transition from '{MapToQueueStatus(appointment.Status)}' to '{newStatus}'.");
        }

        var previousStatus = appointment.Status;
        appointment.Status = targetStatus;

        // Set concurrency token for optimistic concurrency
        _dbContext.SetRowVersion(appointment, rowVersion);

        // Create immutable audit log entry (DR-011, AC-5)
        var auditLog = new AuditLog
        {
            ActorId = staffActorId,
            ActorName = staffActorName,
            Action = "QueueStatusChanged",
            Resource = "Appointment",
            ResourceId = appointmentId.ToString(),
            BeforeState = MapToQueueStatus(previousStatus),
            AfterState = MapToQueueStatus(targetStatus),
        };
        _dbContext.AuditLogs.Add(auditLog);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict updating queue status for appointment {AppointmentId}", appointmentId);
            return Result<QueueEntryDto>.Failure("CONFLICT");
        }

        // Broadcast via SignalR (AC-2: within 500ms)
        await _notificationService.NotifyQueueStatusChangedAsync(
            appointmentId,
            MapToQueueStatus(targetStatus),
            cancellationToken);

        // Build response
        var patient = await _patientLookup.GetByIdAsync(appointment.PatientId, cancellationToken);
        var now = DateTime.UtcNow;
        var waitMinutes = appointment.ArrivedAt.HasValue ? (int)(now - appointment.ArrivedAt.Value).TotalMinutes : 0;
        if (waitMinutes < 0) waitMinutes = 0;

        var dto = new QueueEntryDto(
            appointment.Id,
            appointment.PatientId,
            patient?.FullName ?? "Unknown Patient",
            appointment.Type.ToString(),
            appointment.Provider?.Name ?? "Unknown Provider",
            MapToQueueStatus(targetStatus),
            appointment.ArrivedAt,
            waitMinutes,
            0, // position recalculated on full queue fetch
            Convert.ToBase64String(appointment.RowVersion));

        _logger.LogInformation(
            "Queue status updated: Appointment {AppointmentId} from {From} to {To} by {Actor}",
            appointmentId, MapToQueueStatus(previousStatus), MapToQueueStatus(targetStatus), staffActorName);

        return Result<QueueEntryDto>.Success(dto);
    }

    public async Task<Result<QueueEntryDto>> MarkArrivedAsync(
        Guid appointmentId,
        Guid staffActorId,
        string staffActorName,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _dbContext.Appointments
            .Include(a => a.Provider)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && !a.IsDeleted, cancellationToken);

        if (appointment is null)
        {
            return Result<QueueEntryDto>.Failure("Appointment not found.");
        }

        // Reject cancelled appointments
        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            return Result<QueueEntryDto>.Failure("Cannot mark a cancelled appointment as arrived.");
        }

        // Reject already arrived or later-state appointments
        if (appointment.Status == AppointmentStatus.Arrived
            || appointment.Status == AppointmentStatus.InProgress
            || appointment.Status == AppointmentStatus.Completed)
        {
            return Result<QueueEntryDto>.Failure("Appointment is already arrived or completed.");
        }

        // Validate same-day only — reject future-date appointments
        var todayUtc = DateTime.UtcNow.Date;
        if (appointment.AppointmentDateTime.Date != todayUtc)
        {
            return Result<QueueEntryDto>.Failure("Arrival can only be marked for same-day appointments.");
        }

        var arrivedAt = DateTime.UtcNow;
        var previousStatus = appointment.Status;
        appointment.Status = AppointmentStatus.Arrived;
        appointment.ArrivedAt = arrivedAt;

        // Create immutable audit log entry (DR-011, AC-5)
        var auditLog = new AuditLog
        {
            ActorId = staffActorId,
            ActorName = staffActorName,
            Action = "PatientArrived",
            Resource = "Appointment",
            ResourceId = appointmentId.ToString(),
            BeforeState = previousStatus.ToString(),
            AfterState = AppointmentStatus.Arrived.ToString(),
        };
        _dbContext.AuditLogs.Add(auditLog);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Broadcast patient-arrived event via SignalR (AC-4)
        await _notificationService.NotifyPatientArrivedAsync(
            appointmentId,
            appointment.PatientId,
            cancellationToken);

        var patient = await _patientLookup.GetByIdAsync(appointment.PatientId, cancellationToken);

        var dto = new QueueEntryDto(
            appointment.Id,
            appointment.PatientId,
            patient?.FullName ?? "Unknown Patient",
            appointment.Type.ToString(),
            appointment.Provider?.Name ?? "Unknown Provider",
            MapToQueueStatus(appointment.Status),
            arrivedAt,
            0, // just arrived, no wait time yet
            0, // position recalculated on full queue fetch
            Convert.ToBase64String(appointment.RowVersion));

        _logger.LogInformation(
            "Patient arrival marked: Appointment {AppointmentId} by {Actor} at {ArrivedAt}",
            appointmentId, staffActorName, arrivedAt);

        return Result<QueueEntryDto>.Success(dto);
    }

    public async Task<QueueEntryDto?> GetPatientQueueEntryAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // Find today's appointment for this patient that is in Waiting or InProgress state
        var appointment = await _dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Provider)
            .Where(a => a.PatientId == patientId
                        && a.AppointmentDateTime >= today
                        && a.AppointmentDateTime < tomorrow
                        && (a.Status == AppointmentStatus.Arrived || a.Status == AppointmentStatus.InProgress)
                        && !a.IsDeleted)
            .OrderBy(a => a.AppointmentDateTime)
            .Select(a => new
            {
                a.Id,
                a.PatientId,
                a.Type,
                a.Status,
                a.ArrivedAt,
                ProviderName = a.Provider != null ? a.Provider.Name : "Unknown Provider",
                a.RowVersion,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (appointment is null)
        {
            return null;
        }

        // Calculate position in queue (how many Waiting patients are ahead)
        var waitingAhead = await _dbContext.Appointments
            .AsNoTracking()
            .CountAsync(a => a.AppointmentDateTime >= today
                             && a.AppointmentDateTime < tomorrow
                             && a.Status == AppointmentStatus.Arrived
                             && a.ArrivedAt < appointment.ArrivedAt
                             && !a.IsDeleted, cancellationToken);

        var now = DateTime.UtcNow;
        var patient = await _patientLookup.GetByIdAsync(patientId, cancellationToken);
        var waitMinutes = appointment.ArrivedAt.HasValue ? (int)(now - appointment.ArrivedAt.Value).TotalMinutes : 0;
        if (waitMinutes < 0) waitMinutes = 0;

        return new QueueEntryDto(
            appointment.Id,
            appointment.PatientId,
            patient?.FullName ?? "Unknown Patient",
            appointment.Type.ToString(),
            appointment.ProviderName,
            MapToQueueStatus(appointment.Status),
            appointment.ArrivedAt,
            waitMinutes,
            waitingAhead + 1, // Position (1-based)
            Convert.ToBase64String(appointment.RowVersion));
    }

    private static string MapToQueueStatus(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Arrived => "Waiting",
        AppointmentStatus.InProgress => "InProgress",
        AppointmentStatus.Completed => "Completed",
        AppointmentStatus.Cancelled => "Left",
        AppointmentStatus.NoShow => "NoShow",
        _ => status.ToString(),
    };
}
