using CoreAlign.Application.Reports.Custom;
using CoreAlign.Domain.Entities.Reporting;

namespace CoreAlign.Application.Tests.Reports;

public class FieldCatalogServiceTests
{
    private readonly FieldCatalogService _sut = new();

    [Fact]
    public void GetCatalog_returns_all_supported_entity_groups()
    {
        var catalog = _sut.GetCatalog();
        catalog.Select(g => g.EntityType).Should().Contain(new[]
        {
            "Invoice", "Order", "Customer", "Product", "StockMovement",
        });
    }

    [Fact]
    public void Get_returns_localized_labels_for_invoice_total()
    {
        var invoice = _sut.Get(ReportEntityType.Invoice);
        invoice.Should().NotBeNull();
        var total = invoice!.Fields.Single(f => f.Key == "Total");
        total.LabelEn.Should().Be("Total");
        total.LabelTr.Should().Be("Toplam");
        total.DataType.Should().Be("Decimal");
    }

    [Fact]
    public void Validate_returns_true_for_known_field()
    {
        _sut.Validate(ReportEntityType.Invoice, "Status").Should().BeTrue();
    }

    [Fact]
    public void Validate_returns_false_for_unknown_field()
    {
        _sut.Validate(ReportEntityType.Invoice, "DROP TABLE").Should().BeFalse();
    }
}
