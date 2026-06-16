using Identity.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Identity.Infrastructure.Services;

/// <summary>
/// Blacklists invalidated JWT access tokens (by jti claim) until their natural expiry.
/// Uses Redis (primary) with in-memory fallback.
/// </summary>
public sealed class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<TokenBlacklistService> _logger;

    private const string KeyPrefix = "blacklist:jti:";

    public TokenBlacklistService(
        IMemoryCache memoryCache,
        ILogger<TokenBlacklistService> logger,
        IConnectionMultiplexer? redis = null)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _redis = redis;
    }

    public async Task BlacklistTokenAsync(string jti, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}{jti}";

        if (_redis is not null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                await db.StringSetAsync(key, "revoked", ttl);
                return;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for token blacklisting; falling back to in-memory");
            }
        }

        _memoryCache.Set(key, "revoked", ttl);
    }

    public async Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}{jti}";

        if (_redis is not null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                return await db.KeyExistsAsync(key);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for blacklist check; falling back to in-memory");
            }
        }

        return _memoryCache.TryGetValue(key, out _);
    }
}
