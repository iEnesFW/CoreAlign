using System.Globalization;
using System.Xml.Linq;

namespace CoreAlign.Application.Tax;

public record TaxRateAggregate(decimal RatePercent, decimal TaxableBase, decimal TaxAmount);

public record Kdv1Aggregates(
    decimal TotalTaxableBase,
    decimal TotalTaxAmount,
    decimal TotalWithholdingAmount,
    IReadOnlyList<TaxRateAggregate> RateBreakdown);

public static class Kdv1XmlBuilder
{
    public static XDocument Build(int year, int month, Kdv1Aggregates aggregates)
    {
        if (aggregates is null) throw new ArgumentNullException(nameof(aggregates));

        var ci = CultureInfo.InvariantCulture;
        var donem = $"{year:D4}-{month:D2}";

        var kdvElements = aggregates.RateBreakdown
            .OrderBy(r => r.RatePercent)
            .Select(r => new XElement(
                "Kdv",
                new XElement("Oran", r.RatePercent.ToString(ci)),
                new XElement("Matrah", r.TaxableBase.ToString(ci)),
                new XElement("Vergi", r.TaxAmount.ToString(ci))));

        var beyanname = new XElement(
            "Beyanname",
            new XElement("Donem", donem),
            new XElement("MatrahToplam", aggregates.TotalTaxableBase.ToString(ci)),
            new XElement("HesaplananKDV", aggregates.TotalTaxAmount.ToString(ci)),
            new XElement("TevkifatToplam", aggregates.TotalWithholdingAmount.ToString(ci)),
            kdvElements);

        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), beyanname);
    }
}
