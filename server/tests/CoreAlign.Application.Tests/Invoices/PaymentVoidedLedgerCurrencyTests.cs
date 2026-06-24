using CoreAlign.Application.Invoices.EventHandlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Invoices;

public class PaymentVoidedLedgerCurrencyTests
{
    private readonly ICustomerLedgerRepository _ledger = Substitute.For<ICustomerLedgerRepository>();
    private readonly PaymentVoidedLedgerHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid PaymentId = Guid.NewGuid();

    public PaymentVoidedLedgerCurrencyTests()
    {
        _sut = new PaymentVoidedLedgerHandler(_ledger);
    }

    [Fact]
    public async Task Voided_usd_payment_posts_reversal_in_payment_currency()
    {
        CustomerLedgerEntry? captured = null;
        await _ledger.AddAsync(Arg.Do<CustomerLedgerEntry>(e => captured = e), Arg.Any<CancellationToken>());

        var notification = new PaymentVoidedEvent(
            TenantId,
            PaymentId,
            CustomerId,
            "PMT-USD-0001",
            150m,
            "USD",
            DateTime.UtcNow);

        await _sut.Handle(notification, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Currency.Should().Be("USD");
        captured.Currency.Should().NotBe("TRY");
        captured.EntryType.Should().Be(LedgerEntryType.Debit);
        captured.SourceType.Should().Be(LedgerSourceType.PaymentReversal);
        captured.SourceDocumentId.Should().Be(PaymentId);
        captured.Amount.Should().Be(150m);
    }

    [Fact]
    public async Task Voided_try_payment_posts_reversal_in_try()
    {
        CustomerLedgerEntry? captured = null;
        await _ledger.AddAsync(Arg.Do<CustomerLedgerEntry>(e => captured = e), Arg.Any<CancellationToken>());

        var notification = new PaymentVoidedEvent(
            TenantId,
            PaymentId,
            CustomerId,
            "PMT-TRY-0001",
            200m,
            "TRY",
            DateTime.UtcNow);

        await _sut.Handle(notification, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Currency.Should().Be("TRY");
    }
}
