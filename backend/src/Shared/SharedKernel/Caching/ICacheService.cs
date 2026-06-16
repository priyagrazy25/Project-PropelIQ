namespace SharedKernel.Caching;

/// <summary>
/// Cache service abstraction supporting 3-tier TTL strategy (AD-009, NFR-017).
/// Implementations: RedisCacheService (primary), InMemoryCacheService (fallback).
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    Task SetAsync<T>(string key, T value, CacheTier tier, CancellationToken cancellationToken = default) where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task RemoveByPrefixAsync(string prefixPattern, CancellationToken cancellationToken = default);
}
