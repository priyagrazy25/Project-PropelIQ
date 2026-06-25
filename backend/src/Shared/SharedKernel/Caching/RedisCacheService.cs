using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SharedKernel.Caching;

/// <summary>
/// Redis-backed cache service for Upstash Redis (HTTPS-only, AD-009).
/// Supports 3-tier TTL strategy and event-driven invalidation.
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>((string)value!, SerializerOptions);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis connection failed during GET for key {Key}. Returning null.", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, CacheTier tier, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var db = _redis.GetDatabase();
            var serialized = JsonSerializer.Serialize(value, SerializerOptions);
            await db.StringSetAsync(key, serialized, tier.GetTtl());
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis connection failed during SET for key {Key}. Value not cached.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis connection failed during DELETE for key {Key}.", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefixPattern, CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var endpoint in _redis.GetEndPoints())
            {
                var server = _redis.GetServer(endpoint);
                await foreach (var key in server.KeysAsync(pattern: $"{prefixPattern}*"))
                {
                    await _redis.GetDatabase().KeyDeleteAsync(key);
                }
            }
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis connection failed during prefix DELETE for pattern {Pattern}.", prefixPattern);
        }
    }
}
