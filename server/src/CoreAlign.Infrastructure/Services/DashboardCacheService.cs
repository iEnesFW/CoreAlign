using System.Collections.Concurrent;
using CoreAlign.Application.Common.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace CoreAlign.Infrastructure.Services;

public class DashboardCacheService : IDashboardCacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromSeconds(30);
    private const string KeyPrefix = "dashboard";

    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _tenantKeys = new();

    public DashboardCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string BuildKey(Guid tenantId, string suffix) => $"{KeyPrefix}:{tenantId:N}:{suffix}";

    public async Task<T> GetOrAddAsync<T>(
        string cacheKey,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(cacheKey, out var existing) && existing is T cachedValue)
        {
            return cachedValue;
        }

        var produced = await factory(cancellationToken);
        var entryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl,
            // The shared IMemoryCache sets a SizeLimit, so every entry must
            // declare a Size (1 unit each, matching LookupCacheService).
            Size = 1,
        };
        entryOptions.RegisterPostEvictionCallback(OnEviction);

        _cache.Set(cacheKey, produced!, entryOptions);
        TrackTenantKey(cacheKey);

        return produced;
    }

    public void InvalidateTenant(Guid tenantId)
    {
        if (!_tenantKeys.TryRemove(tenantId, out var keys)) return;
        foreach (var key in keys.Keys)
        {
            _cache.Remove(key);
        }
    }

    private void TrackTenantKey(string cacheKey)
    {
        var tenantId = ExtractTenantId(cacheKey);
        if (tenantId == Guid.Empty) return;
        var bag = _tenantKeys.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, byte>());
        bag[cacheKey] = 0;
    }

    private void OnEviction(object key, object? value, EvictionReason reason, object? state)
    {
        if (key is not string s) return;
        var tenantId = ExtractTenantId(s);
        if (tenantId == Guid.Empty) return;
        if (_tenantKeys.TryGetValue(tenantId, out var bag))
        {
            bag.TryRemove(s, out _);
        }
    }

    private static Guid ExtractTenantId(string cacheKey)
    {
        var parts = cacheKey.Split(':');
        if (parts.Length < 3) return Guid.Empty;
        return Guid.TryParseExact(parts[1], "N", out var id) ? id : Guid.Empty;
    }
}
