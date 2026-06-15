using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.BugHuntC;

/// <summary>
/// HUNTER C — C-2 (CRITICAL): MarkInvoiceAsPaidCommandHandler guards only
/// {Paid, Cancelled}; it omits Void. Invoice.MarkAsPaid() itself has NO guard. A VOIDED
/// invoice (whose AR was already reversed in the GL by the void) can be resurrected to
/// Paid: it sets AmountPaid = Total and emits InvoicePaidEvent, which the ledger handler
/// turns into a phantom payment transaction against a customer whose AR no longer exists.
/// Illegal terminal-state transition (Void → Paid). INVARIANTS 16 (no sign flip / no
/// double-post) + state-machine self-guard rule.
/// </summary>
public class MarkVoidInvoicePaidTests
{
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly MarkInvoiceAsPaidCommandHandler _sut;

    public MarkVoidInvoicePaidTests()
    {
        _sut = new MarkInvoiceAsPaidCommandHandler(_invoices, _uow);
    }

    [Fact]
    public async Task MarkAsPaid_OnVoidInvoice_IsRejected()
    {
        var invoice = BuildVoidInvoice();
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        Func<Task> act = () => _sut.Handle(new MarkInvoiceAsPaidCommand(invoice.Id), default);

        // A voided (already AR-reversed) invoice must NOT be markable as paid. On current
        // code this succeeds, flips Void → Paid and emits a phantom InvoicePaidEvent.
        await act.Should().ThrowAsync<Exception>(
            "marking a voided invoice as paid is an illegal terminal-state transition");
        invoice.Status.Should().Be(InvoiceStatus.Void, "the void must stand");
        invoice.DomainEvents.OfType<InvoicePaidEvent>().Should()
            .BeEmpty("a voided invoice must never raise InvoicePaidEvent");
    }

    private static Invoice BuildVoidInvoice()
    {
        var invoice = new Invoice("INV-1", Guid.NewGuid(), "Acme", "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
        };
        invoice.Lines.Add(new InvoiceLine(Guid.NewGuid(), "SKU", "Item", 1m, 100m));
        invoice.Issue("INV-1");
        invoice.Void("issued in error", creditNoteId: null);
        invoice.ClearDomainEvents();
        return invoice;
    }
}
