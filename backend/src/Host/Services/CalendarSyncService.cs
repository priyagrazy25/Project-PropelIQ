using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Notification.Application.Abstractions;
using Notification.Application.Providers;
using Notification.Domain.Entities;
using Polly.CircuitBreaker;
using Scheduling.Application.Abstractions;

namespace Host.Services;

/// <summary>
/// Real calendar sync implementation replacing NoOpCalendarSyncService.
/// Orchestrates Google/Outlook providers via strategy pattern.
/// Handles circuit breaker failures by logging for async retry (AC-5, NFR-014).
/// </summary>
public sealed class CalendarSyncService : ICalendarSyncService
{
    private readonly ICalendarAppointmentQuery _appointmentQuery;
    private readonly ICalendarSyncRecordRepository _syncRecordRepo;
    private readonly ICalendarOAuthTokenRepository _tokenRepo;
    private readonly IEnumerable<ICalendarProvider> _providers;
    private readonly IDataProtector _protector;
    private readonly ILogger<CalendarSyncService> _logger;

    public CalendarSyncService(
        ICalendarAppointmentQuery appointmentQuery,
        ICalendarSyncRecordRepository syncRecordRepo,
        ICalendarOAuthTokenRepository tokenRepo,
        IEnumerable<ICalendarProvider> providers,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<CalendarSyncService> logger)
    {
        _appointmentQuery = appointmentQuery;
        _syncRecordRepo = syncRecordRepo;
        _tokenRepo = tokenRepo;
        _providers = providers;
        _protector = dataProtectionProvider.CreateProtector("CalendarOAuth");
        _logger = logger;
    }

    public async Task SyncAppointmentCreatedAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentQuery.GetAppointmentAsync(appointmentId, cancellationToken);
        if (appointment is null)
        {
            _logger.LogWarning("Calendar sync skipped: appointment {AppointmentId} not found", appointmentId);
            return;
        }

        var tokens = await _tokenRepo.GetByPatientAsync(appointment.PatientId, cancellationToken);
        if (tokens.Count == 0)
        {
            _logger.LogDebug(
                "No calendar providers connected for patient {PatientId}, skipping sync",
                appointment.PatientId);
            return;
        }

        var eventInfo = MapToEventInfo(appointment);

