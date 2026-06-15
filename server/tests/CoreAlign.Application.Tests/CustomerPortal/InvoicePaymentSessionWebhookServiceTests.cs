using CoreAlign.Application.Billing.Payments;
using CoreAlign.Application.CustomerPortal.Payments;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Payments;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.CustomerPortal;

public class InvoicePaymentSessionWebhookServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly IPaymentSessionRepository _sessions = Substitute.For<IPaymentSessionRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly InvoicePaymentSessionWebhookService _sut;

    public InvoicePaymentSessionWebhookServiceTests()
    {
        _tenant.PushScope(Arg.Any<Guid>()).Returns(Substitute.For<IDisposable>());
        _sequences.ConsumeAsync(DocumentSequenceType.PaymentNumber, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("PAY-2026-1");

        _sut = new InvoicePaymentSessionWebhookService(
            _sessions, _invoices, _payments, _sequences, _tenant, _uow, _publisher, NullLogger<InvoicePaymentSessionWebhookService>.Instance);
    }

    [Fact]
    public async Task TryProcess_returns_null_when_no_session_match()
    {
        _sessions.GetByIntentAsync("mock", "intent-x", Arg.Any<CancellationToken>())
            .Returns((PaymentSession?)null);

        var result = await _sut.TryProcessAsync("mock", new WebhookProcessingResult("intent-x", PaymentIntentStatus.Succeeded, null, null, "{}"), default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Succeeded_records_payment_and_applies_to_invoice()
    {
        var (invoice, session) = BuildPair(total: 500m, sessionAmount: 500m);
        _sessions.GetByIntentAsync("mock", session.IntentId, Arg.Any<CancellationToken>()).Returns(session);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        Payment? captured = null;
        await _payments.AddAsync(Arg.Do<Payment>(p => captured = p), Arg.Any<CancellationToken>());

        var result = await _sut.TryProcessAsync("mock", new WebhookProcessingResult(session.IntentId, PaymentIntentStatus.Succeeded, "PAYREF", null, "{}"), default);

        result.Should().NotBeNull();
        captured.Should().NotBeNull();
        captured!.Amount.Should().Be(500m);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.AmountPaid.Should().Be(500m);
        session.Status.Should().Be(PaymentSessionStatus.Succeeded);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Succeeded_is_idempotent_when_session_already_succeeded()
    {
        var (_, session) = BuildPair(total: 500m, sessionAmount: 500m);
        session.MarkSucceeded("ref-prior");
        _sessions.GetByIntentAsync("mock", session.IntentId, Arg.Any<CancellationToken>()).Returns(session);

        var result = await _sut.TryProcessAsync("mock", new WebhookProcessingResult(session.IntentId, PaymentIntentStatus.Succeeded, "new-ref", null, "{}"), default);

        result.Should().NotBeNull();
        await _payments.DidNotReceive().AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Failed_marks_session_failed_without_payment()
    {
        var (_, session) = BuildPair(total: 100m, sessionAmount: 100m);
        _sessions.GetByIntentAsync("mock", session.IntentId, Arg.Any<CancellationToken>()).Returns(session);

        var result = await _sut.TryProcessAsync("mock", new WebhookProcessingResult(session.IntentId, PaymentIntentStatus.Failed, null, "Card declined", "{}"), default);

        result.Should().NotBeNull();
        session.Status.Should().Be(PaymentSessionStatus.Failed);
        session.FailureReason.Should().Be("Card declined");
        await _payments.DidNotReceive().AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Succeeded_with_overpayment_caps_application_and_flags_refund_in_message()
    {
        var (invoice, session) = BuildPair(total: 500m, sessionAmount: 500m);
        invoice.RecordPayment(300m, DateTime.UtcNow);
        _sessions.GetByIntentAsync("mock", session.IntentId, Arg.Any<CancellationToken>()).Returns(session);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        Payment? captured = null;
        await _payments.AddAsync(Arg.Do<Payment>(p => captured = p), Arg.Any<CancellationToken>());

        var result = await _sut.TryProcessAsync("mock", new WebhookProcessingResult(session.IntentId, PaymentIntentStatus.Succeeded, "PAYREF", null, "{}"), default);

        result.Should().NotBeNull();
        captured.Should().NotBeNull();
        captured!.Amount.Should().Be(200m);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.AmountPaid.Should().Be(500m);
        session.Status.Should().Be(PaymentSessionStatus.Succeeded);
        result!.Message.Should().Contain("overpaid");
        result.Message.Should().Contain("300");
    }

    [Fact]
    public async Task Succeeded_when_invoice_already_fully_paid_closes_session_without_payment()
    {
        var (invoice, session) = BuildPair(total: 500m, sessionAmount: 500m);
        invoice.RecordPayment(500m, DateTime.UtcNow);
        _sessions.GetByIntentAsync("mock", session.IntentId, Arg.Any<CancellationToken>()).Returns(session);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await _sut.TryProcessAsync("mock", new WebhookProcessingResult(session.IntentId, PaymentIntentStatus.Succeeded, "PAYREF", null, "{}"), default);

        result.Should().NotBeNull();
        session.Status.Should().Be(PaymentSessionStatus.Succeeded);
        await _payments.DidNotReceive().AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        result!.Message.Should().Contain("already paid");
    }

    [Fact]
    public async Task Cancelled_marks_session_cancelled()
    {
        var (_, session) = BuildPair(total: 100m, sessionAmount: 100m);
        _sessions.GetByIntentAsync("mock", session.IntentId, Arg.Any<CancellationToken>()).Returns(session);

        await _sut.TryProcessAsync("mock", new WebhookProcessingResult(session.IntentId, PaymentIntentStatus.Cancelled, null, "abandoned", "{}"), default);

        session.Status.Should().Be(PaymentSessionStatus.Cancelled);
    }

    private static (Invoice invoice, PaymentSession session) BuildPair(decimal total, decimal sessionAmount)
    {
        var invoice = new Invoice("INV-T", CustomerId, "Acme", "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        invoice.Lines.Add(new InvoiceLine("SKU", "Fixture", null, 1m, total));
        invoice.Recalculate();
        invoice.Issue("INV-T");

        var session = new PaymentSession(invoice.Id, CustomerId, UserId, "mock", $"intent_{Guid.NewGuid():N}", sessionAmount, "TRY", null)
        {
            TenantId = TenantId,
        };
        return (invoice, session);
    }
}
