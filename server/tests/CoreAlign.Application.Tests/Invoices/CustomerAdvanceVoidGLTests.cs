using CoreAlign.Application.Accounting.EventHandlers;
using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Invoices;

// Voiding a CUSTOMER advance receipt must reverse to CustomerAdvanceReceived(340) — the
// account the advance was booked to — NOT AccountsReceivable(120), which never held the
// prepayment. An offset advance must additionally reverse each CustomerAdvanceApplied
// entry (DR 120 / CR 340). Mirrors the vendor-side VoidVendorPaymentHandler fix.
public class CustomerAdvanceVoidGLTests
{
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IGLPostingOutbox _outbox = Substitute.For<IGLPostingOutbox>();
    private readonly List<GLPostingRequest> _captured = new();
    private readonly PaymentVoidedGLHandler _sut;

    public CustomerAdvanceVoidGLTests()
    {
        _outbox.WhenForAnyArgs(o => o.EnqueueAsync(default!, default))
            .Do(ci => _captured.Add(ci.Arg<GLPostingRequest>()));
        _sut = new PaymentVoidedGLHandler(_outbox, _payments);
    }

    private static Payment Receipt(bool isAdvance) =>
        new("PAY-1", Guid.NewGuid(), "Cust", PaymentDirection.CustomerReceipt, DateTime.UtcNow,
            PaymentMethod.BankTransfer, 1000m, "TRY", isAdvance) { Id = Guid.NewGuid() };

    private PaymentVoidedEvent VoidEvent(Payment p) =>
        new(Guid.NewGuid(), p.Id, p.CustomerId, p.PaymentNumber, p.Amount, p.Currency, DateTime.UtcNow);

    [Fact]
    public async Task Voiding_a_regular_payment_reverses_to_accounts_receivable()
    {
        var payment = Receipt(isAdvance: false);
        _payments.GetWithApplicationsAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        await _sut.Handle(VoidEvent(payment), default);

        var req = _captured.Should().ContainSingle().Subject;
        req.SourceType.Should().Be(JournalSourceType.CustomerPaymentReversal);
        req.Lines.Single(l => l.Key == GLPostingKey.AccountsReceivable).Debit.Should().Be(1000m);
        req.Lines.Should().NotContain(l => l.Key == GLPostingKey.CustomerAdvanceReceived);
        AssertBalanced(req);
    }

    [Fact]
    public async Task Voiding_an_unoffset_advance_reverses_to_340_not_ar()
    {
        var payment = Receipt(isAdvance: true);
        _payments.GetWithApplicationsAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        await _sut.Handle(VoidEvent(payment), default);

        var req = _captured.Should().ContainSingle().Subject;
        req.SourceType.Should().Be(JournalSourceType.CustomerAdvanceReceivedReversal);
        req.Lines.Single(l => l.Key == GLPostingKey.CustomerAdvanceReceived).Debit.Should().Be(1000m);
        req.Lines.Should().NotContain(l => l.Key == GLPostingKey.AccountsReceivable);
        AssertBalanced(req);
    }

    [Fact]
    public async Task Voiding_an_offset_advance_reverses_both_the_offset_and_the_advance()
    {
        var payment = Receipt(isAdvance: true);
        payment.Confirm(null);
        payment.Apply(Guid.NewGuid(), 600m, 1000m);
        _payments.GetWithApplicationsAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        await _sut.Handle(VoidEvent(payment), default);

        _captured.Should().HaveCount(2);
        _captured.Should().OnlyContain(r => IsBalanced(r));

        var offsetReversal = _captured.Single(r => r.SourceType == JournalSourceType.CustomerAdvanceAppliedReversal);
        offsetReversal.Lines.Single(l => l.Key == GLPostingKey.AccountsReceivable).Debit.Should().Be(600m);
        offsetReversal.Lines.Single(l => l.Key == GLPostingKey.CustomerAdvanceReceived).Credit.Should().Be(600m);

        var advanceReversal = _captured.Single(r => r.SourceType == JournalSourceType.CustomerAdvanceReceivedReversal);
        advanceReversal.Lines.Single(l => l.Key == GLPostingKey.CustomerAdvanceReceived).Debit.Should().Be(1000m);
    }

    private static bool IsBalanced(GLPostingRequest r) =>
        r.Lines.Sum(l => l.Debit) == r.Lines.Sum(l => l.Credit);

    private static void AssertBalanced(GLPostingRequest r) =>
        r.Lines.Sum(l => l.Debit).Should().Be(r.Lines.Sum(l => l.Credit));
}
