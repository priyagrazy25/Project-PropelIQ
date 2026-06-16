using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notification.Application.Providers;
using Polly;
using Polly.CircuitBreaker;

namespace Notification.Infrastructure.Providers;

/// <summary>
/// Google Calendar API v3 provider. Creates, updates, and deletes calendar events
/// via REST API with Polly circuit breaker resilience.
/// </summary>
public sealed class GoogleCalendarProvider : ICalendarProvider
{
    private const string BaseUrl = "https://www.googleapis.com/calendar/v3";
    private const string CalendarId = "primary";

    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleCalendarProvider> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;

    public string ProviderName => "Google";

    public GoogleCalendarProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GoogleCalendarProvider> logger)
    {
        _httpClient = httpClientFactory.CreateClient("GoogleCalendar");
        _logger = logger;

        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(2),
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Google Calendar retry attempt {Attempt} after {Delay}ms",
                        args.AttemptNumber + 1,
                        args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                SamplingDuration = TimeSpan.FromSeconds(60),
                BreakDuration = TimeSpan.FromSeconds(120),
                OnOpened = args =>
                {
                    _logger.LogError(
                        "Google Calendar circuit breaker OPENED for {Duration}s",
                        args.BreakDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = _ =>
                {
                    _logger.LogInformation("Google Calendar circuit breaker CLOSED");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<string> CreateEventAsync(
        string accessToken,
        CalendarEventInfo eventInfo,
        CancellationToken cancellationToken = default)
    {
        return await _resiliencePipeline.ExecuteAsync(async ct =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{BaseUrl}/calendars/{CalendarId}/events");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var body = BuildGoogleEventBody(eventInfo);
            request.Content = JsonContent.Create(body);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var eventId = result.GetProperty("id").GetString()
                          ?? throw new InvalidOperationException("Google Calendar returned no event ID");

            _logger.LogInformation(
                "Google Calendar event created: {EventId} for appointment {AppointmentId}",
                eventId, eventInfo.AppointmentId);

            return eventId;
        }, cancellationToken);
    }

    public async Task UpdateEventAsync(
        string accessToken,
        string externalEventId,
        CalendarEventInfo eventInfo,
        CancellationToken cancellationToken = default)
    {
        await _resiliencePipeline.ExecuteAsync(async ct =>
        {
            var request = new HttpRequestMessage(HttpMethod.Put,
                $"{BaseUrl}/calendars/{CalendarId}/events/{externalEventId}");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var body = BuildGoogleEventBody(eventInfo);
            request.Content = JsonContent.Create(body);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Google Calendar event updated: {EventId} for appointment {AppointmentId}",
                externalEventId, eventInfo.AppointmentId);
        }, cancellationToken);
    }

    public async Task DeleteEventAsync(
        string accessToken,
        string externalEventId,
        CancellationToken cancellationToken = default)
    {
        await _resiliencePipeline.ExecuteAsync(async ct =>
        {
            var request = new HttpRequestMessage(HttpMethod.Delete,
                $"{BaseUrl}/calendars/{CalendarId}/events/{externalEventId}");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, ct);

            // 410 Gone is acceptable — event already deleted
            if (response.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                _logger.LogInformation(
                    "Google Calendar event already deleted: {EventId}", externalEventId);
                return;
            }

            response.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Google Calendar event deleted: {EventId}", externalEventId);
        }, cancellationToken);
    }

    private static object BuildGoogleEventBody(CalendarEventInfo info)
    {
        return new
        {
            summary = $"Appointment with {info.ProviderName}",
            description = info.Notes ?? $"Medical appointment — {info.ProviderName}",
            location = info.Location,
            start = new { dateTime = info.StartTime.ToString("o"), timeZone = "UTC" },
            end = new { dateTime = info.EndTime.ToString("o"), timeZone = "UTC" },
        };
    }
}
