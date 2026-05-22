namespace CoreAlign.Domain.Interfaces;

/// <summary>
/// Lightweight in-memory cache for slowly-changing reference data (tenant rows,
/// tax rates, units of measure, payment terms). Backed by IMemoryCache with a
/// short TTL — handlers fall back to the repository on miss.
/// </summary>
public interface ILookupCacheService
{
    Task<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T?>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
    void Invalidate(string key);
    void InvalidatePrefix(string prefix);
}
