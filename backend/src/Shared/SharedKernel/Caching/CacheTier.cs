namespace SharedKernel.Caching;

/// <summary>
/// Defines the three-tier caching strategy per AD-009.
/// L1: Provider availability (30s TTL) - high-frequency, low-staleness tolerance.
/// L2: Patient profiles (5min TTL) - moderate-frequency reads.
/// L3: 360-degree views (15min TTL) - aggregated views with higher staleness tolerance.
/// </summary>
public enum CacheTier
{
    L1 = 1,
    L2 = 2,
    L3 = 3
}

public static class CacheTierExtensions
{
    public static TimeSpan GetTtl(this CacheTier tier) => tier switch
    {
        CacheTier.L1 => TimeSpan.FromSeconds(30),
        CacheTier.L2 => TimeSpan.FromMinutes(5),
        CacheTier.L3 => TimeSpan.FromMinutes(15),
        _ => TimeSpan.FromMinutes(5)
    };
}
