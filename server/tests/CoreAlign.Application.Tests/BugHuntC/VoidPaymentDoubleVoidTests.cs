using CoreAlign.Application.Payments.Commands;
using CoreAlign.Application.Payments.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.BugHuntC;

/// <summary>
/// HUNTER C — C-1 (CRITICAL): VoidPaymentHandler / Payment.Void() has NO terminal-state
/// guard. Voiding an already-Void payment a second time re-runs Payment.Void(), which
/// re-emits PaymentVoidedEvent. That event drives PaymentVoidedGLHandler (posts a cash
/// reversal journal: DR AR / CR Cash) AND PaymentVoidedLedgerHandler (posts a Debit
/// customer-ledger entry). A second void therefore DOUBLE-reverses the cash movement —
/// money is not conserved. INVARIANTS 16 (no double-post) + 26 (idempotency on retry).
/// </summary>
public class VoidPaymentDoubleVoidTests
{
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly VoidPaymentHandler _sut;

    public VoidPaymentDoubleVoidTests()
    {
        _sut = new VoidPaymentHandler(_payments, _invoices, _uow);
    }

    [Fact]
    public async Task DoubleVoid_EmitsPaymentVoidedEventOnlyOnce()
    {
        var payment = BuildConfirmedPayment(100m);
        _payments.GetWithApplicationsAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _invoices.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Invoice>());

        // First void — legitimate.
        await _sut.Handle(new VoidPaymentCommand(payment.Id, "customer dispute"), default);
        payment.Status.Should().Be(PaymentStatus.Void);

        // Network retry / double-click: the SAME already-Void payment is voided again.
        await _sut.Handle(new VoidPaymentCommand(payment.Id, "customer dispute"), default);

        // A terminal payment must not re-emit the cash-reversal event. On current code it
        // does, so the GL/ledger reversal posts twice → cash double-credited.
        payment.DomainEvents.OfType<PaymentVoidedEvent>().Should()
            .ContainSingle("voiding an already-Void payment must be a no-op, not a second cash reversal");
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
