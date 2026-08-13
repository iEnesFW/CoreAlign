using CoreAlign.Application.Invoices.EventHandlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Invoices;

public class CreditNoteReversalFidelityTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private readonly ICustomerLedgerRepository _ledger = Substitute.For<ICustomerLedgerRepository>();
    private readonly ICustomerTransactionRepository _transactions =
        Substitute.For<ICustomerTransactionRepository>();

    private readonly InvoiceCancelledLedgerHandler _sut;

    public CreditNoteReversalFidelityTests()
    {
        _ledger.GetLastRunningBalanceAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(0m);
        _sut = new InvoiceCancelledLedgerHandler(_ledger, _transactions);
    }

    private static InvoiceCancelledEvent Cancelled(InvoiceType type, decimal amount) =>
        new(TenantId, Guid.NewGuid(), CustomerId, "DOC-1", amount, "TRY", WasIssued: true,
            DateTime.UtcNow, type);

    [Fact]
    public async Task Cancelling_a_credit_note_debits_the_ledger_to_undo_its_credit()
    {
        CustomerLedgerEntry? captured = null;
        await _ledger.AddAsync(Arg.Do<CustomerLedgerEntry>(e => captured = e), Arg.Any<CancellationToken>());

        await _sut.Handle(Cancelled(InvoiceType.CreditNote, 1200m), default);

        captured.Should().NotBeNull();
        captured!.EntryType.Should().Be(LedgerEntryType.Debit);
        captured.Amount.Should().Be(1200m);
    }

    [Fact]
    public async Task Cancelling_a_sales_invoice_still_credits_the_ledger()
    {
        CustomerLedgerEntry? captured = null;
        await _ledger.AddAsync(Arg.Do<CustomerLedgerEntry>(e => captured = e), Arg.Any<CancellationToken>());

        await _sut.Handle(Cancelled(InvoiceType.SalesInvoice, 1200m), default);

        captured.Should().NotBeNull();
        captured!.EntryType.Should().Be(LedgerEntryType.Credit);
    }

    [Fact]
    public async Task Cancelling_a_credit_note_reverses_the_legacy_transaction_sign_too()
    {
        CustomerTransaction? captured = null;
        await _transactions.AddAsync(Arg.Do<CustomerTransaction>(t => captured = t), Arg.Any<CancellationToken>());

        await _sut.Handle(Cancelled(InvoiceType.CreditNote, 1200m), default);

        captured.Should().NotBeNull();
        captured!.Amount.Should().Be(1200m);
    }

    [Fact]
    public async Task A_never_issued_document_posts_nothing()
    {
        await _sut.Handle(
            new InvoiceCancelledEvent(TenantId, Guid.NewGuid(), CustomerId, "DOC-2", 500m, "TRY",
                WasIssued: false, DateTime.UtcNow, InvoiceType.SalesInvoice),
            default);

        await _ledger.DidNotReceive().AddAsync(Arg.Any<CustomerLedgerEntry>(), Arg.Any<CancellationToken>());
    }
}
