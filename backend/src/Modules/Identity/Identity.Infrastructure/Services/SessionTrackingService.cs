using Identity.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Identity.Infrastructure.Services;

/// <summary>
/// Tracks active user sessions using Redis (primary) with in-memory fallback.
/// Each session key has a TTL matching the inactivity timeout (15 minutes).
/// </summary>
public sealed class SessionTrackingService : ISessionTrackingService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<SessionTrackingService> _logger;

    private const string KeyPrefix = "session:";

    public SessionTrackingService(
        IMemoryCache memoryCache,
        ILogger<SessionTrackingService> logger,
        IConnectionMultiplexer? redis = null)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _redis = redis;
    }

    public async Task TrackSessionAsync(Guid userId, string? deviceId, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(userId, deviceId);

        if (_redis is not null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                await db.StringSetAsync(key, "active", ttl);
                return;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for session tracking; falling back to in-memory");
            }
        }

        _memoryCache.Set(key, "active", ttl);
    }

    public async Task<bool> IsSessionActiveAsync(Guid userId, string? deviceId, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(userId, deviceId);

        if (_redis is not null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                return await db.KeyExistsAsync(key);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for session check; falling back to in-memory");
            }
        }

        return _memoryCache.TryGetValue(key, out _);
    }

    public async Task InvalidateAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var pattern = $"{KeyPrefix}{userId}:*";

        if (_redis is not null && _redis.IsConnected)
        {
            try
            {
                foreach (var endpoint in _redis.GetEndPoints())
                {
                    var server = _redis.GetServer(endpoint);
                    var db = _redis.GetDatabase();

                    await foreach (var key in server.KeysAsync(pattern: pattern))
                    {
                        await db.KeyDeleteAsync(key);
                    }
                }
                return;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for session invalidation");
            }
        }

        // In-memory fallback: cannot enumerate keys by prefix; tracked sessions will expire naturally
        _logger.LogDebug("In-memory cache does not support prefix-based invalidation; sessions will expire by TTL");
    }

    private static string BuildKey(Guid userId, string? deviceId)
    {
        return string.IsNullOrEmpty(deviceId)
            ? $"{KeyPrefix}{userId}:default"
            : $"{KeyPrefix}{userId}:{deviceId}";
    }
}
