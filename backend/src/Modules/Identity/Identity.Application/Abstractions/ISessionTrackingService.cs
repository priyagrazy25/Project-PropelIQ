namespace Identity.Application.Abstractions;

/// <summary>
/// Tracks active user sessions with TTL-based expiration.
/// </summary>
public interface ISessionTrackingService
{
    /// <summary>Records or extends a session for the given user/device.</summary>
    Task TrackSessionAsync(Guid userId, string? deviceId, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>Checks whether a session is still active.</summary>
    Task<bool> IsSessionActiveAsync(Guid userId, string? deviceId, CancellationToken cancellationToken = default);

    /// <summary>Removes all sessions for the given user.</summary>
    Task InvalidateAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
