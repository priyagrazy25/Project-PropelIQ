using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.Application.Abstractions;
using Notification.Application.Providers;
using Notification.Domain.Entities;
using Scheduling.Application.Abstractions;

namespace Host.Controllers;

/// <summary>
/// Triggers calendar sync for an appointment after OAuth tokens have been stored.
/// Lives in Host because it bridges Scheduling and Notification modules.
/// Uses the fresh OAuth token from the request to create the calendar event directly,
/// bypassing the DB token lookup chain which can silently fail.
/// </summary>
[ApiController]
[Route("api/scheduling/calendar")]
[Authorize]
public class CalendarSyncTriggerController : ControllerBase
{
    private readonly ICalendarAppointmentQuery _appointmentQuery;
    private readonly IEnumerable<ICalendarProvider> _providers;
    private readonly ICalendarSyncRecordRepository _syncRecordRepo;
    private readonly ILogger<CalendarSyncTriggerController> _logger;

    public CalendarSyncTriggerController(
        ICalendarAppointmentQuery appointmentQuery,
        IEnumerable<ICalendarProvider> providers,
        ICalendarSyncRecordRepository syncRecordRepo,
        ILogger<CalendarSyncTriggerController> logger)
    {
        _appointmentQuery = appointmentQuery;
        _providers = providers;
        _syncRecordRepo = syncRecordRepo;
        _logger = logger;
    }

    /// <summary>
    /// Creates a calendar event for an appointment using the fresh OAuth token
    /// obtained from the frontend OAuth callback.
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncAppointment(
        [FromBody] SyncRequest request,
        CancellationToken cancellationToken)
    {
        var patientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(patientId))
            return Unauthorized();

        if (!Guid.TryParse(request.AppointmentId, out var appointmentGuid))
            return BadRequest(new SyncResponse
            {
                Status = "failed",
                Provider = request.Provider,
                Message = "Invalid appointmentId format."
            });

        if (string.IsNullOrWhiteSpace(request.OauthToken))
            return BadRequest(new SyncResponse
            {
                Status = "failed",
                Provider = request.Provider,
                Message = "OAuth token is required."
            });

        // 1. Load appointment details
        var appointment = await _appointmentQuery.GetAppointmentAsync(appointmentGuid, cancellationToken);
        if (appointment is null)
        {
            _logger.LogWarning("Calendar sync: appointment {AppointmentId} not found", appointmentGuid);
            return NotFound(new SyncResponse
            {
                Status = "failed",
                Provider = request.Provider,
                Message = "Appointment not found."
            });
        }

        // 2. Resolve the calendar provider (Google / Outlook)
        var provider = _providers.FirstOrDefault(
            p => p.ProviderName.Equals(request.Provider, StringComparison.OrdinalIgnoreCase));
        if (provider is null)
        {
            return BadRequest(new SyncResponse
            {
                Status = "failed",
                Provider = request.Provider,
                Message = $"Unsupported calendar provider: {request.Provider}"
            });
        }

        // 3. Build event info from appointment
        var eventInfo = new CalendarEventInfo(
            AppointmentId: appointment.AppointmentId,
            PatientName: appointment.PatientName,
            ProviderName: appointment.ProviderName,
            StartTime: appointment.StartTime,
            EndTime: appointment.EndTime,
            Location: appointment.Location,
            Notes: appointment.Notes);

        // 4. Create the calendar event using the fresh OAuth token directly
        string externalEventId;
        try
        {
            externalEventId = await provider.CreateEventAsync(
                request.OauthToken, eventInfo, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create {Provider} calendar event for appointment {AppointmentId}",
                request.Provider, appointmentGuid);

            return StatusCode(503, new SyncResponse
            {
                SyncId = Guid.NewGuid().ToString(),
                Status = "failed",
                Provider = request.Provider,
                Message = "Failed to create calendar event. Please try again."
            });
        }

        // 5. Record the successful sync
        var syncRecord = new CalendarSyncRecord
        {
            AppointmentId = appointmentGuid,
            PatientId = appointment.PatientId,
            Provider = provider.ProviderName,
            ExternalEventId = externalEventId,
            Status = "Synced",
            LastSyncedAt = DateTime.UtcNow,
        };

        await _syncRecordRepo.AddAsync(syncRecord, cancellationToken);

        _logger.LogInformation(
            "Calendar event {EventId} created via {Provider} for appointment {AppointmentId}",
            externalEventId, provider.ProviderName, appointmentGuid);

        return Ok(new SyncResponse
        {
            SyncId = syncRecord.Id.ToString(),
            Status = "synced",
            Provider = provider.ProviderName,
            Message = $"{provider.ProviderName} Calendar event created successfully."
        });
    }

    public sealed class SyncRequest
    {
        [JsonPropertyName("appointmentId")]
        public string AppointmentId { get; init; } = string.Empty;

        [JsonPropertyName("provider")]
        public string Provider { get; init; } = string.Empty;

        [JsonPropertyName("oauthToken")]
        public string? OauthToken { get; init; }
    }

    public sealed class SyncResponse
    {
        [JsonPropertyName("syncId")]
        public string SyncId { get; init; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;

        [JsonPropertyName("provider")]
        public string Provider { get; init; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;
    }
}
