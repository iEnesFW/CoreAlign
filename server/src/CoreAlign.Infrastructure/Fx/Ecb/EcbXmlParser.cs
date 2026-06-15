using System.Globalization;
using System.Xml.Linq;

namespace CoreAlign.Infrastructure.Fx.Ecb;

public static class EcbXmlParser
{
    public static IReadOnlyList<EcbRate> Parse(string xmlContent, DateTime fallbackDate)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            return Array.Empty<EcbRate>();
        }

        XDocument doc;
        try
        {
            doc = XDocument.Parse(xmlContent);
        }
        catch (System.Xml.XmlException)
        {
            return Array.Empty<EcbRate>();
        }

        var results = new List<EcbRate>();
        foreach (var obs in doc.Descendants().Where(e => e.Name.LocalName == "Obs"))
        {
            var dateAttr = obs.Attribute("TIME_PERIOD")?.Value ?? obs.Element(obs.Name.Namespace + "ObsDimension")?.Attribute("value")?.Value;
            var valueAttr = obs.Attribute("OBS_VALUE")?.Value ?? obs.Element(obs.Name.Namespace + "ObsValue")?.Attribute("value")?.Value;
            var currencyAttr = ResolveCurrencyCode(obs);

            if (string.IsNullOrWhiteSpace(currencyAttr)) continue;
            if (!TryParseDecimal(valueAttr, out var rate) || rate <= 0m) continue;

            var effectiveDate = TryParseDate(dateAttr, out var parsedDate)
                ? parsedDate
                : DateTime.SpecifyKind(fallbackDate.Date, DateTimeKind.Utc);

            results.Add(new EcbRate(currencyAttr.Trim().ToUpperInvariant(), rate, effectiveDate));
        }

        return results;
    }

    private static string? ResolveCurrencyCode(XElement obs)
    {
        var current = obs;
        while (current is not null)
        {
            var key = current.Attribute("CURRENCY")?.Value;
            if (!string.IsNullOrWhiteSpace(key))
            {
                return key;
            }
            var seriesKey = current.Elements().FirstOrDefault(e => e.Name.LocalName == "SeriesKey");
            if (seriesKey is not null)
            {
                var ccyValue = seriesKey.Elements()
                    .FirstOrDefault(v => string.Equals(v.Attribute("id")?.Value, "CURRENCY", StringComparison.OrdinalIgnoreCase))
                    ?.Attribute("value")?.Value;
                if (!string.IsNullOrWhiteSpace(ccyValue))
                {
                    return ccyValue;
                }
            }
            current = current.Parent;
        }
        return null;
    }

    private static bool TryParseDecimal(string? raw, out decimal value)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = 0m;
            return false;
        }
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseDate(string? raw, out DateTime value)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = default;
            return false;
        }
        if (DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
        {
            value = DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
            return true;
        }
        value = default;
        return false;
    }
}

public sealed record EcbRate(string CurrencyCode, decimal RateAgainstEur, DateTime EffectiveDate);
