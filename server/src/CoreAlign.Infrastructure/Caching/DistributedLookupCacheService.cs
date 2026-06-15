using CoreAlign.Application.Common.Caching;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Infrastructure.Caching;

public sealed class DistributedLookupCacheService : ILookupCacheService
{
    private const string Region = nameof(CacheRegion.Lookups);
    private static readonly Guid GlobalTenant = Guid.Empty;

    private readonly IDistributedCacheService _cache;
    private readonly ITenantContext _tenantContext;

    public DistributedLookupCacheService(IDistributedCacheService cache, ITenantContext tenantContext)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        var tenantKey = BuildTenantKey(key);
        var existing = await _cache.GetAsync<T>(Region, tenantKey, cancellationToken);
        if (existing is not null) return existing;

        var produced = await factory(cancellationToken);
        if (produced is null) return produced;

        await _cache.SetAsync(Region, tenantKey, produced, ttl, cancellationToken);
        return produced;
    }

    public void Invalidate(string key)
    {
        var tenantKey = BuildTenantKey(key);
        _cache.RemoveAsync(Region, tenantKey).GetAwaiter().GetResult();
    }

    public void InvalidatePrefix(string prefix)
    {
        var tenantId = ResolveTenantId();
        _cache.RemoveByPrefixAsync(Region, tenantId, prefix).GetAwaiter().GetResult();
    }

    private string BuildTenantKey(string suffix)
    {
        if (string.IsNullOrWhiteSpace(suffix)) throw new ArgumentException("Key suffix is required.", nameof(suffix));
        return _cache.BuildKey(Region, ResolveTenantId(), suffix);
    }

    private Guid ResolveTenantId() => _tenantContext.CurrentTenantId ?? GlobalTenant;
}
