namespace CoreAlign.Application.Common.Caching;

public enum CacheRegion
{
    Dashboard,
    Lookups,
    CustomReportData,
    Generic,
}

public interface IDistributedCacheService
{
    Task<T?> GetAsync<T>(string region, string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string region, string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    Task<T> GetOrAddAsync<T>(
        string region,
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string region, string key, CancellationToken cancellationToken = default);

    Task RemoveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task RemoveByRegionTenantAsync(string region, Guid tenantId, CancellationToken cancellationToken = default);

    Task RemoveByPrefixAsync(string region, Guid tenantId, string prefix, CancellationToken cancellationToken = default);

    string BuildKey(string region, Guid tenantId, string suffix);

    TimeSpan ResolveTtl(string region, TimeSpan? requestedTtl);
}
