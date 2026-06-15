using CoreAlign.Application.Common.Caching;

namespace CoreAlign.Infrastructure.Caching;

public sealed class DistributedDashboardCacheService : IDashboardCacheService
{
    private const string Region = nameof(CacheRegion.Dashboard);

    private readonly IDistributedCacheService _cache;

    public DistributedDashboardCacheService(IDistributedCacheService cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public string BuildKey(Guid tenantId, string suffix) => _cache.BuildKey(Region, tenantId, suffix);

    public Task<T> GetOrAddAsync<T>(
        string cacheKey,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
        => _cache.GetOrAddAsync(Region, cacheKey, factory, ttl, cancellationToken);

    public void InvalidateTenant(Guid tenantId)
    {
        _cache.RemoveByRegionTenantAsync(Region, tenantId).GetAwaiter().GetResult();
    }
}
