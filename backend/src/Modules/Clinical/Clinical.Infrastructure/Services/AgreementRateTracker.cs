using Clinical.Domain.Enums;
using Clinical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Clinical.Infrastructure.Services;

/// <summary>
/// Tracks and caches rolling 30-day agreement rate metrics (AIR-Q01).
/// Target: >98% agreement rate for AI code suggestions.
/// </summary>
public sealed class AgreementRateTracker
{
    private readonly ClinicalDbContext _dbContext;
    private readonly IDistributedCache? _cache;
    private readonly ILogger<AgreementRateTracker> _logger;

    private const string CacheKey = "coding:agreement-rate:30d";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const int RollingDays = 30;
    private const double TargetAgreementRate = 98.0;

    public AgreementRateTracker(
        ClinicalDbContext dbContext,
        ILogger<AgreementRateTracker> logger,
        IDistributedCache? cache = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Gets cached agreement rate metrics or computes fresh if expired.
    /// </summary>
    public async Task<AgreementRateSnapshot> GetCachedMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        // Try cache first
        if (_cache is not null)
        {
            try
            {
                var cached = await _cache.GetStringAsync(CacheKey, cancellationToken);
                if (!string.IsNullOrEmpty(cached))
                {
                    var snapshot = JsonSerializer.Deserialize<AgreementRateSnapshot>(cached);
                    if (snapshot is not null)
                    {
                        return snapshot;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read agreement rate from cache");
            }
        }

        // Compute fresh
        var fresh = await ComputeMetricsAsync(cancellationToken);

        // Cache result
        if (_cache is not null)
        {
            try
            {
                var json = JsonSerializer.Serialize(fresh);
                await _cache.SetStringAsync(
                    CacheKey,
                    json,
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache agreement rate metrics");
            }
        }

        return fresh;
    }

    /// <summary>
    /// Computes fresh agreement rate metrics from database.
    /// </summary>
    public async Task<AgreementRateSnapshot> ComputeMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var periodEnd = DateTime.UtcNow;
        var periodStart = periodEnd.AddDays(-RollingDays);

        var verifiedCodes = await _dbContext.MedicalCodes
            .AsNoTracking()
            .Where(mc => mc.VerifiedAt >= periodStart && mc.VerifiedAt <= periodEnd)
            .ToListAsync(cancellationToken);

        var acceptedCount = verifiedCodes.Count(mc => mc.VerificationStatus == VerificationStatus.Verified);
        var rejectedCount = verifiedCodes.Count(mc => mc.VerificationStatus == VerificationStatus.Rejected);
        var overriddenCount = verifiedCodes.Count(mc => mc.VerificationStatus == VerificationStatus.Overridden);

        var pendingCount = await _dbContext.MedicalCodes
            .AsNoTracking()
            .CountAsync(mc =>
                mc.VerificationStatus == VerificationStatus.Pending ||
                mc.VerificationStatus == VerificationStatus.NeedsReview,
                cancellationToken);

        // Agreement rate: accepted / (accepted + rejected) * 100
        // Overridden codes indicate AI was close but needed adjustment
        var agreementBase = acceptedCount + rejectedCount;
        var agreementRate = agreementBase > 0
            ? (double)acceptedCount / agreementBase * 100.0
            : 100.0;

        // Calculate daily trend
        var dailyRates = verifiedCodes
            .Where(mc => mc.VerificationStatus is VerificationStatus.Verified or VerificationStatus.Rejected)
            .GroupBy(mc => mc.VerifiedAt!.Value.Date)
            .Select(g =>
            {
                var dayAccepted = g.Count(mc => mc.VerificationStatus == VerificationStatus.Verified);
                var dayRejected = g.Count(mc => mc.VerificationStatus == VerificationStatus.Rejected);
                var dayTotal = dayAccepted + dayRejected;
                return new DailyRate(
                    g.Key,
                    dayTotal > 0 ? (double)dayAccepted / dayTotal * 100.0 : 100.0);
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Calculate trend (positive = improving, negative = declining)
        var trend = CalculateTrend(dailyRates);

        return new AgreementRateSnapshot
        {
            Rate = Math.Round(agreementRate, 2),
            AcceptedCount = acceptedCount,
            RejectedCount = rejectedCount,
            OverriddenCount = overriddenCount,
            PendingCount = pendingCount,
            TotalProcessed = acceptedCount + rejectedCount + overriddenCount,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TargetMet = agreementRate >= TargetAgreementRate,
            Trend = trend,
            DailyRates = dailyRates,
            ComputedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Invalidates cached metrics (call after verification actions).
    /// </summary>
    public async Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
    {
        if (_cache is not null)
        {
            try
            {
                await _cache.RemoveAsync(CacheKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate agreement rate cache");
            }
        }
    }

    private static double CalculateTrend(List<DailyRate> dailyRates)
    {
        if (dailyRates.Count < 2)
        {
            return 0.0;
        }

        // Simple linear regression slope
        var n = dailyRates.Count;
        var sumX = 0.0;
        var sumY = 0.0;
        var sumXY = 0.0;
        var sumX2 = 0.0;

        for (var i = 0; i < n; i++)
        {
            sumX += i;
            sumY += dailyRates[i].Rate;
            sumXY += i * dailyRates[i].Rate;
            sumX2 += i * i;
        }

        var denominator = n * sumX2 - sumX * sumX;
        if (Math.Abs(denominator) < 0.0001)
        {
            return 0.0;
        }

        return Math.Round((n * sumXY - sumX * sumY) / denominator, 4);
    }
}

/// <summary>
/// Snapshot of agreement rate metrics.
/// </summary>
public sealed record AgreementRateSnapshot
{
    /// <summary>
    /// Agreement rate as percentage.
    /// </summary>
    public required double Rate { get; init; }

    /// <summary>
    /// Number of accepted (verified) codes.
    /// </summary>
    public required int AcceptedCount { get; init; }

    /// <summary>
    /// Number of rejected codes.
    /// </summary>
    public required int RejectedCount { get; init; }

    /// <summary>
    /// Number of overridden codes.
    /// </summary>
    public required int OverriddenCount { get; init; }

    /// <summary>
    /// Number of pending codes.
    /// </summary>
    public required int PendingCount { get; init; }

    /// <summary>
    /// Total codes processed.
    /// </summary>
    public required int TotalProcessed { get; init; }

    /// <summary>
    /// Start of rolling period.
    /// </summary>
    public required DateTime PeriodStart { get; init; }

    /// <summary>
    /// End of rolling period.
    /// </summary>
    public required DateTime PeriodEnd { get; init; }

    /// <summary>
    /// Whether >98% target is met.
    /// </summary>
    public required bool TargetMet { get; init; }

    /// <summary>
    /// Trend slope (positive = improving).
    /// </summary>
    public required double Trend { get; init; }

    /// <summary>
    /// Daily rate breakdown.
    /// </summary>
    public required IReadOnlyList<DailyRate> DailyRates { get; init; }

    /// <summary>
    /// When metrics were computed.
    /// </summary>
    public required DateTime ComputedAt { get; init; }
}

/// <summary>
/// Daily agreement rate data point.
/// </summary>
public sealed record DailyRate(DateTime Date, double Rate);
