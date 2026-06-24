using CoreAlign.Application.Invoices;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.Invoices;

public class CreditNoteCalculationsTests
{
    [Fact]
    public void Sums_credited_quantity_per_origin_line_across_notes()
    {
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();

        var totals = CreditNoteCalculations.SumCreditedByOriginLine(new[]
        {
            CreditNote(InvoiceStatus.Issued, (lineA, 2m), (lineB, 1m)),
            CreditNote(InvoiceStatus.Issued, (lineA, 3m)),
        });

        totals[lineA].Should().Be(5m);
        totals[lineB].Should().Be(1m);
    }

    [Fact]
    public void Skips_cancelled_and_void_credit_notes()
    {
        var line = Guid.NewGuid();

        var totals = CreditNoteCalculations.SumCreditedByOriginLine(new[]
        {
            CreditNote(InvoiceStatus.Issued, (line, 2m)),
            CreditNote(InvoiceStatus.Cancelled, (line, 4m)),
            CreditNote(InvoiceStatus.Void, (line, 8m)),
        });

        totals[line].Should().Be(2m);
    }

    [Fact]
    public void Ignores_lines_without_origin_line_id()
    {
        var creditNote = new Invoice("CN", Guid.NewGuid(), "Acme", "TRY", InvoiceType.CreditNote)
        {
            Id = Guid.NewGuid(),
        };
        var orphan = new InvoiceLine(Guid.NewGuid(), "SKU", "Name", 5m, 1m) { Id = Guid.NewGuid() };
        creditNote.Lines.Add(orphan);
        creditNote.Issue("CN");

        var totals = CreditNoteCalculations.SumCreditedByOriginLine(new[] { creditNote });

        totals.Should().BeEmpty();
    }

    private static Invoice CreditNote(InvoiceStatus status, params (Guid OriginLineId, decimal Quantity)[] lines)
    {
        var creditNote = new Invoice("CN", Guid.NewGuid(), "Acme", "TRY", InvoiceType.CreditNote)
        {
            Id = Guid.NewGuid(),
        };
        foreach (var (originLineId, quantity) in lines)
        {
            var line = new InvoiceLine(Guid.NewGuid(), "SKU", "Name", quantity, 1m) { Id = Guid.NewGuid() };
            line.ApplyPricing(
                quantity: quantity,
                unitPrice: 1m,
                lineDiscountPercent: 0m,
                lineDiscountAmount: 0m,
                taxRatePercent: 0m,
                taxRateId: null,
                isTaxInclusive: false,
                withholdingRatePercent: 0m,
                uomId: null,
                uomCode: null,
                description: null,
                revenueAccountCode: null,
                costCenter: null,
                project: null,
                originOrderLineId: originLineId);
            creditNote.Lines.Add(line);
        }
        creditNote.Issue("CN");
        if (status == InvoiceStatus.Cancelled) creditNote.Cancel(DateTime.UtcNow);
        else if (status == InvoiceStatus.Void) creditNote.Void(null, null);
        return creditNote;
    }
}
