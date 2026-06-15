using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Fx;

public interface ITenantFxPreferences
{
    Task<TenantFxPreferenceSnapshot> GetAsync(Guid tenantId, CancellationToken ct = default);
    Task SetDefaultSourceAsync(Guid tenantId, FxSource source, CancellationToken ct = default);
    Task SetPerCurrencyOverridesAsync(Guid tenantId, IReadOnlyDictionary<string, FxSource> overrides, CancellationToken ct = default);
}

public sealed record TenantFxPreferenceSnapshot(
    FxSource DefaultSource,
    IReadOnlyDictionary<string, FxSource> PerCurrencyOverrides)
{
    public static TenantFxPreferenceSnapshot Default { get; } =
        new(FxSource.Tcmb, new Dictionary<string, FxSource>(StringComparer.OrdinalIgnoreCase));

    public FxSource ResolveSourceFor(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return DefaultSource;
        }
        var code = currencyCode.Trim().ToUpperInvariant();
        return PerCurrencyOverrides.TryGetValue(code, out var src) ? src : DefaultSource;
    }
}
