using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;

namespace Scheduling.Application.Services;

/// <summary>
/// No-op calendar sync. Logs operations until Google Calendar / Microsoft Graph
/// credentials are configured. Replace with real implementation when ready.
/// </summary>
public sealed class NoOpCalendarSyncService : ICalendarSyncService
{
    private readonly ILogger<NoOpCalendarSyncService> _logger;

    public NoOpCalendarSyncService(ILogger<NoOpCalendarSyncService> logger)
    {
        _logger = logger;
    }

    public Task SyncAppointmentCreatedAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Calendar sync stub: appointment {AppointmentId} created", appointmentId);
        return Task.CompletedTask;
    }

    public Task SyncAppointmentCancelledAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Calendar sync stub: appointment {AppointmentId} cancelled", appointmentId);
        return Task.CompletedTask;
    }

    public Task SyncAppointmentRescheduledAsync(Guid oldAppointmentId, Guid newAppointmentId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Calendar sync stub: appointment {OldId} rescheduled to {NewId}", oldAppointmentId, newAppointmentId);
        return Task.CompletedTask;
    }
}
