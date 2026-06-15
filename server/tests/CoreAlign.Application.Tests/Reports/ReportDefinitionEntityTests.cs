using CoreAlign.Domain.Entities.Reporting;

namespace CoreAlign.Application.Tests.Reports;

public class ReportDefinitionEntityTests
{
    [Fact]
    public void Ctor_trims_name_and_stores_jsons()
    {
        var def = new ReportDefinition(
            name: "  Sales by Customer  ",
            entityType: ReportEntityType.Invoice,
            dimensionsJson: "[\"CustomerName\"]",
            measuresJson: "[{\"field\":\"Total\",\"function\":\"Sum\"}]",
            filtersJson: "[]",
            sortByJson: null,
            limit: 100);

        def.Name.Should().Be("Sales by Customer");
        def.EntityType.Should().Be(ReportEntityType.Invoice);
        def.Limit.Should().Be(100);
    }

    [Fact]
    public void Ctor_throws_on_empty_name()
    {
        Action act = () => new ReportDefinition(
            name: "  ",
            entityType: ReportEntityType.Invoice,
            dimensionsJson: "[]",
            measuresJson: "[]",
            filtersJson: "[]",
            sortByJson: null,
            limit: null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_overwrites_collections_and_bumps_timestamp()
    {
        var def = new ReportDefinition(
            name: "v1",
            entityType: ReportEntityType.Order,
            dimensionsJson: "[]",
            measuresJson: "[]",
            filtersJson: "[]",
            sortByJson: null,
            limit: null);
        var earlier = def.UpdatedAtUtc;
        Thread.Sleep(5);
        def.Update("v2", "desc", "[\"Status\"]", "[]", "[]", null, 50);
        def.Name.Should().Be("v2");
        def.Description.Should().Be("desc");
        def.DimensionsJson.Should().Contain("Status");
        def.Limit.Should().Be(50);
        def.UpdatedAtUtc.Should().BeAfter(earlier);
    }
}
