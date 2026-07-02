using CoreAlign.Application.Accounting.EventHandlers;
using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Invoices;

public class WriteOffInvoiceTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Writing_off_an_open_invoice_sets_status_and_emits_event_for_the_amount_due()
    {
        var invoice = BuildIssuedInvoice(total: 100m, amountPaid: 30m);

        invoice.WriteOff(DateTime.UtcNow, "uncollectible");

        invoice.Status.Should().Be(InvoiceStatus.WrittenOff);
        var evt = invoice.DomainEvents.OfType<InvoiceWrittenOffEvent>().Should().ContainSingle().Subject;
        evt.Amount.Should().Be(70m, "only the outstanding receivable is written off, not the full invoice");
        evt.Reason.Should().Be("uncollectible");
    }

    [Fact]
    public void Writing_off_a_draft_invoice_is_rejected()
    {
        var invoice = new Invoice("INV-D", CustomerId, "Acme", "TRY") { Id = Guid.NewGuid(), TenantId = TenantId };
        invoice.Lines.Add(new InvoiceLine("SKU", "Fixture", null, quantity: 1m, unitPrice: 50m));
        invoice.Recalculate();

        var act = () => invoice.WriteOff(DateTime.UtcNow, null);

        act.Should().Throw<InvoiceStatusTransitionException>();
    }

    [Fact]
    public void Writing_off_a_fully_paid_invoice_is_rejected()
    {
        var invoice = BuildIssuedInvoice(total: 100m, amountPaid: 100m);

        var act = () => invoice.WriteOff(DateTime.UtcNow, null);

        act.Should().Throw<InvoiceStatusTransitionException>();
    }

    [Fact]
    public void Second_write_off_is_an_idempotent_no_op()
    {
        var invoice = BuildIssuedInvoice(total: 100m, amountPaid: 0m);
        invoice.WriteOff(DateTime.UtcNow, null);
        invoice.ClearDomainEvents();

        invoice.WriteOff(DateTime.UtcNow, null);

        invoice.Status.Should().Be(InvoiceStatus.WrittenOff);
        invoice.DomainEvents.OfType<InvoiceWrittenOffEvent>().Should().BeEmpty("a terminal write-off must not re-emit");
    }

    [Fact]
    public async Task GL_handler_posts_a_balanced_doubtful_debt_expense_against_receivable()
    {
        var outbox = Substitute.For<IGLPostingOutbox>();
        var invoices = Substitute.For<IInvoiceRepository>();
        var invoice = BuildIssuedInvoice(total: 100m, amountPaid: 30m);
        invoices.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(invoice);

        GLPostingRequest? captured = null;
        await outbox.EnqueueAsync(Arg.Do<GLPostingRequest>(r => captured = r), Arg.Any<CancellationToken>());

        var handler = new InvoiceWrittenOffGLHandler(outbox, invoices);
        await handler.Handle(
            new InvoiceWrittenOffEvent(TenantId, invoice.Id, CustomerId, "INV-1", 70m, "TRY", "bad debt", DateTime.UtcNow),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.SourceType.Should().Be(JournalSourceType.InvoiceWriteOff);
        var debit = captured.Lines.Single(l => l.Key == GLPostingKey.DoubtfulDebtExpense);
        var credit = captured.Lines.Single(l => l.Key == GLPostingKey.AccountsReceivable);
        debit.Debit.Should().Be(70m);
        credit.Credit.Should().Be(70m);
        captured.Lines.Sum(l => l.Debit).Should().Be(captured.Lines.Sum(l => l.Credit), "the write-off journal must balance");
    }

    private static Invoice BuildIssuedInvoice(decimal total, decimal amountPaid)
    {
        var invoice = new Invoice($"INV-{Guid.NewGuid():N}".Substring(0, 12), CustomerId, "Acme", "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        invoice.Lines.Add(new InvoiceLine("SKU", "Fixture", null, quantity: 1m, unitPrice: total));
        invoice.Recalculate();
        invoice.Issue(invoice.InvoiceNumber);
        if (amountPaid > 0m)
        {
            invoice.RecordPayment(amountPaid, DateTime.UtcNow);
        }
        return invoice;
    }
}
