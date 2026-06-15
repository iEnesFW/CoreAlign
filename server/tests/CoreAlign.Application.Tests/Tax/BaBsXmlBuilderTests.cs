using System.Xml.Linq;
using CoreAlign.Application.Tax;

namespace CoreAlign.Application.Tests.Tax;

public class BaBsXmlBuilderTests
{
    [Fact]
    public void Build_with_only_sales_emits_bs_kayit()
    {
        var aggregates = new BaBsAggregates(
            Sales: new[]
            {
                new BaBsCounterpartyAggregate("1234567890", "Acme Ltd", 3, 12000m, 2160m),
            },
            Purchases: Array.Empty<BaBsCounterpartyAggregate>());

        var doc = BaBsXmlBuilder.Build(2026, 5, aggregates);

        doc.Root!.Name.LocalName.Should().Be("BaBsBildirimi");
        doc.Root.Element("Bs")!.Elements("Kayit").Should().HaveCount(1);
        doc.Root.Element("Ba")!.Elements("Kayit").Should().BeEmpty();
        doc.Root.Element("Bs")!.Element("Kayit")!.Element("Vkn")!.Value.Should().Be("1234567890");
    }

    [Fact]
    public void Build_with_only_purchases_emits_ba_kayit()
    {
        var aggregates = new BaBsAggregates(
            Sales: Array.Empty<BaBsCounterpartyAggregate>(),
            Purchases: new[]
            {
                new BaBsCounterpartyAggregate("9876543210", "Supplier Co", 2, 8000m, 1440m),
            });

        var doc = BaBsXmlBuilder.Build(2026, 5, aggregates);

        doc.Root!.Element("Ba")!.Elements("Kayit").Should().HaveCount(1);
        doc.Root.Element("Bs")!.Elements("Kayit").Should().BeEmpty();
    }

    [Fact]
    public void Build_orders_records_by_total_amount_descending()
    {
        var aggregates = new BaBsAggregates(
            Sales: new[]
            {
                new BaBsCounterpartyAggregate("111", "Small", 1, 5500m, 990m),
                new BaBsCounterpartyAggregate("222", "Big", 5, 50000m, 9000m),
                new BaBsCounterpartyAggregate("333", "Mid", 2, 12000m, 2160m),
            },
            Purchases: Array.Empty<BaBsCounterpartyAggregate>());

        var doc = BaBsXmlBuilder.Build(2026, 5, aggregates);

        var names = doc.Root!.Element("Bs")!.Elements("Kayit")
            .Select(e => e.Element("Unvan")!.Value).ToArray();
        names.Should().BeEquivalentTo(new[] { "Big", "Mid", "Small" }, opts => opts.WithStrictOrdering());
    }

    [Fact]
    public void Build_with_null_tax_number_emits_empty_vkn()
    {
        var aggregates = new BaBsAggregates(
            Sales: new[]
            {
                new BaBsCounterpartyAggregate(null, "No VKN Customer", 1, 6000m, 1080m),
            },
            Purchases: Array.Empty<BaBsCounterpartyAggregate>());

        var doc = BaBsXmlBuilder.Build(2026, 5, aggregates);

        doc.Root!.Element("Bs")!.Element("Kayit")!.Element("Vkn")!.Value.Should().Be(string.Empty);
    }

    [Fact]
    public void Build_emits_period_donem()
    {
        var aggregates = new BaBsAggregates(
            Array.Empty<BaBsCounterpartyAggregate>(),
            Array.Empty<BaBsCounterpartyAggregate>());

        var doc = BaBsXmlBuilder.Build(2026, 3, aggregates);

        doc.Root!.Element("Donem")!.Value.Should().Be("2026-03");
        XDocument.Parse(doc.ToString()).Should().NotBeNull();
    }
}
