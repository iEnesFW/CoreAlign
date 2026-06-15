using CoreAlign.Application.Fx;
using CoreAlign.Application.Treasury.Fx;
using CoreAlign.Domain.Entities.Treasury;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Fx;

public sealed class FxRateProvider : IFxRateProvider
{
    public const string SourceTcmb = "TCMB";
    public const string TryCode = "TRY";

    private const int CacheEntrySize = 1;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(4);

    private readonly IExchangeRateRepository _exchangeRates;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<FxRateProvider> _logger;
    private readonly IFxRateResolver? _resolver;
    private readonly ITenantContext? _tenantContext;

    public FxRateProvider(
        IExchangeRateRepository exchangeRates,
        IMemoryCache memoryCache,
        ILogger<FxRateProvider> logger,
        IFxRateResolver? resolver = null,
        ITenantContext? tenantContext = null)
    {
        _exchangeRates = exchangeRates;
        _memoryCache = memoryCache;
        _logger = logger;
        _resolver = resolver;
        _tenantContext = tenantContext;
    }

    public async Task<FxRateSnapshot?> GetRateAsync(string currencyCode, DateTime asOfDate, CancellationToken ct = default)
    {
        var code = Normalize(currencyCode);
        if (string.IsNullOrEmpty(code))
        {
            return null;
        }
        if (string.Equals(code, TryCode, StringComparison.OrdinalIgnoreCase))
        {
            var pivot = DateOnlyUtc(asOfDate);
            return new FxRateSnapshot(TryCode, 1m, 1m, pivot, "PIVOT");
        }

        var cacheKey = BuildRateCacheKey(code, asOfDate);
        if (_memoryCache.TryGetValue<FxRateSnapshot?>(cacheKey, out var cached))
        {
            return cached;
        }

        FxRateSnapshot? snapshot = null;
        if (_resolver is not null)
        {
            var tenantId = _tenantContext?.CurrentTenantId;
            snapshot = await _resolver.ResolveAsync(code, asOfDate, tenantId, ct);
        }

        if (snapshot is null)
        {
            var rates = await _exchangeRates.GetLatestPerCurrencyOnOrBeforeAsync(asOfDate, ct);
            var hit = rates.FirstOrDefault(r => string.Equals(r.Currency, code, StringComparison.OrdinalIgnoreCase));
            snapshot = hit is null ? null : ToSnapshot(hit);
        }

        SetCache(cacheKey, snapshot);
        return snapshot;
    }

    public async Task<IReadOnlyList<FxRateSnapshot>> GetLatestAsync(CancellationToken ct = default)
    {
        const string cacheKey = "fx-rates:latest";
        if (_memoryCache.TryGetValue<IReadOnlyList<FxRateSnapshot>>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var rates = await _exchangeRates.GetLatestPerCurrencyOnOrBeforeAsync(DateTime.UtcNow, ct);
        var snapshots = rates
            .OrderBy(r => r.Currency, StringComparer.OrdinalIgnoreCase)
            .Select(ToSnapshot)
            .ToList();
        SetCache(cacheKey, snapshots);
        return snapshots;
    }

    public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency, DateTime asOfDate, CancellationToken ct = default)
    {
        var fromCode = Normalize(fromCurrency);
        var toCode = Normalize(toCurrency);
        if (string.IsNullOrEmpty(fromCode) || string.IsNullOrEmpty(toCode))
        {
            throw new ArgumentException("Both fromCurrency and toCurrency are required.");
        }
        if (string.Equals(fromCode, toCode, StringComparison.OrdinalIgnoreCase))
        {
            return amount;
        }

        var fromRate = await ResolveTryRateAsync(fromCode, asOfDate, ct);
        var toRate = await ResolveTryRateAsync(toCode, asOfDate, ct);

        if (toRate <= 0m)
        {
            throw new InvalidOperationException($"Cannot resolve FX rate for target currency '{toCode}' as of {asOfDate:yyyy-MM-dd}.");
        }

        var amountInTry = amount * fromRate;
        return Math.Round(amountInTry / toRate, 6, MidpointRounding.ToEven);
    }

    public Task<int> SyncFromTcmbAsync(DateTime? targetDate = null, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "FxRateProvider.SyncFromTcmbAsync invoked (deprecated). Phase 40 TcmbFxIngestJob owns the canonical pipeline; no-op.");
        return Task.FromResult(0);
    }

    private async Task<decimal> ResolveTryRateAsync(string code, DateTime asOfDate, CancellationToken ct)
    {
        if (string.Equals(code, TryCode, StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        var snapshot = await GetRateAsync(code, asOfDate, ct);
        if (snapshot is null)
        {
            throw new InvalidOperationException($"No FX rate available for '{code}' as of {asOfDate:yyyy-MM-dd}.");
        }
        return snapshot.BuyingRate;
    }

    private static FxRateSnapshot ToSnapshot(ExchangeRate rate) =>
        new(
            rate.Currency,
            rate.RateAgainstTry,
            rate.RateAgainstTry,
            DateTime.SpecifyKind(rate.ValidOnDate.Date, DateTimeKind.Utc),
            string.IsNullOrWhiteSpace(rate.Source) ? SourceTcmb : rate.Source);

    private static string BuildRateCacheKey(string code, DateTime asOfDate) =>
        $"fx-rates:{code}:{DateOnlyUtc(asOfDate):yyyy-MM-dd}";

    private void SetCache<T>(string key, T value)
    {
        _memoryCache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl,
            Size = CacheEntrySize,
        });
    }

    private static string Normalize(string? code) =>
        string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim().ToUpperInvariant();

    private static DateTime DateOnlyUtc(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
}
