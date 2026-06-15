using CoreAlign.Domain.Entities.Reporting;

namespace CoreAlign.Application.Tests.Reports;

public class FieldCatalogTests
{
    [Theory]
    [InlineData(ReportEntityType.Invoice, "CustomerName")]
    [InlineData(ReportEntityType.Invoice, "Total")]
    [InlineData(ReportEntityType.Order, "Status")]
    [InlineData(ReportEntityType.Customer, "Code")]
    [InlineData(ReportEntityType.Product, "Sku")]
    [InlineData(ReportEntityType.StockMovement, "Quantity")]
    public void Known_fields_are_present(ReportEntityType entity, string key)
    {
        FieldCatalog.IsKnown(entity, key).Should().BeTrue();
    }

    [Theory]
    [InlineData(ReportEntityType.Invoice, "SecretSqlInjection")]
    [InlineData(ReportEntityType.Order, "TotallyMadeUp")]
    public void Unknown_fields_are_rejected(ReportEntityType entity, string key)
    {
        FieldCatalog.IsKnown(entity, key).Should().BeFalse();
    }

    [Fact]
    public void Supported_entities_list_covers_all_enum_values()
    {
        var supported = FieldCatalog.SupportedEntities();
        supported.Should().Contain(new[]
        {
            ReportEntityType.Invoice,
            ReportEntityType.Order,
            ReportEntityType.Customer,
            ReportEntityType.Product,
            ReportEntityType.StockMovement,
        });
    }

    [Fact]
    public void Total_field_allows_sum_aggregation_on_invoice()
    {
        var desc = FieldCatalog.Find(ReportEntityType.Invoice, "Total");
        desc.Should().NotBeNull();
        desc!.AllowedAggregations.Should().Contain(ReportMeasureFunction.Sum);
        desc.IsMeasureEligible.Should().BeTrue();
    }

    [Fact]
    public void CustomerName_only_allows_count_aggregation()
    {
        var desc = FieldCatalog.Find(ReportEntityType.Invoice, "CustomerName");
        desc.Should().NotBeNull();
        desc!.AllowedAggregations.Should().ContainSingle().Which.Should().Be(ReportMeasureFunction.Count);
        desc.IsMeasureEligible.Should().BeFalse();
    }
}
