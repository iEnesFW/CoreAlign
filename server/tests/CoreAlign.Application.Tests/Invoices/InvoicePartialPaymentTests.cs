using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Invoices;

/// <summary>
/// AR settlement invariants on <see cref="Invoice.RecordPayment"/>: a partial
/// receipt advances AmountPaid, leaves AmountDue == Total - AmountPaid and moves
/// the invoice to PartiallyPaid; a final receipt that closes the balance flips it
/// to Paid with AmountDue == 0; and a receipt larger than the outstanding balance
/// is rejected (AR can never be driven negative). The invoice carries a non-zero
/// ShippingCost + RoundingAdjustment so the billed Total — the figure payments
/// settle against — is the gross the customer actually owes.
/// </summary>
public class InvoicePartialPaymentTests
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

    private static Invoice IssuedInvoice()
    {
        var invoice = new Invoice("INV-1", CustomerId, "Acme", "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        invoice.ReplaceLines(new[] { TaxedLine(1m, 1000m, 20m) });
        invoice.UpdateDetails(
            issueDate: DateTime.UtcNow,
            dueDate: DateTime.UtcNow.AddDays(30),
            postingDate: DateTime.UtcNow.Date,
            exchangeRate: 1m,
            paymentTermsId: null,
            paymentTermsNetDaysSnapshot: null,
            headerDiscountPercent: 0m,
            headerDiscountAmount: 0m,
            shippingCost: 50m,
            roundingAdjustment: 0.30m,
            internalNotes: null,
            publicNotes: null,
            termsAndConditions: null,
            notes: null);
        invoice.Issue("INV-1");
        return invoice;
    }

    [Fact]
    public void Partial_payment_advances_amount_paid_and_leaves_remaining_due_partially_paid()
    {
        var invoice = IssuedInvoice();
        invoice.Total.Should().Be(1250.30m);

        invoice.RecordPayment(500m, DateTime.UtcNow);

        invoice.AmountPaid.Should().Be(500m);
        invoice.AmountDue.Should().Be(750.30m);
        invoice.AmountDue.Should().Be(invoice.Total - invoice.AmountPaid);
        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
    }

    [Fact]
    public void Final_payment_closing_the_balance_marks_paid_with_zero_due()
    {
        var invoice = IssuedInvoice();
        invoice.RecordPayment(500m, DateTime.UtcNow);

        invoice.RecordPayment(750.30m, DateTime.UtcNow);

        invoice.AmountPaid.Should().Be(invoice.Total);
        invoice.AmountDue.Should().Be(0m);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Over_payment_beyond_outstanding_balance_is_rejected()
    {
        var invoice = IssuedInvoice();
        invoice.RecordPayment(1000m, DateTime.UtcNow);

        Action act = () => invoice.RecordPayment(300m, DateTime.UtcNow);

        act.Should().Throw<CannotOverPayInvoiceException>();
        // The rejected payment leaves the prior state untouched.
        invoice.AmountPaid.Should().Be(1000m);
        invoice.AmountDue.Should().Be(250.30m);
        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
    }

    [Fact]
    public void A_single_payment_for_the_full_total_marks_paid()
    {
        var invoice = IssuedInvoice();

        invoice.RecordPayment(invoice.Total, DateTime.UtcNow);

        invoice.AmountPaid.Should().Be(invoice.Total);
        invoice.AmountDue.Should().Be(0m);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
    }
}
