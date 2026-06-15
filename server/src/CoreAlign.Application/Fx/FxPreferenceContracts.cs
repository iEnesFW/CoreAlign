using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Fx;

public sealed record FxPreferenceDto(string DefaultSource, IReadOnlyDictionary<string, string> PerCurrencyOverrides);

public sealed record GetFxPreferencesQuery : IRequest<FxPreferenceDto>;

public sealed record UpdateFxPreferencesCommand(string DefaultSource, IReadOnlyDictionary<string, string> PerCurrencyOverrides) : IRequest<FxPreferenceDto>;

public sealed record ResolveFxRateQuery(string CurrencyCode, DateTime? AsOfDate) : IRequest<FxResolutionDto?>;

public sealed record FxResolutionDto(
    string CurrencyCode,
    decimal BuyingRate,
    decimal SellingRate,
    DateTime EffectiveDate,
    string Source,
    bool UsedTenantOverride);

public sealed class GetFxPreferencesHandler : IRequestHandler<GetFxPreferencesQuery, FxPreferenceDto>
{
    private readonly ITenantFxPreferences _preferences;
    private readonly ITenantContext _tenantContext;

    public GetFxPreferencesHandler(ITenantFxPreferences preferences, ITenantContext tenantContext)
    {
        _preferences = preferences;
        _tenantContext = tenantContext;
    }

    public async Task<FxPreferenceDto> Handle(GetFxPreferencesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var snap = await _preferences.GetAsync(tenantId, cancellationToken);
        var overrides = snap.PerCurrencyOverrides
            .ToDictionary(kvp => kvp.Key, kvp => FxSourceCodes.ToCode(kvp.Value), StringComparer.OrdinalIgnoreCase);
        return new FxPreferenceDto(FxSourceCodes.ToCode(snap.DefaultSource), overrides);
    }
}

public sealed class UpdateFxPreferencesHandler : IRequestHandler<UpdateFxPreferencesCommand, FxPreferenceDto>
{
    private readonly ITenantFxPreferences _preferences;
    private readonly ITenantContext _tenantContext;

    public UpdateFxPreferencesHandler(ITenantFxPreferences preferences, ITenantContext tenantContext)
    {
        _preferences = preferences;
        _tenantContext = tenantContext;
    }

    public async Task<FxPreferenceDto> Handle(UpdateFxPreferencesCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var defaultSource = FxSourceCodes.Parse(request.DefaultSource);
        var overrides = request.PerCurrencyOverrides
            .ToDictionary(kvp => kvp.Key, kvp => FxSourceCodes.Parse(kvp.Value), StringComparer.OrdinalIgnoreCase);

        await _preferences.SetDefaultSourceAsync(tenantId, defaultSource, cancellationToken);
        await _preferences.SetPerCurrencyOverridesAsync(tenantId, overrides, cancellationToken);

        var snap = await _preferences.GetAsync(tenantId, cancellationToken);
        var dto = snap.PerCurrencyOverrides
            .ToDictionary(kvp => kvp.Key, kvp => FxSourceCodes.ToCode(kvp.Value), StringComparer.OrdinalIgnoreCase);
        return new FxPreferenceDto(FxSourceCodes.ToCode(snap.DefaultSource), dto);
    }
}

public sealed class ResolveFxRateHandler : IRequestHandler<ResolveFxRateQuery, FxResolutionDto?>
{
    private readonly IFxRateResolverDetailed _resolver;
    private readonly ITenantContext _tenantContext;

    public ResolveFxRateHandler(IFxRateResolverDetailed resolver, ITenantContext tenantContext)
    {
        _resolver = resolver;
        _tenantContext = tenantContext;
    }

    public async Task<FxResolutionDto?> Handle(ResolveFxRateQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        var asOf = request.AsOfDate ?? DateTime.UtcNow;
        var result = await _resolver.ResolveDetailedAsync(request.CurrencyCode, asOf, tenantId, cancellationToken);
        if (result is null) return null;
        return new FxResolutionDto(
            result.Snapshot.CurrencyCode,
            result.Snapshot.BuyingRate,
            result.Snapshot.SellingRate,
            result.Snapshot.EffectiveDate,
            result.Snapshot.Source,
            result.UsedTenantOverride);
    }
}