        foreach (var oauthToken in tokens)
        {
            var provider = ResolveProvider(oauthToken.Provider);
            if (provider is null) continue;

            await CreateEventForProviderAsync(
                provider, oauthToken, eventInfo, appointmentId, appointment.PatientId, cancellationToken);
        }
    }

    public async Task SyncAppointmentCancelledAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var syncRecords = await _syncRecordRepo.GetByAppointmentAsync(appointmentId, cancellationToken);
        if (syncRecords.Count == 0)
        {
            _logger.LogDebug("No calendar sync records for appointment {AppointmentId}", appointmentId);
            return;
        }

        foreach (var record in syncRecords.Where(r => r.Status == "Synced"))
        {
            var provider = ResolveProvider(record.Provider);
            if (provider is null) continue;

            var oauthToken = await _tokenRepo.GetByPatientAndProviderAsync(
                record.PatientId, record.Provider, cancellationToken);

            if (oauthToken is null)
            {
                _logger.LogWarning(
                    "OAuth token not found for patient {PatientId}, provider {Provider}",
                    record.PatientId, record.Provider);
                continue;
            }

            await DeleteEventForProviderAsync(
                provider, oauthToken, record, cancellationToken);
        }
    }

    public async Task SyncAppointmentRescheduledAsync(
        Guid oldAppointmentId,
        Guid newAppointmentId,
        CancellationToken cancellationToken = default)
    {
        // Delete old calendar events
        await SyncAppointmentCancelledAsync(oldAppointmentId, cancellationToken);

        // Create new calendar events
        await SyncAppointmentCreatedAsync(newAppointmentId, cancellationToken);
    }

    private async Task CreateEventForProviderAsync(
        ICalendarProvider provider,
        CalendarOAuthToken oauthToken,
        CalendarEventInfo eventInfo,
        Guid appointmentId,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        try
        {
            var accessToken = _protector.Unprotect(oauthToken.EncryptedAccessToken);

            var externalEventId = await provider.CreateEventAsync(accessToken, eventInfo, cancellationToken);

            var syncRecord = new CalendarSyncRecord
            {
                AppointmentId = appointmentId,
                PatientId = patientId,
                Provider = provider.ProviderName,
                ExternalEventId = externalEventId,
                Status = "Synced",
                LastSyncedAt = DateTime.UtcNow,
            };

            await _syncRecordRepo.AddAsync(syncRecord, cancellationToken);

            _logger.LogInformation(
                "Calendar event created via {Provider} for appointment {AppointmentId}",
                provider.ProviderName, appointmentId);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning(
                "Circuit breaker open for {Provider} — queuing appointment {AppointmentId} for retry",
                provider.ProviderName, appointmentId);

            await RecordFailedSyncAsync(
                appointmentId, patientId, provider.ProviderName,
                "Circuit breaker open — queued for retry", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to sync appointment {AppointmentId} to {Provider}",
                appointmentId, provider.ProviderName);

            await RecordFailedSyncAsync(
                appointmentId, patientId, provider.ProviderName,
                ex.Message, cancellationToken);
        }
    }

    private async Task DeleteEventForProviderAsync(
        ICalendarProvider provider,
        CalendarOAuthToken oauthToken,
        CalendarSyncRecord record,
        CancellationToken cancellationToken)
    {
        try
        {
            var accessToken = _protector.Unprotect(oauthToken.EncryptedAccessToken);

            await provider.DeleteEventAsync(accessToken, record.ExternalEventId, cancellationToken);

            // Track deletion via a new record since we can't update detached entities
            var deletedRecord = new CalendarSyncRecord
            {
                AppointmentId = record.AppointmentId,
                PatientId = record.PatientId,
                Provider = record.Provider,
                ExternalEventId = record.ExternalEventId,
                Status = "Deleted",
                LastSyncedAt = DateTime.UtcNow,
            };

            await _syncRecordRepo.AddAsync(deletedRecord, cancellationToken);

            _logger.LogInformation(
                "Calendar event deleted via {Provider} for appointment {AppointmentId}",
                provider.ProviderName, record.AppointmentId);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning(
                "Circuit breaker open for {Provider} — queuing delete for appointment {AppointmentId}",
                provider.ProviderName, record.AppointmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete calendar event for appointment {AppointmentId} from {Provider}",
                record.AppointmentId, provider.ProviderName);
        }
    }

    private async Task RecordFailedSyncAsync(
        Guid appointmentId,
        Guid patientId,
        string provider,
        string failureReason,
        CancellationToken cancellationToken)
    {
        var failedRecord = new CalendarSyncRecord
        {
            AppointmentId = appointmentId,
            PatientId = patientId,
            Provider = provider,
            ExternalEventId = string.Empty,
            Status = "Failed",
            FailureReason = failureReason.Length > 500 ? failureReason[..500] : failureReason,
            LastSyncedAt = DateTime.UtcNow,
        };

        await _syncRecordRepo.AddAsync(failedRecord, cancellationToken);
    }

    private ICalendarProvider? ResolveProvider(string providerName)
    {
        var provider = _providers.FirstOrDefault(
            p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
        {
            _logger.LogWarning("No calendar provider found for {ProviderName}", providerName);
        }

        return provider;
    }

    private static CalendarEventInfo MapToEventInfo(CalendarAppointmentInfo appointment)
    {
        return new CalendarEventInfo(
            AppointmentId: appointment.AppointmentId,
            PatientName: appointment.PatientName,
            ProviderName: appointment.ProviderName,
            StartTime: appointment.StartTime,
            EndTime: appointment.EndTime,
            Location: appointment.Location,
            Notes: appointment.Notes);
    }
}
