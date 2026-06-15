using System.Text.Json;
using CoreAlign.Application.Fx;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace CoreAlign.Infrastructure.Fx;

public sealed class TenantFxPreferences : ITenantFxPreferences
{
    public const string Category = "Fx";
    public const string DefaultSourceKey = "DefaultFxSource";
    public const string PerCurrencyOverridesKey = "PerCurrencyOverrides";
    private const int CacheEntrySize = 1;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private readonly ITenantSettingRepository _settings;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _memoryCache;

    public TenantFxPreferences(ITenantSettingRepository settings, IUnitOfWork uow, IMemoryCache memoryCache)
    {
        _settings = settings;
        _uow = uow;
        _memoryCache = memoryCache;
    }

    public async Task<TenantFxPreferenceSnapshot> GetAsync(Guid tenantId, CancellationToken ct = default)
    {
        var cacheKey = BuildCacheKey(tenantId);
        if (_memoryCache.TryGetValue<TenantFxPreferenceSnapshot>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var defaultRaw = await _settings.GetAsync(Category, DefaultSourceKey, ct);
        var overridesRaw = await _settings.GetAsync(Category, PerCurrencyOverridesKey, ct);

        var defaultSource = FxSourceCodes.Parse(defaultRaw?.Value);
        var overrides = ParseOverrides(overridesRaw?.Value);

        var snapshot = new TenantFxPreferenceSnapshot(defaultSource, overrides);
        _memoryCache.Set(cacheKey, snapshot, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl,
            Size = CacheEntrySize,
        });
        return snapshot;
    }

    public async Task SetDefaultSourceAsync(Guid tenantId, FxSource source, CancellationToken ct = default)
    {
        await _settings.UpsertAsync(Category, DefaultSourceKey, FxSourceCodes.ToCode(source), "select", "Tenant default FX source", false, ct);
        await _uow.SaveChangesAsync(ct);
        _memoryCache.Remove(BuildCacheKey(tenantId));
    }

    public async Task SetPerCurrencyOverridesAsync(Guid tenantId, IReadOnlyDictionary<string, FxSource> overrides, CancellationToken ct = default)
    {
        var serializable = overrides.ToDictionary(
            kvp => (kvp.Key ?? string.Empty).Trim().ToUpperInvariant(),
            kvp => FxSourceCodes.ToCode(kvp.Value),
            StringComparer.OrdinalIgnoreCase);
        var json = JsonSerializer.Serialize(serializable);
        await _settings.UpsertAsync(Category, PerCurrencyOverridesKey, json, "json", "Per-currency FX source overrides", false, ct);
        await _uow.SaveChangesAsync(ct);
        _memoryCache.Remove(BuildCacheKey(tenantId));
    }

    private static Dictionary<string, FxSource> ParseOverrides(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, FxSource>(StringComparer.OrdinalIgnoreCase);
        }
        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (raw is null)
            {
                return new Dictionary<string, FxSource>(StringComparer.OrdinalIgnoreCase);
            }
            return raw.ToDictionary(
                kvp => kvp.Key.Trim().ToUpperInvariant(),
                kvp => FxSourceCodes.Parse(kvp.Value),
                StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, FxSource>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string BuildCacheKey(Guid tenantId) => $"fx-prefs:{tenantId:N}";
}
