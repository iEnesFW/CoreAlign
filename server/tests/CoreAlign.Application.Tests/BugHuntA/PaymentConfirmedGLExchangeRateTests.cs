using CoreAlign.Application.Accounting.EventHandlers;
using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.BugHuntA;

/// <summary>
/// HUNTER A — A-3: a foreign-currency customer receipt must book its AR side at the
/// SAME exchange rate the invoice used on issuance. PaymentConfirmedEvent now carries
/// the payment's ExchangeRate and PaymentConfirmedGLHandler threads it into the
/// GLPostingRequest. RED before the fix (ExchangeRate omitted → defaulted to 1m, so a
/// USD invoice issued at rate 33 credited AR 33×amount but the payment only reversed
/// 1×amount, leaving an un-clearing residual).
/// </summary>
public class PaymentConfirmedGLExchangeRateTests
{
    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();

    [Fact]
    public async Task PaymentConfirmedGL_books_AR_at_the_payments_exchange_rate_not_one()
    {
        const decimal rate = 33m;
        const decimal amount = 1000m;

        var payment = new Payment(
            "PAY-1", Guid.NewGuid(), "Customer",
            PaymentDirection.CustomerReceipt, DateTime.UtcNow,
            PaymentMethod.BankTransfer, amount, "USD");
        payment.UpdateDetails(
            paymentDate: DateTime.UtcNow, postingDate: DateTime.UtcNow.Date,
            method: PaymentMethod.BankTransfer, amount: amount, exchangeRate: rate,
            bankAccountInfo: null, referenceNumber: null, checkNumber: null,
            checkDueDate: null, notes: null);

        _payments.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(payment);

        GLPostingRequest? enqueued = null;
        await _outbox.EnqueueAsync(Arg.Do<GLPostingRequest>(r => enqueued = r), Arg.Any<CancellationToken>());

        var sut = new PaymentConfirmedGLHandler(_outbox, _payments);
        var evt = new PaymentConfirmedEvent(
            payment.TenantId, payment.Id, payment.CustomerId, payment.PaymentNumber,
            PaymentDirection.CustomerReceipt, amount, "USD", DateTime.UtcNow, rate);

        await sut.Handle(evt, CancellationToken.None);

        enqueued.Should().NotBeNull();
        enqueued!.ExchangeRate.Should().Be(rate,
            "the payment AR side must translate at the same rate the invoice booked, else a foreign-currency residual never clears");
        enqueued.Currency.Should().Be("USD");
    }
}
