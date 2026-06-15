namespace CoreAlign.Application.Fx;

public interface IFxRateProvider
{
    Task<FxRateSnapshot?> GetRateAsync(string currencyCode, DateTime asOfDate, CancellationToken ct = default);
    Task<IReadOnlyList<FxRateSnapshot>> GetLatestAsync(CancellationToken ct = default);
    Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency, DateTime asOfDate, CancellationToken ct = default);
    Task<int> SyncFromTcmbAsync(DateTime? targetDate = null, CancellationToken ct = default);
}

public sealed record FxRateSnapshot(
    string CurrencyCode,
    decimal BuyingRate,
    decimal SellingRate,
    DateTime EffectiveDate,
    string Source);
