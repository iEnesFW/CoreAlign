using System.Collections.Concurrent;
using CoreAlign.Application.Common.Caching;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.Caching;

public sealed class InMemoryDistributedCacheService : IDistributedCacheService
{
    private readonly IMemoryCache _cache;
    private readonly CacheRegionOptions _regionOptions;
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _tenantKeys = new();

    public InMemoryDistributedCacheService(IMemoryCache cache, IOptions<CacheRegionOptions> regionOptions)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _regionOptions = regionOptions?.Value ?? new CacheRegionOptions();
    }

    public string BuildKey(string region, Guid tenantId, string suffix)
    {
        if (string.IsNullOrWhiteSpace(region)) throw new ArgumentException("Region is required.", nameof(region));
        if (string.IsNullOrWhiteSpace(suffix)) throw new ArgumentException("Suffix is required.", nameof(suffix));
        return $"{region}:{tenantId:N}:{suffix}";
    }

    public TimeSpan ResolveTtl(string region, TimeSpan? requestedTtl)
    {
        if (requestedTtl.HasValue && requestedTtl.Value > TimeSpan.Zero) return requestedTtl.Value;
        var seconds = region switch
        {
            nameof(CacheRegion.Dashboard) => _regionOptions.DashboardTtlSeconds,
            nameof(CacheRegion.Lookups) => _regionOptions.LookupsTtlSeconds,
            nameof(CacheRegion.CustomReportData) => _regionOptions.CustomReportDataTtlSeconds,
            _ => _regionOptions.GenericTtlSeconds,
        };
        return TimeSpan.FromSeconds(Math.Max(1, seconds));
    }

    public Task<T?> GetAsync<T>(string region, string key, CancellationToken cancellationToken = default)
    {
        EnsureKeyShape(region, key);
        if (_cache.TryGetValue(key, out var existing) && existing is T hit)
        {
            return Task.FromResult<T?>(hit);
        }
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string region, string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        EnsureKeyShape(region, key);
        if (value is null) return Task.CompletedTask;

        var entryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ResolveTtl(region, ttl),
            Size = 1,
        };
        entryOptions.RegisterPostEvictionCallback(OnEviction);

        _cache.Set(key, value!, entryOptions);
        TrackTenantKey(key);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrAddAsync<T>(
        string region,
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        EnsureKeyShape(region, key);
        if (_cache.TryGetValue(key, out var existing) && existing is T cached)
        {
            return cached;
        }

        var produced = await factory(cancellationToken);
        if (produced is null) return produced!;

        await SetAsync(region, key, produced, ttl, cancellationToken);
        return produced;
    }

    public Task RemoveAsync(string region, string key, CancellationToken cancellationToken = default)
    {
        EnsureKeyShape(region, key);
        _cache.Remove(key);
        var tenantId = ExtractTenantId(key);
        if (tenantId != Guid.Empty && _tenantKeys.TryGetValue(tenantId, out var bag))
        {
            bag.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }

    public Task RemoveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (!_tenantKeys.TryRemove(tenantId, out var keys)) return Task.CompletedTask;
        foreach (var key in keys.Keys)
        {
            _cache.Remove(key);
        }
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string region, Guid tenantId, string prefix, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("Prefix is required.", nameof(prefix));
        if (!_tenantKeys.TryGetValue(tenantId, out var bag)) return Task.CompletedTask;

        var full = $"{region}:{tenantId:N}:{prefix}";
        foreach (var key in bag.Keys)
        {
            if (key.StartsWith(full, StringComparison.Ordinal))
            {
                _cache.Remove(key);
                bag.TryRemove(key, out _);
            }
        }
        return Task.CompletedTask;
    }

    public Task RemoveByRegionTenantAsync(string region, Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(region)) throw new ArgumentException("Region is required.", nameof(region));
        if (!_tenantKeys.TryGetValue(tenantId, out var bag)) return Task.CompletedTask;

        var regionPrefix = $"{region}:{tenantId:N}:";
        foreach (var key in bag.Keys)
        {
            if (key.StartsWith(regionPrefix, StringComparison.Ordinal))
            {
                _cache.Remove(key);
                bag.TryRemove(key, out _);
            }
        }
        return Task.CompletedTask;
    }

    private static void EnsureKeyShape(string region, string key)
    {
        if (string.IsNullOrWhiteSpace(region)) throw new ArgumentException("Region is required.", nameof(region));
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));
        var parts = key.Split(':');
        if (parts.Length < 3 || !string.Equals(parts[0], region, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Key must be built via BuildKey('{region}', tenantId, suffix).", nameof(key));
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
