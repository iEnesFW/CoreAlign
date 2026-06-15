using CoreAlign.Application.Providers;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Infrastructure.Providers;

public sealed class TenantProviderConfigResolver : ITenantProviderConfigResolver
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;

    public TenantProviderConfigResolver(IServiceScopeFactory scopeFactory, IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    public async Task<string?> GetDefaultProviderNameAsync(Guid tenantId, ProviderCategory category, CancellationToken cancellationToken = default)
    {
        var key = DefaultKey(tenantId, category);
        if (_cache.TryGetValue<string?>(key, out var cached))
        {
            return cached;
        }

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITenantProviderConfigRepository>();
        var config = await repo.GetDefaultForTenantAsync(tenantId, category, cancellationToken);

        var providerName = config?.IsEnabled == true ? config.ProviderName : null;
        SetCache(key, providerName);
        return providerName;
    }

    public async Task<string?> GetEncryptedCredentialsAsync(Guid tenantId, ProviderCategory category, string providerName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        var key = CredsKey(tenantId, category, providerName);
        if (_cache.TryGetValue<string?>(key, out var cached))
        {
            return cached;
        }

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITenantProviderConfigRepository>();
        var config = await repo.GetByTenantAndCategoryAsync(tenantId, category, providerName, cancellationToken);

        var creds = config?.EncryptedCredentialsJson;
        SetCache(key, creds);
        return creds;
    }

    public Task InvalidateCacheAsync(Guid tenantId, ProviderCategory? category = null)
    {
        if (category.HasValue)
        {
            _cache.Remove(DefaultKey(tenantId, category.Value));
        }
        else
        {
            foreach (ProviderCategory cat in Enum.GetValues<ProviderCategory>())
            {
                _cache.Remove(DefaultKey(tenantId, cat));
            }
        }

        return Task.CompletedTask;
    }

    private void SetCache(string key, string? value)
    {
        var entryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl,
            Size = 1,
        };
        _cache.Set(key, value, entryOptions);
    }

    private static string DefaultKey(Guid tenantId, ProviderCategory category) =>
        $"tpc:default:{tenantId:N}:{(int)category}";

    private static string CredsKey(Guid tenantId, ProviderCategory category, string providerName) =>
        $"tpc:creds:{tenantId:N}:{(int)category}:{providerName.ToLowerInvariant()}";
}
