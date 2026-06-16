using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Host.Hubs;

/// <summary>
/// Health check for Redis connectivity. Returns Degraded when Redis is unreachable (AC-4 graceful degradation).
/// </summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly ConfigurationOptions _options;

    public RedisHealthCheck(ConfigurationOptions options)
    {
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var redis = await ConnectionMultiplexer.ConnectAsync(_options);
            var db = redis.GetDatabase();
            var latency = await db.PingAsync();

            return HealthCheckResult.Healthy(
                $"Redis connected. Latency: {latency.TotalMilliseconds:F1}ms");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded(
                "Redis unavailable. Fallback to in-memory cache active.", ex);
        }
    }
}
