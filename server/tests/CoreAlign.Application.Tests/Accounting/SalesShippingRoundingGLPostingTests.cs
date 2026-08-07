using CoreAlign.Application.Accounting.EventHandlers;
using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Accounting;

/// <summary>
/// FIN footing guard: the AR-side sales journal must debit AccountsReceivable for
/// the exact amount the customer is billed — invoice.Total — not the taxable+tax
/// base. Recharged freight (ShippingCost) and any RoundingAdjustment the invoice
/// applies have to flow through, otherwise AR is understated and a customer paying
/// invoice.Total drives AR negative while shipping income is never recognized.
/// These tests drive the real <see cref="InvoiceIssuedGLHandler"/> over a real
/// <see cref="Invoice"/> carrying non-zero shipping + rounding and assert
/// Σdebit==Σcredit AND Debit(120) == invoice.Total, plus the reversing path nets
/// to zero and a negative rounding adjustment books a rounding loss.
/// </summary>
public class SalesShippingRoundingGLPostingTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();

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

    private static Invoice IssuedInvoice(
        decimal taxableUnitPrice,
        decimal taxPercent,
        decimal shippingCost,
        decimal roundingAdjustment,
        InvoiceType type = InvoiceType.SalesInvoice)
    {
        var invoice = new Invoice("INV-1", CustomerId, "Acme", "TRY", type)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        invoice.ReplaceLines(new[] { TaxedLine(1m, taxableUnitPrice, taxPercent) });
        invoice.UpdateDetails(
            issueDate: DateTime.UtcNow,
            dueDate: DateTime.UtcNow.AddDays(30),
            postingDate: DateTime.UtcNow.Date,
            exchangeRate: 1m,
            paymentTermsId: null,
            paymentTermsNetDaysSnapshot: null,
            headerDiscountPercent: 0m,
            headerDiscountAmount: 0m,
            shippingCost: shippingCost,
            roundingAdjustment: roundingAdjustment,
            internalNotes: null,
            publicNotes: null,
            termsAndConditions: null,
            notes: null);
        invoice.Issue("INV-1");
        return invoice;
    }

    private InvoiceIssuedGLHandler IssuedHandler() => new(_outbox);
    private InvoiceVoidedGLHandler VoidedHandler() => new(_outbox, _invoices);

    /// <summary>
    /// Drives the event the aggregate ACTUALLY raised, not a hand-built one, and leaves the invoice
    /// repository unstubbed: this event is dispatched inside the SaveChanges that inserts the
    /// invoice, so anything the handler needs has to arrive on the event itself.
    /// </summary>
    private async Task<GLPostingRequest> CaptureIssueAsync(Invoice invoice)
    {
        var issued = invoice.DomainEvents.OfType<InvoiceIssuedEvent>().Single();
        GLPostingRequest? captured = null;
        await _outbox.EnqueueAsync(Arg.Do<GLPostingRequest>(r => captured = r), Arg.Any<CancellationToken>());

        await IssuedHandler().Handle(issued, default);

        captured.Should().NotBeNull();
        return captured!;
    }

    private async Task<GLPostingRequest> CaptureVoidAsync(Invoice invoice)
    {
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        GLPostingRequest? captured = null;
        await _outbox.EnqueueAsync(Arg.Do<GLPostingRequest>(r => captured = r), Arg.Any<CancellationToken>());

        await VoidedHandler().Handle(
            new InvoiceVoidedEvent(TenantId, invoice.Id, CustomerId, invoice.InvoiceNumber,
                invoice.Total, invoice.Currency, "void", DateTime.UtcNow),
            default);

        captured.Should().NotBeNull();
        return captured!;
    }

    private static GLPostingLine Line(GLPostingRequest r, GLPostingKey key) =>
        r.Lines.Single(l => l.Key == key);

    private static decimal NetDebit(GLPostingRequest r, GLPostingKey key) =>
        r.Lines.Where(l => l.Key == key).Sum(l => l.Debit - l.Credit);

    [Fact]
    public async Task Issue_with_shipping_and_rounding_debits_AR_for_the_full_invoice_total_balanced()
    {
        // Taxable 1000, Tax 200, Withholding 0, Shipping 50, Rounding 0.30 → Total 1250.30.
        var invoice = IssuedInvoice(taxableUnitPrice: 1000m, taxPercent: 20m, shippingCost: 50m, roundingAdjustment: 0.30m);
        invoice.TaxableTotal.Should().Be(1000m);
        invoice.TaxTotal.Should().Be(200m);
        invoice.Total.Should().Be(1250.30m);

        var request = await CaptureIssueAsync(invoice);

        request.SourceType.Should().Be(JournalSourceType.SalesInvoice);
        request.Lines.Sum(l => l.Debit).Should().Be(request.Lines.Sum(l => l.Credit));
        Line(request, GLPostingKey.AccountsReceivable).Debit.Should().Be(invoice.Total);
        Line(request, GLPostingKey.AccountsReceivable).Debit.Should().Be(1250.30m);
        // Freight + rounding gain are recognized on the credit side, not swallowed.
        Line(request, GLPostingKey.ShippingIncome).Credit.Should().Be(50m);
        Line(request, GLPostingKey.RoundingGain).Credit.Should().Be(0.30m);
        Line(request, GLPostingKey.SalesRevenue).Credit.Should().Be(1000m);
        Line(request, GLPostingKey.OutputVat).Credit.Should().Be(200m);
    }

    [Fact]
    public async Task Negative_rounding_adjustment_books_a_rounding_loss_and_still_foots_to_total()
    {
        // Rounding -0.45 → Total 1249.55. The loss is a debit; AR still equals Total.
        var invoice = IssuedInvoice(taxableUnitPrice: 1000m, taxPercent: 20m, shippingCost: 50m, roundingAdjustment: -0.45m);
        invoice.Total.Should().Be(1249.55m);

        var request = await CaptureIssueAsync(invoice);

        request.Lines.Sum(l => l.Debit).Should().Be(request.Lines.Sum(l => l.Credit));
        Line(request, GLPostingKey.AccountsReceivable).Debit.Should().Be(invoice.Total);
        Line(request, GLPostingKey.RoundingLoss).Debit.Should().Be(0.45m);
        Line(request, GLPostingKey.RoundingGain).Credit.Should().Be(0m);
    }

    [Fact]
    public async Task Issue_then_void_nets_every_account_to_zero()
    {
        var invoice = IssuedInvoice(taxableUnitPrice: 1000m, taxPercent: 20m, shippingCost: 50m, roundingAdjustment: 0.30m);

        var issue = await CaptureIssueAsync(invoice);
        var reversal = await CaptureVoidAsync(invoice);

        reversal.SourceType.Should().Be(JournalSourceType.SalesInvoiceReversal);
        foreach (var key in Enum.GetValues<GLPostingKey>())
        {
            (NetDebit(issue, key) + NetDebit(reversal, key)).Should().Be(0m,
                $"issue + void must net account role {key} to zero");
        }
        // And the reversal credits AR for the full billed total.
        Line(reversal, GLPostingKey.AccountsReceivable).Credit.Should().Be(invoice.Total);
        reversal.Lines.Sum(l => l.Debit).Should().Be(reversal.Lines.Sum(l => l.Credit));
    }

    /// <summary>
    /// RED-BEFORE: the handler used to re-read the invoice through IInvoiceRepository. That event
    /// is dispatched INSIDE the SaveChanges that inserts the invoice, so an invoice created and
    /// issued in one command was not queryable yet — the read returned null and the handler booked
    /// NOTHING. Measured on the dev database: 18 issued invoices with customer-ledger rows and zero
    /// journal entries. With an empty repository the handler must still enqueue the full entry.
    /// </summary>
    [Fact]
    public async Task An_invoice_issued_before_its_row_is_queryable_still_books_to_the_ledger()
    {
        var invoice = IssuedInvoice(taxableUnitPrice: 1000m, taxPercent: 20m, shippingCost: 50m, roundingAdjustment: 0.30m);
        _invoices.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Invoice?)null);

        var request = await CaptureIssueAsync(invoice);

        request.SourceType.Should().Be(JournalSourceType.SalesInvoice);
        Line(request, GLPostingKey.AccountsReceivable).Debit.Should().Be(invoice.Total);
        Line(request, GLPostingKey.SalesRevenue).Credit.Should().Be(invoice.TaxableTotal);
        Line(request, GLPostingKey.OutputVat).Credit.Should().Be(invoice.TaxTotal);
        request.Lines.Sum(l => l.Debit).Should().Be(request.Lines.Sum(l => l.Credit));
        await _invoices.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
