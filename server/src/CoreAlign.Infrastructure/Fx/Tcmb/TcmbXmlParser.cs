using System.Globalization;
using System.Xml.Linq;

namespace CoreAlign.Infrastructure.Fx.Tcmb;

public static class TcmbXmlParser
{
    public static IReadOnlyList<TcmbRate> Parse(string xmlContent, DateTime effectiveDate)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            return Array.Empty<TcmbRate>();
        }

        XDocument doc;
        try
        {
            doc = XDocument.Parse(xmlContent);
        }
        catch (System.Xml.XmlException)
        {
            return Array.Empty<TcmbRate>();
        }

        var date = ResolveEffectiveDate(doc, effectiveDate);

        var results = new List<TcmbRate>();
        foreach (var currency in doc.Descendants("Currency"))
        {
            var code = currency.Attribute("CurrencyCode")?.Value;
            if (string.IsNullOrWhiteSpace(code)) continue;

            if (!TryParseDecimal(currency.Element("ForexBuying")?.Value, out var buying)) continue;
            if (!TryParseDecimal(currency.Element("ForexSelling")?.Value, out var selling)) continue;
            if (buying <= 0m || selling <= 0m) continue;

            var unit = 1;
            if (int.TryParse(currency.Element("Unit")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedUnit) && parsedUnit > 0)
            {
                unit = parsedUnit;
            }

            results.Add(new TcmbRate(
                CurrencyCode: code.Trim().ToUpperInvariant(),
                ForexBuying: buying,
                ForexSelling: selling,
                Unit: unit,
                EffectiveDate: date));
        }

        return results;
    }

    private static DateTime ResolveEffectiveDate(XDocument doc, DateTime fallback)
    {
        var dateAttr = doc.Root?.Attribute("Tarih")?.Value;
        if (!string.IsNullOrWhiteSpace(dateAttr) &&
            DateTime.TryParseExact(
                dateAttr,
                "dd.MM.yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
        }
        return DateTime.SpecifyKind(fallback.Date, DateTimeKind.Utc);
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
}

public sealed record TcmbRate(
    string CurrencyCode,
    decimal ForexBuying,
    decimal ForexSelling,
    int Unit,
    DateTime EffectiveDate)
{
    public decimal NormalizedBuying => Unit > 0 ? Math.Round(ForexBuying / Unit, 6, MidpointRounding.ToEven) : ForexBuying;
    public decimal NormalizedSelling => Unit > 0 ? Math.Round(ForexSelling / Unit, 6, MidpointRounding.ToEven) : ForexSelling;
}
