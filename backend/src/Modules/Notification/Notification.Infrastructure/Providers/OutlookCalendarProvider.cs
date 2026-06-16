using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notification.Application.Providers;
using Polly;
using Polly.CircuitBreaker;

namespace Notification.Infrastructure.Providers;

/// <summary>
/// Microsoft Graph v1.0 calendar provider. Creates, updates, and deletes
/// Outlook calendar events via REST API with Polly circuit breaker resilience.
/// </summary>
public sealed class OutlookCalendarProvider : ICalendarProvider
{
    private const string BaseUrl = "https://graph.microsoft.com/v1.0/me";

    private readonly HttpClient _httpClient;
    private readonly ILogger<OutlookCalendarProvider> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;

    public string ProviderName => "Outlook";

    public OutlookCalendarProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OutlookCalendarProvider> logger)
    {
        _httpClient = httpClientFactory.CreateClient("OutlookCalendar");
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
                        "Outlook Calendar retry attempt {Attempt} after {Delay}ms",
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
                        "Outlook Calendar circuit breaker OPENED for {Duration}s",
                        args.BreakDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = _ =>
                {
                    _logger.LogInformation("Outlook Calendar circuit breaker CLOSED");
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
            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/events");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var body = BuildOutlookEventBody(eventInfo);
            request.Content = JsonContent.Create(body);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var eventId = result.GetProperty("id").GetString()
                          ?? throw new InvalidOperationException("Microsoft Graph returned no event ID");

            _logger.LogInformation(
                "Outlook Calendar event created: {EventId} for appointment {AppointmentId}",
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
            var request = new HttpRequestMessage(HttpMethod.Patch,
                $"{BaseUrl}/events/{externalEventId}");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var body = BuildOutlookEventBody(eventInfo);
            request.Content = JsonContent.Create(body);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Outlook Calendar event updated: {EventId} for appointment {AppointmentId}",
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
                $"{BaseUrl}/events/{externalEventId}");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, ct);

            // 404 is acceptable — event already deleted
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation(
                    "Outlook Calendar event already deleted: {EventId}", externalEventId);
                return;
            }

            response.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Outlook Calendar event deleted: {EventId}", externalEventId);
        }, cancellationToken);
    }

    private static object BuildOutlookEventBody(CalendarEventInfo info)
    {
        return new
        {
            subject = $"Appointment with {info.ProviderName}",
            body = new
            {
                contentType = "Text",
                content = info.Notes ?? $"Medical appointment — {info.ProviderName}"
            },
            start = new { dateTime = info.StartTime.ToString("o"), timeZone = "UTC" },
            end = new { dateTime = info.EndTime.ToString("o"), timeZone = "UTC" },
            location = new { displayName = info.Location ?? string.Empty },
        };
    }
}
