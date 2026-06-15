using CoreAlign.Application.Fx;
using CoreAlign.Application.Treasury.Fx;
using CoreAlign.Domain.Entities.Treasury;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Infrastructure.Fx;

public sealed class ManualFxProvider : IFxSourceProvider
{
    private const string TryCode = "TRY";

    private readonly IExchangeRateRepository _exchangeRates;

    public ManualFxProvider(IExchangeRateRepository exchangeRates)
    {
        _exchangeRates = exchangeRates;
    }

    public FxSource Source => FxSource.Manual;

    public bool SupportsCurrency(string currencyCode) =>
        !string.IsNullOrWhiteSpace(currencyCode);

    public async Task<FxRateSnapshot?> TryGetRateAsync(string currencyCode, DateTime asOfDate, CancellationToken ct = default)
    {
        var code = Normalize(currencyCode);
        if (string.IsNullOrEmpty(code)) return null;
        if (string.Equals(code, TryCode, StringComparison.OrdinalIgnoreCase))
        {
            return new FxRateSnapshot(TryCode, 1m, 1m, DateOnlyUtc(asOfDate), FxSourceCodes.Manual);
        }

        var hits = await _exchangeRates.GetLatestPerCurrencyOnOrBeforeAsync(asOfDate, ct);
        var match = hits
            .Where(r => string.Equals(r.Currency, code, StringComparison.OrdinalIgnoreCase))
            .Where(r => string.Equals(r.Source, FxSourceCodes.Manual, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.ValidOnDate)
            .FirstOrDefault();
        return match is null ? null : ToSnapshot(match);
    }

    private static FxRateSnapshot ToSnapshot(ExchangeRate r) =>
        new(r.Currency, r.RateAgainstTry, r.RateAgainstTry, DateOnlyUtc(r.ValidOnDate), FxSourceCodes.Manual);

    private static string Normalize(string? code) =>
        string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim().ToUpperInvariant();

    private static DateTime DateOnlyUtc(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
}
