namespace Notification.Application.Providers;

/// <summary>
/// Strategy interface for external calendar providers (Google Calendar, Outlook).
/// Each provider implementation handles event CRUD via its respective API.
/// </summary>
public interface ICalendarProvider
{
    string ProviderName { get; }

    Task<string> CreateEventAsync(
        string accessToken,
        CalendarEventInfo eventInfo,
        CancellationToken cancellationToken = default);

    Task UpdateEventAsync(
        string accessToken,
        string externalEventId,
        CalendarEventInfo eventInfo,
        CancellationToken cancellationToken = default);

    Task DeleteEventAsync(
        string accessToken,
        string externalEventId,
        CancellationToken cancellationToken = default);
}
