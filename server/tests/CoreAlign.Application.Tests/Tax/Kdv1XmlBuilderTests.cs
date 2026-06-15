using System.Xml.Linq;
using CoreAlign.Application.Tax;

namespace CoreAlign.Application.Tests.Tax;

public class Kdv1XmlBuilderTests
{
    [Fact]
    public void Build_with_single_rate_emits_one_kdv_block()
    {
        var aggregates = new Kdv1Aggregates(
            TotalTaxableBase: 1000m,
            TotalTaxAmount: 200m,
            TotalWithholdingAmount: 0m,
            RateBreakdown: new[] { new TaxRateAggregate(20m, 1000m, 200m) });

        var doc = Kdv1XmlBuilder.Build(2026, 5, aggregates);

        doc.Root!.Name.LocalName.Should().Be("Beyanname");
        doc.Root.Element("Donem")!.Value.Should().Be("2026-05");
        doc.Root.Element("MatrahToplam")!.Value.Should().Be("1000");
        doc.Root.Element("HesaplananKDV")!.Value.Should().Be("200");
        doc.Root.Elements("Kdv").Should().HaveCount(1);

        var roundTrip = XDocument.Parse(doc.ToString());
        roundTrip.Root!.Element("Donem")!.Value.Should().Be("2026-05");
    }

    [Fact]
    public void Build_with_multiple_rates_orders_by_rate_ascending()
    {
        var aggregates = new Kdv1Aggregates(
            TotalTaxableBase: 3000m,
            TotalTaxAmount: 380m,
            TotalWithholdingAmount: 0m,
            RateBreakdown: new[]
            {
                new TaxRateAggregate(20m, 1000m, 200m),
                new TaxRateAggregate(1m, 500m, 5m),
                new TaxRateAggregate(10m, 1500m, 150m),
            });

        var doc = Kdv1XmlBuilder.Build(2026, 5, aggregates);
        var rates = doc.Root!.Elements("Kdv").Select(e => decimal.Parse(e.Element("Oran")!.Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();

        rates.Should().BeEquivalentTo(new[] { 1m, 10m, 20m }, opts => opts.WithStrictOrdering());
    }

    [Fact]
    public void Build_with_zero_invoices_emits_empty_breakdown()
    {
        var aggregates = new Kdv1Aggregates(0m, 0m, 0m, Array.Empty<TaxRateAggregate>());

        var doc = Kdv1XmlBuilder.Build(2026, 5, aggregates);

        doc.Root!.Elements("Kdv").Should().BeEmpty();
        doc.Root.Element("MatrahToplam")!.Value.Should().Be("0");
        doc.Root.Element("HesaplananKDV")!.Value.Should().Be("0");
    }

    [Fact]
    public void Build_emits_tevkifat_toplam_for_withholding()
    {
        var aggregates = new Kdv1Aggregates(
            TotalTaxableBase: 1000m,
            TotalTaxAmount: 180m,
            TotalWithholdingAmount: 90m,
            RateBreakdown: new[] { new TaxRateAggregate(18m, 1000m, 180m) });

        var doc = Kdv1XmlBuilder.Build(2026, 6, aggregates);

        doc.Root!.Element("TevkifatToplam")!.Value.Should().Be("90");
    }

    [Fact]
    public void Build_emits_xml_declaration()
    {
        var aggregates = new Kdv1Aggregates(0m, 0m, 0m, Array.Empty<TaxRateAggregate>());
        var doc = Kdv1XmlBuilder.Build(2026, 1, aggregates);

        doc.Declaration.Should().NotBeNull();
        doc.Declaration!.Encoding.Should().Be("UTF-8");
    }
}
