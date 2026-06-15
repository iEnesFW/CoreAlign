using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.Idempotency;

[Collection(IdempotencyTestCollection.Name)]
public class ApplyPaymentIdempotencyTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid InvoiceId = Guid.NewGuid();
    private static readonly Guid OtherInvoiceId = Guid.NewGuid();

    [Fact]
    public void Apply_SameInvoiceTwice_DoesNotDoubleApply()
    {
        var payment = BuildConfirmedPayment(amount: 100m);

        var first = payment.Apply(InvoiceId, 40m, invoiceRemaining: 40m);
        var retry = payment.Apply(InvoiceId, 40m, invoiceRemaining: 40m);

        payment.Applications.Should().HaveCount(1, "applying the same invoice again must be a no-op");
        payment.AppliedAmount.Should().Be(40m, "AppliedAmount must not be double-counted on retry");
        retry.Should().BeSameAs(first, "retry returns the existing application instead of creating a new one");
    }

    [Fact]
    public void Apply_DifferentInvoices_RecordsEachOnce()
    {
        var payment = BuildConfirmedPayment(amount: 100m);

        payment.Apply(InvoiceId, 40m, invoiceRemaining: 40m);
        payment.Apply(OtherInvoiceId, 30m, invoiceRemaining: 30m);

        payment.Applications.Should().HaveCount(2, "distinct invoices each get their own application");
        payment.AppliedAmount.Should().Be(70m);
    }

    private static Payment BuildConfirmedPayment(decimal amount)
    {
        var payment = new Payment(
            paymentNumber: $"PAY-{Guid.NewGuid():N}",
            customerId: CustomerId,
            customerNameSnapshot: "Acme",
            direction: PaymentDirection.CustomerReceipt,
            paymentDate: DateTime.UtcNow.Date,
            method: PaymentMethod.BankTransfer,
            amount: amount,
            currency: "TRY");
        payment.Confirm(null);
        return payment;
    }
}
