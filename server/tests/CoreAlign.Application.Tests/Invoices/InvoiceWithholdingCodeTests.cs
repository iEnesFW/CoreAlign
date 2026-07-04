using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Tests.Invoices;

public class InvoiceWithholdingCodeTests
{
    private static InvoiceLine BuildLine(
        decimal quantity,
        decimal unitPrice,
        decimal taxRatePercent,
        decimal withholdingRatePercent = 0m,
        int? numerator = null,
        int? denominator = null,
        string? code = null)
    {
        var line = new InvoiceLine("SKU-1", "Hizmet", null, quantity, unitPrice);
        line.ApplyPricing(
            quantity: quantity,
            unitPrice: unitPrice,
            lineDiscountPercent: 0m,
            lineDiscountAmount: 0m,
            taxRatePercent: taxRatePercent,
            taxRateId: null,
            isTaxInclusive: false,
            withholdingRatePercent: withholdingRatePercent,
            uomId: null,
            uomCode: null,
            description: null,
            revenueAccountCode: null,
            costCenter: null,
            project: null,
            originOrderLineId: null,
            withholdingTaxCodeId: numerator.HasValue ? Guid.NewGuid() : null,
            withholdingCode: code,
            withholdingNumerator: numerator,
            withholdingDenominator: denominator);
        return line;
    }

    [Fact]
    public void Withholding_code_fraction_is_computed_from_vat_amount()
    {
        var line = BuildLine(1m, 1000m, 20m, numerator: 7, denominator: 10, code: "617");

        line.TaxAmount.Should().Be(200m);
        line.WithholdingAmount.Should().Be(140m);
        line.WithholdingCode.Should().Be("617");
    }

    [Fact]
    public void Legacy_percent_withholding_is_unchanged_when_no_code_is_set()
    {
        var line = BuildLine(1m, 1000m, 20m, withholdingRatePercent: 5m);

        line.WithholdingAmount.Should().Be(60m);
    }

    [Fact]
    public void Code_fraction_takes_precedence_over_legacy_percent()
    {
        var line = BuildLine(1m, 1000m, 20m, withholdingRatePercent: 5m, numerator: 9, denominator: 10, code: "606");

        line.WithholdingAmount.Should().Be(180m);
    }

    [Fact]
    public void Zero_vat_line_with_code_produces_zero_withholding()
    {
        var line = BuildLine(1m, 1000m, 0m, numerator: 7, denominator: 10, code: "617");

        line.WithholdingAmount.Should().Be(0m);
    }

    [Fact]
    public void Invoice_total_subtracts_code_based_withholding()
    {
        var invoice = new Invoice("INV-1", Guid.NewGuid(), "Müşteri", "TRY");
        var line = BuildLine(1m, 1000m, 20m, numerator: 7, denominator: 10, code: "617");

        invoice.ReplaceLines([line]);

        invoice.TaxTotal.Should().Be(200m);
        invoice.WithholdingTotal.Should().Be(140m);
        invoice.Total.Should().Be(1060m);
    }
}
