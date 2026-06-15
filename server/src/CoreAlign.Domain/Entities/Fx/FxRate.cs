using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Fx;

[Obsolete("Wave 1B consolidation: FxRate entity retired. Treasury.ExchangeRate is the canonical FX store (Phase 40 pipeline). Type retained only for binary compatibility with Phase53FxRates Designer snapshots — the fx_rates table is dropped in Phase58FxRatesRetire.")]
public class FxRate : BaseEntity, IHasConcurrencyToken
{
    public string CurrencyCode { get; private set; } = string.Empty;
    public string Source { get; private set; } = "TCMB";
    public DateTime EffectiveDate { get; private set; }
    public decimal BuyingRate { get; private set; }
    public decimal SellingRate { get; private set; }
    public decimal? CrossRateUsd { get; private set; }
    public DateTime FetchedAtUtc { get; private set; }
    public long ConcurrencyToken { get; private set; }

    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    protected FxRate() { }

    public FxRate(
        string currencyCode,
        string source,
        DateTime effectiveDate,
        decimal buyingRate,
        decimal sellingRate,
        decimal? crossRateUsd = null)
    {
        CurrencyCode = (currencyCode ?? string.Empty).Trim().ToUpperInvariant();
        Source = string.IsNullOrWhiteSpace(source) ? "TCMB" : source.Trim().ToUpperInvariant();
        EffectiveDate = DateTime.SpecifyKind(effectiveDate.Date, DateTimeKind.Utc);
        BuyingRate = buyingRate;
        SellingRate = sellingRate;
        CrossRateUsd = crossRateUsd;
        FetchedAtUtc = DateTime.UtcNow;
    }
}
