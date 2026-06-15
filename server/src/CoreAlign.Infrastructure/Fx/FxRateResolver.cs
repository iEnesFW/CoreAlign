using CoreAlign.Application.Fx;
using CoreAlign.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Fx;

public sealed class FxRateResolver : IFxRateResolver, IFxRateResolverDetailed
{
    private readonly IEnumerable<IFxSourceProvider> _providers;
    private readonly TenantOverrideFxProvider _tenantOverrides;
    private readonly ITenantFxPreferences _preferences;
    private readonly ILogger<FxRateResolver> _logger;

    public FxRateResolver(
        IEnumerable<IFxSourceProvider> providers,
        TenantOverrideFxProvider tenantOverrides,
        ITenantFxPreferences preferences,
        ILogger<FxRateResolver> logger)
    {
        _providers = providers;
        _tenantOverrides = tenantOverrides;
        _preferences = preferences;
        _logger = logger;
    }

    public async Task<FxRateSnapshot?> ResolveAsync(string currencyCode, DateTime asOfDate, Guid? tenantId, CancellationToken ct = default)
    {
        var result = await ResolveDetailedAsync(currencyCode, asOfDate, tenantId, ct);
        return result?.Snapshot;
    }

    public async Task<FxResolutionResult?> ResolveDetailedAsync(string currencyCode, DateTime asOfDate, Guid? tenantId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return null;
        }

        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            var tenantOverride = await _tenantOverrides.TryGetTenantOverrideAsync(currencyCode, asOfDate, tenantId.Value, ct);
            if (tenantOverride is not null)
            {
                return new FxResolutionResult(tenantOverride, FxSource.TenantOverride, true);
            }
        }

        FxSource preferredSource = FxSource.Tcmb;
        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            var prefs = await _preferences.GetAsync(tenantId.Value, ct);
            preferredSource = prefs.ResolveSourceFor(currencyCode);
        }

        var preferredProvider = _providers.FirstOrDefault(p => p.Source == preferredSource && p.SupportsCurrency(currencyCode));
        if (preferredProvider is not null)
        {
            var snapshot = await preferredProvider.TryGetRateAsync(currencyCode, asOfDate, ct);
            if (snapshot is not null)
            {
                return new FxResolutionResult(snapshot, preferredSource, false);
            }
            _logger.LogInformation("FX preferred source {Source} returned no rate for {Currency} on {AsOf}; falling back to TCMB.",
                preferredSource, currencyCode, asOfDate);
        }

        if (preferredSource != FxSource.Tcmb)
        {
            var tcmb = _providers.FirstOrDefault(p => p.Source == FxSource.Tcmb);
            if (tcmb is not null)
            {
                var fallback = await tcmb.TryGetRateAsync(currencyCode, asOfDate, ct);
                if (fallback is not null)
                {
                    return new FxResolutionResult(fallback, FxSource.Tcmb, false);
                }
            }
        }

        return null;
    }
}
