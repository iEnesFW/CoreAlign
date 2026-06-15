using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Tests.Invoices;

/// <summary>
/// Rule 16: line totals round to 4dp with Math.Round and the header total must
/// equal the sum of the rounded lines — no penny drift. These tests feed baskets
/// of repeating-decimal unit prices and quantities and assert Σ lines == header.
/// </summary>
public class MoneyRoundingBoundaryTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static InvoiceLine TaxedLine(decimal quantity, decimal unitPrice, decimal taxPercent)
    {
        var line = new InvoiceLine(Guid.NewGuid(), "SKU", "Widget", quantity, unitPrice)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        line.ApplyPricing(
            quantity: quantity,
            unitPrice: unitPrice,
            lineDiscountPercent: 0m,
            lineDiscountAmount: 0m,
            taxRatePercent: taxPercent,
            taxRateId: null,
            isTaxInclusive: false,
            withholdingRatePercent: 0m,
            uomId: null,
            uomCode: null,
            description: null,
            revenueAccountCode: null,
            costCenter: null,
            project: null,
            originOrderLineId: null);
        return line;
    }

    private static Invoice InvoiceWith(params InvoiceLine[] lines)
    {
        var invoice = new Invoice("INV-R", CustomerId, "Acme", "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        invoice.ReplaceLines(lines);
        return invoice;
    }

    [Fact]
    public void Line_subtotal_is_rounded_to_four_decimal_places()
    {
        // 3 x 33.3333 = 99.9999 → already 4dp; 7 x 33.33335 rounds.
        var line = TaxedLine(7m, 33.33335m, 0m);

        line.LineSubtotal.Should().Be(Math.Round(7m * 33.33335m, 4));
        decimal.Round(line.LineSubtotal, 4).Should().Be(line.LineSubtotal);
    }

    [Fact]
    public void Header_subtotal_equals_sum_of_rounded_line_subtotals_for_repeating_decimals()
    {
        var l1 = TaxedLine(3m, 33.333m, 0m);
        var l2 = TaxedLine(3m, 33.333m, 0m);
        var l3 = TaxedLine(3m, 33.333m, 0m);
        var invoice = InvoiceWith(l1, l2, l3);

        var sumOfLines = Math.Round(l1.LineSubtotal + l2.LineSubtotal + l3.LineSubtotal, 4);
        invoice.Subtotal.Should().Be(sumOfLines);
    }

    [Fact]
    public void Header_total_equals_sum_of_rounded_line_totals_with_tax()
    {
        var l1 = TaxedLine(3m, 33.333m, 18m);
        var l2 = TaxedLine(5m, 7.777m, 18m);
        var l3 = TaxedLine(11m, 1.111m, 18m);
        var invoice = InvoiceWith(l1, l2, l3);

        // Header Total = TaxableTotal + TaxTotal (no discount/shipping/withholding).
        var expectedTaxable = Math.Round(l1.LineNetAmount + l2.LineNetAmount + l3.LineNetAmount, 4);
        var expectedTax = Math.Round(l1.TaxAmount + l2.TaxAmount + l3.TaxAmount, 4);

        invoice.TaxableTotal.Should().Be(expectedTaxable);
        invoice.TaxTotal.Should().Be(expectedTax);
        invoice.Total.Should().Be(Math.Round(expectedTaxable + expectedTax, 4));
    }

    [Fact]
    public void Tax_total_is_sum_of_per_line_rounded_tax_not_tax_on_rounded_sum()
    {
        // Per-line rounding is the contract: header TaxTotal = Σ Math.Round(lineTax).
        var l1 = TaxedLine(1m, 0.005m, 18m);
        var l2 = TaxedLine(1m, 0.005m, 18m);
        var invoice = InvoiceWith(l1, l2);

        invoice.TaxTotal.Should().Be(Math.Round(l1.TaxAmount + l2.TaxAmount, 4));
    }

    [Fact]
    public void No_penny_drift_across_a_large_basket_of_repeating_decimal_lines()
    {
        var lines = Enumerable.Range(0, 17)
            .Select(i => TaxedLine(quantity: 3m + i % 4, unitPrice: 16.6667m, taxPercent: i % 2 == 0 ? 18m : 8m))
            .ToArray();
        var invoice = InvoiceWith(lines);

        var sumNet = Math.Round(lines.Sum(l => l.LineNetAmount), 4);
        var sumTax = Math.Round(lines.Sum(l => l.TaxAmount), 4);

        invoice.TaxableTotal.Should().Be(sumNet);
        invoice.TaxTotal.Should().Be(sumTax);
        invoice.Total.Should().Be(Math.Round(sumNet + sumTax, 4));
    }

    [Fact]
    public void Credit_note_lines_preserve_rounding_and_balance_to_header()
    {
        var origin = InvoiceWith(
            TaxedLine(9m, 11.111m, 18m),
            TaxedLine(4m, 7.7777m, 18m));
        origin.Issue("INV-R");

        var creditLines = origin.Lines.Select(src =>
        {
            var cl = new InvoiceLine(src.ProductId ?? Guid.Empty, src.ProductSku, src.ProductName, src.Quantity, src.UnitPrice)
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
            };
            cl.ApplyPricing(
                quantity: src.Quantity,
                unitPrice: src.UnitPrice,
                lineDiscountPercent: 0m,
                lineDiscountAmount: 0m,
                taxRatePercent: src.TaxRatePercent,
                taxRateId: src.TaxRateId,
                isTaxInclusive: false,
                withholdingRatePercent: 0m,
                uomId: null,
                uomCode: null,
                description: null,
                revenueAccountCode: null,
                costCenter: null,
                project: null,
                originOrderLineId: src.Id);
            return cl;
        }).ToList();

        var creditNote = Invoice.IssueCreditNote(origin, "CN-R", DateTime.UtcNow, creditLines, "return", null, null);

        creditNote.Total.Should().Be(origin.Total);
        creditNote.TaxTotal.Should().Be(origin.TaxTotal);
        creditNote.Total.Should().Be(Math.Round(creditNote.TaxableTotal + creditNote.TaxTotal, 4));
    }
}
