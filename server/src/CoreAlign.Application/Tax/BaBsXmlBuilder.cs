using System.Globalization;
using System.Xml.Linq;

namespace CoreAlign.Application.Tax;

public record BaBsCounterpartyAggregate(
    string? TaxNumber,
    string CounterpartyName,
    int DocumentCount,
    decimal TotalAmount,
    decimal TaxAmount);

public record BaBsAggregates(
    IReadOnlyList<BaBsCounterpartyAggregate> Sales,
    IReadOnlyList<BaBsCounterpartyAggregate> Purchases);

public static class BaBsXmlBuilder
{
    public static XDocument Build(int year, int month, BaBsAggregates aggregates)
    {
        if (aggregates is null) throw new ArgumentNullException(nameof(aggregates));

        var ci = CultureInfo.InvariantCulture;
        var donem = $"{year:D4}-{month:D2}";

        var bsElement = new XElement(
            "Bs",
            aggregates.Sales
                .OrderByDescending(s => s.TotalAmount)
                .Select(s => new XElement(
                    "Kayit",
                    new XElement("Vkn", s.TaxNumber ?? string.Empty),
                    new XElement("Unvan", s.CounterpartyName),
                    new XElement("BelgeSayisi", s.DocumentCount.ToString(ci)),
                    new XElement("Tutar", s.TotalAmount.ToString(ci)),
                    new XElement("KdvTutar", s.TaxAmount.ToString(ci)))));

        var baElement = new XElement(
            "Ba",
            aggregates.Purchases
                .OrderByDescending(p => p.TotalAmount)
                .Select(p => new XElement(
                    "Kayit",
                    new XElement("Vkn", p.TaxNumber ?? string.Empty),
                    new XElement("Unvan", p.CounterpartyName),
                    new XElement("BelgeSayisi", p.DocumentCount.ToString(ci)),
                    new XElement("Tutar", p.TotalAmount.ToString(ci)),
                    new XElement("KdvTutar", p.TaxAmount.ToString(ci)))));

        var bildirim = new XElement(
            "BaBsBildirimi",
            new XElement("Donem", donem),
            bsElement,
            baElement);

        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), bildirim);
    }
}
