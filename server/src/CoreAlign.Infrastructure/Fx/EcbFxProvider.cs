using CoreAlign.Application.Fx;
using CoreAlign.Application.Treasury.Fx;
using CoreAlign.Domain.Entities.Treasury;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Fx.Ecb;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Fx;

public sealed class EcbFxProvider : IFxSourceProvider
{
    public const string HttpClientName = "EcbFx";
    private const string BaseCurrency = "EUR";
    private const string TryCode = "TRY";
    private const int CacheEntrySize = 1;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(4);
    private static readonly string[] SupportedCurrencies =
    {
        "USD", "EUR", "GBP", "JPY", "CHF", "CAD", "AUD", "TRY",
        "SEK", "NOK", "DKK", "CNY", "RUB", "PLN", "CZK", "HUF",
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IExchangeRateRepository _exchangeRates;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<EcbFxProvider> _logger;

    public EcbFxProvider(
        IHttpClientFactory httpClientFactory,
        IExchangeRateRepository exchangeRates,
        IMemoryCache memoryCache,
        ILogger<EcbFxProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _exchangeRates = exchangeRates;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public FxSource Source => FxSource.Ecb;

    public bool SupportsCurrency(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode)) return false;
        var code = currencyCode.Trim().ToUpperInvariant();
        return SupportedCurrencies.Contains(code, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<FxRateSnapshot?> TryGetRateAsync(string currencyCode, DateTime asOfDate, CancellationToken ct = default)
    {
        var code = Normalize(currencyCode);
        if (string.IsNullOrEmpty(code)) return null;
        if (string.Equals(code, BaseCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return new FxRateSnapshot(BaseCurrency, 1m, 1m, DateOnlyUtc(asOfDate), FxSourceCodes.Ecb);
        }

        var cacheKey = BuildCacheKey(code, asOfDate);
        if (_memoryCache.TryGetValue<FxRateSnapshot?>(cacheKey, out var cached))
        {
            return cached;
        }

        FxRateSnapshot? snapshot;
        try
        {
            snapshot = await ResolveTryBasedSnapshotAsync(code, asOfDate, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "ECB FX request failed for {Currency}.", code);
            snapshot = null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "ECB FX request cancelled for {Currency}.", code);
            snapshot = null;
        }

        SetCache(cacheKey, snapshot);
        return snapshot;
    }

    private async Task<FxRateSnapshot?> ResolveTryBasedSnapshotAsync(string code, DateTime asOfDate, CancellationToken ct)
    {
        var eurPerTry = await FetchEurRateAsync(TryCode, asOfDate, ct);
        if (eurPerTry is null || eurPerTry.Value <= 0m)
        {
            return null;
        }

        if (string.Equals(code, TryCode, StringComparison.OrdinalIgnoreCase))
        {
            var tryRate = Math.Round(1m / eurPerTry.Value, 6, MidpointRounding.ToEven);
            return new FxRateSnapshot(TryCode, tryRate, tryRate, DateOnlyUtc(asOfDate), FxSourceCodes.Ecb);
        }

        var eurPerTarget = await FetchEurRateAsync(code, asOfDate, ct);
        if (eurPerTarget is null || eurPerTarget.Value <= 0m)
        {
            return null;
        }

        var rateAgainstTry = Math.Round(eurPerTry.Value / eurPerTarget.Value, 6, MidpointRounding.ToEven);
        return new FxRateSnapshot(code, rateAgainstTry, rateAgainstTry, DateOnlyUtc(asOfDate), FxSourceCodes.Ecb);
    }

    private async Task<decimal?> FetchEurRateAsync(string currencyCode, DateTime asOfDate, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var url = BuildSdmxUrl(currencyCode, asOfDate);
        using var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogInformation("ECB API returned {Status} for {Currency} {AsOf}.", response.StatusCode, currencyCode, asOfDate);
            return null;
        }
        var xml = await response.Content.ReadAsStringAsync(ct);
        var parsed = EcbXmlParser.Parse(xml, asOfDate);
        var match = parsed
            .Where(r => string.Equals(r.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefault();
        return match?.RateAgainstEur;
    }

    public static string BuildSdmxUrl(string currencyCode, DateTime asOfDate)
    {
        var endDate = asOfDate.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var startDate = asOfDate.Date.AddDays(-7).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        return $"https://data-api.ecb.europa.eu/service/data/EXR/D.{currencyCode}.EUR.SP00.A?startPeriod={startDate}&endPeriod={endDate}&detail=dataonly";
    }

    public async Task<int> PersistDailySyncAsync(DateTime asOfDate, IUnitOfWork uow, CancellationToken ct = default)
    {
        var upserted = 0;
        foreach (var code in SupportedCurrencies)
        {
            if (string.Equals(code, TryCode, StringComparison.OrdinalIgnoreCase)) continue;
            var snapshot = await TryGetRateAsync(code, asOfDate, ct);
            if (snapshot is null) continue;
            var existing = await _exchangeRates.GetAsync(code, asOfDate.Date, ct);
            if (existing is null)
            {
                await _exchangeRates.AddAsync(new ExchangeRate
                {
                    Id = Guid.NewGuid(),
                    TenantId = Guid.Empty,
                    Currency = code,
                    RateAgainstTry = snapshot.BuyingRate,
                    ValidOnDate = DateTime.SpecifyKind(asOfDate.Date, DateTimeKind.Utc),
                    Source = FxSourceCodes.Ecb,
                    FetchedAtUtc = DateTime.UtcNow,
                }, ct);
            }
            else if (string.Equals(existing.Source, FxSourceCodes.Ecb, StringComparison.OrdinalIgnoreCase))
            {
                existing.RateAgainstTry = snapshot.BuyingRate;
                existing.FetchedAtUtc = DateTime.UtcNow;
                _exchangeRates.Update(existing);
            }
            upserted++;
        }
        await uow.SaveChangesAsync(ct);
        return upserted;
    }

    private static string Normalize(string? code) =>
        string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim().ToUpperInvariant();

    private static DateTime DateOnlyUtc(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    private static string BuildCacheKey(string code, DateTime asOfDate) =>
        $"fx-ecb:{code}:{DateOnlyUtc(asOfDate):yyyy-MM-dd}";

    private void SetCache<T>(string key, T value)
    {
        _memoryCache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl,
            Size = CacheEntrySize,
        });
    }
}
