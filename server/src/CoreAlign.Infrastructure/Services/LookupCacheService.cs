using System.Collections.Concurrent;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace CoreAlign.Infrastructure.Services;

public class LookupCacheService : ILookupCacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public LookupCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<T>(key, out var hit))
        {
            return hit;
        }

        var value = await factory(cancellationToken);
        if (value is null) return value;

        using var entry = _cache.CreateEntry(key);
        entry.Value = value;
        entry.AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl;
        entry.Size = 1;
        entry.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
        {
            if (evictedKey is string k) _keys.TryRemove(k, out _);
        });
        _keys.TryAdd(key, 0);
        return value;
    }

    public void Invalidate(string key)
    {
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
    }

    public void InvalidatePrefix(string prefix)
    {
        foreach (var key in _keys.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                _cache.Remove(key);
                _keys.TryRemove(key, out _);
            }
        }
    }
}
