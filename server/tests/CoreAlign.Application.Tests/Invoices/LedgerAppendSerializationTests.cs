using CoreAlign.Application.Invoices.EventHandlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Invoices;

public class LedgerAppendSerializationTests
{
    [Fact]
    public async Task Posting_ledger_entry_acquires_append_lock_before_reading_last_balance()
    {
        var ledger = Substitute.For<ICustomerLedgerRepository>();
        var legacy = Substitute.For<ICustomerTransactionRepository>();
        ledger.GetLastRunningBalanceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(100m);

        var customerId = Guid.NewGuid();
        var handler = new InvoiceIssuedLedgerHandler(ledger, legacy);
        var evt = new InvoiceIssuedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            customerId,
            null,
            "INV-1",
            InvoiceType.SalesInvoice,
            50m,
            "TRY",
            DateTime.UtcNow);

        await handler.Handle(evt, CancellationToken.None);

        Received.InOrder(() =>
        {
            ledger.AcquireAppendLockAsync(customerId, Arg.Any<CancellationToken>());
            ledger.GetLastRunningBalanceAsync(customerId, Arg.Any<CancellationToken>());
            ledger.AddAsync(Arg.Any<CustomerLedgerEntry>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Running_balance_is_computed_from_the_locked_read()
    {
        var ledger = Substitute.For<ICustomerLedgerRepository>();
        var legacy = Substitute.For<ICustomerTransactionRepository>();
        ledger.GetLastRunningBalanceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(100m);

        CustomerLedgerEntry? posted = null;
        ledger.When(l => l.AddAsync(Arg.Any<CustomerLedgerEntry>(), Arg.Any<CancellationToken>()))
            .Do(ci => posted = ci.Arg<CustomerLedgerEntry>());

        var handler = new InvoiceIssuedLedgerHandler(ledger, legacy);
        var evt = new InvoiceIssuedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "INV-2",
            InvoiceType.SalesInvoice,
            50m,
            "TRY",
            DateTime.UtcNow);

        await handler.Handle(evt, CancellationToken.None);

        posted.Should().NotBeNull();
        posted!.RunningBalanceAfter.Should().Be(150m);
    }
}
