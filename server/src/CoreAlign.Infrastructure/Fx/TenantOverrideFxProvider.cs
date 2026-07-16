using CoreAlign.Application.Fx;
using CoreAlign.Application.Treasury.Fx;
using CoreAlign.Domain.Entities.Treasury;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Infrastructure.Fx;

public sealed class TenantOverrideFxProvider
{
    private readonly IExchangeRateRepository _exchangeRates;

    public TenantOverrideFxProvider(IExchangeRateRepository exchangeRates)
    {
        _exchangeRates = exchangeRates;
    }

    public async Task<FxRateSnapshot?> TryGetTenantOverrideAsync(string currencyCode, DateTime asOfDate, Guid tenantId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currencyCode) || tenantId == Guid.Empty)
        {
            return null;
        }
        var code = currencyCode.Trim().ToUpperInvariant();
        var hits = await _exchangeRates.GetLatestTenantOverridesOnOrBeforeAsync(tenantId, asOfDate, ct);
        var match = hits
            .Where(r => string.Equals(r.Currency, code, StringComparison.OrdinalIgnoreCase))
            .Where(r => string.Equals(r.Source, FxSourceCodes.TenantOverride, StringComparison.OrdinalIgnoreCase))
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.ValidOnDate)
            .FirstOrDefault();
        return match is null
            ? null
            : new FxRateSnapshot(
                match.Currency,
                match.RateAgainstTry,
                match.RateAgainstTry,
                DateTime.SpecifyKind(match.ValidOnDate.Date, DateTimeKind.Utc),
                FxSourceCodes.TenantOverride);
    }
}
