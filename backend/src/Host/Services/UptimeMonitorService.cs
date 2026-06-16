using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Host.Services;

/// <summary>
/// Background service that tracks platform uptime and triggers alerts when
/// uptime drops below 99.9% threshold per NFR-012.
/// Polls health endpoints every 60 seconds and records uptime/downtime events.
/// </summary>
public sealed class UptimeMonitorService : BackgroundService
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<UptimeMonitorService> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);
    private static readonly double UptimeThreshold = 99.9;

    // In-memory metrics (in production, persist to PlatformMetrics table)
    private int _totalChecks;
    private int _healthyChecks;
    private DateTime _windowStart = DateTime.UtcNow;
    private static readonly TimeSpan RollingWindow = TimeSpan.FromDays(30);

    public UptimeMonitorService(
        HealthCheckService healthCheckService,
        ILogger<UptimeMonitorService> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UptimeMonitorService started — monitoring uptime every {Interval}s",
            (int)PollInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckUptimeAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Uptime check failed");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("UptimeMonitorService stopped");
    }

    private async Task CheckUptimeAsync(CancellationToken cancellationToken)
    {
        // Reset rolling window if expired
        if (DateTime.UtcNow - _windowStart > RollingWindow)
        {
            _windowStart = DateTime.UtcNow;
            _totalChecks = 0;
            _healthyChecks = 0;
            _logger.LogInformation("Uptime rolling window reset");
        }

        var report = await _healthCheckService.CheckHealthAsync(
            registration => registration.Tags.Contains("ready") || registration.Tags.Contains("module"),
            cancellationToken);

        _totalChecks++;

        if (report.Status == HealthStatus.Healthy)
        {
            _healthyChecks++;
        }
        else
        {
            _logger.LogWarning(
                "Platform health degraded: {Status} — {Entries}",
                report.Status,
                string.Join(", ", report.Entries
                    .Where(e => e.Value.Status != HealthStatus.Healthy)
                    .Select(e => $"{e.Key}={e.Value.Status}")));
        }

        var uptimePercent = _totalChecks > 0
            ? (double)_healthyChecks / _totalChecks * 100
            : 100;

        // Alert if uptime drops below threshold
        if (uptimePercent < UptimeThreshold && _totalChecks >= 10)
        {
            _logger.LogError(
                "UPTIME ALERT: Rolling 30-day uptime is {Uptime:F2}%, below {Threshold}% threshold (NFR-012)",
                uptimePercent,
                UptimeThreshold);
        }

        // Log uptime metrics periodically (every 10 checks = ~10 minutes)
        if (_totalChecks % 10 == 0)
        {
            _logger.LogInformation(
                "Uptime metrics: {Healthy}/{Total} checks healthy ({Uptime:F2}%) over rolling window",
                _healthyChecks,
                _totalChecks,
                uptimePercent);
        }
    }

    /// <summary>
    /// Gets the current rolling 30-day uptime percentage.
    /// </summary>
    public double GetCurrentUptimePercent()
    {
        return _totalChecks > 0
            ? (double)_healthyChecks / _totalChecks * 100
            : 100;
    }

    /// <summary>
    /// Gets uptime statistics for monitoring endpoints.
    /// </summary>
    public UptimeStats GetStats()
    {
        return new UptimeStats(
            _totalChecks,
            _healthyChecks,
            GetCurrentUptimePercent(),
            _windowStart);
    }
}

/// <summary>
/// Uptime statistics record for monitoring endpoints.
/// </summary>
public sealed record UptimeStats(
    int TotalChecks,
    int HealthyChecks,
    double UptimePercent,
    DateTime WindowStart);
