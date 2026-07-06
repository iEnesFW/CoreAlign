using CoreAlign.Application.Payments.Commands;
using CoreAlign.Application.Payments.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Payments;

public class UnapplyPaymentIdempotencyTests
{
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly UnapplyPaymentHandler _sut;

    public UnapplyPaymentIdempotencyTests()
    {
        _sut = new UnapplyPaymentHandler(_payments, _invoices);
    }

    [Fact]
    public async Task Unapply_retry_is_idempotent_and_reverses_the_invoice_once()
    {
        var invoiceId = Guid.NewGuid();
        var payment = BuildConfirmedPayment(100m);
        payment.Apply(invoiceId, 100m, 100m);
        var applicationId = payment.Applications.Single().Id;
        payment.ClearDomainEvents();

        _payments.GetWithApplicationsAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _invoices.GetByIdAsync(invoiceId, Arg.Any<CancellationToken>()).Returns((Invoice?)null);

        // First unapply removes the application.
        await _sut.Handle(new UnapplyPaymentCommand(payment.Id, applicationId), default);
        payment.Applications.Should().BeEmpty();

        // Network retry / double-click with the SAME application id: must not throw a 400 — it
        // replays the current state and does not look up (and re-reverse) the invoice again.
        var retry = await _sut.Handle(new UnapplyPaymentCommand(payment.Id, applicationId), default);

        retry.Should().NotBeNull();
        await _invoices.Received(1).GetByIdAsync(invoiceId, Arg.Any<CancellationToken>());
    }

    private static Payment BuildConfirmedPayment(decimal amount)
    {
        var payment = new Payment(
            paymentNumber: $"PAY-{Guid.NewGuid():N}",
            customerId: Guid.NewGuid(),
            customerNameSnapshot: "Acme",
            direction: PaymentDirection.CustomerReceipt,
            paymentDate: DateTime.UtcNow.Date,
            method: PaymentMethod.BankTransfer,
            amount: amount,
            currency: "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
        };
        payment.Confirm(null);
        payment.ClearDomainEvents();
        return payment;
    }
}
