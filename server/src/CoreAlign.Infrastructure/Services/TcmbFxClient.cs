using System.Globalization;
using System.Xml.Linq;
using CoreAlign.Application.Treasury.Fx;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Services;

public sealed class TcmbFxClient : ITcmbFxClient
{
    private readonly HttpClient _http;
    private readonly ILogger<TcmbFxClient> _logger;

    public TcmbFxClient(HttpClient http, ILogger<TcmbFxClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TcmbRate>> FetchTodayAsync(CancellationToken ct)
    {
        using var response = await _http.GetAsync("kurlar/today.xml", ct);
        response.EnsureSuccessStatusCode();
        var xml = await response.Content.ReadAsStringAsync(ct);
        return Parse(xml, _logger);
    }

    public static IReadOnlyList<TcmbRate> Parse(string xml, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(xml)) return Array.Empty<TcmbRate>();
        XDocument doc;
        try { doc = XDocument.Parse(xml); }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "TCMB XML parse failed.");
            return Array.Empty<TcmbRate>();
        }

        var root = doc.Root;
        if (root is null) return Array.Empty<TcmbRate>();

        var dateAttr = root.Attribute("Tarih")?.Value;
        var validOn = DateTime.UtcNow.Date;
        if (!string.IsNullOrEmpty(dateAttr) &&
            DateTime.TryParseExact(dateAttr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
        {
            validOn = DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
        }

        var results = new List<TcmbRate>();
        foreach (var currency in root.Elements("Currency"))
        {
            var code = currency.Attribute("CurrencyCode")?.Value;
            var sellingRaw = currency.Element("ForexSelling")?.Value;
            var unitRaw = currency.Element("Unit")?.Value;
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(sellingRaw)) continue;

            if (!decimal.TryParse(sellingRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var selling)) continue;
            var unit = 1m;
            if (!string.IsNullOrWhiteSpace(unitRaw) && decimal.TryParse(unitRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedUnit) && parsedUnit > 0m)
            {
                unit = parsedUnit;
            }
            var perOne = Math.Round(selling / unit, 6, MidpointRounding.ToEven);
            results.Add(new TcmbRate(code.Trim().ToUpperInvariant(), perOne, validOn));
        }
        return results;
    }
}
