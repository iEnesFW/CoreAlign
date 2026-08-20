using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Invoices;

public class CancelInvoiceCommandHandlerTests
{
    private readonly IInvoiceRepository _invoiceRepository = Substitute.For<IInvoiceRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CancelInvoiceCommandHandler _sut;

    public CancelInvoiceCommandHandlerTests()
    {
        _sut = new CancelInvoiceCommandHandler(_invoiceRepository, Substitute.For<IOrderRepository>(), _unitOfWork);
    }

    [Fact]
    public async Task Cancels_issued_invoice_and_raises_event_with_was_issued_true()
    {
        var invoice = BuildIssuedInvoice();
        _invoiceRepository.GetWithLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        await _sut.Handle(new CancelInvoiceCommand(invoice.Id), default);

        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
        var ev = invoice.DomainEvents.OfType<InvoiceCancelledEvent>().Should().ContainSingle().Subject;
        ev.WasIssued.Should().BeTrue();
    }

    [Fact]
    public async Task Cancels_draft_invoice_with_was_issued_false()
    {
        var invoice = new Invoice("INV-2", Guid.NewGuid(), "Acme", "USD")
        {
            Id = Guid.NewGuid(),
        };
        _invoiceRepository.GetWithLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        await _sut.Handle(new CancelInvoiceCommand(invoice.Id), default);

        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
        var ev = invoice.DomainEvents.OfType<InvoiceCancelledEvent>().Should().ContainSingle().Subject;
        ev.WasIssued.Should().BeFalse();
    }

    [Fact]
    public async Task Throws_when_paid()
    {
        var invoice = BuildIssuedInvoice();
        invoice.ClearDomainEvents();
        invoice.MarkAsPaid(DateTime.UtcNow);
        _invoiceRepository.GetWithLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        Func<Task> act = () => _sut.Handle(new CancelInvoiceCommand(invoice.Id), default);
        await act.Should().ThrowAsync<InvoiceStatusTransitionException>();
    }

    private static Invoice BuildIssuedInvoice()
    {
        var invoice = new Invoice("INV-1", Guid.NewGuid(), "Acme", "USD")
        {
            Id = Guid.NewGuid(),
        };
        invoice.Lines.Add(new InvoiceLine(Guid.NewGuid(), "SKU", "Item", 1m, 100m));
        invoice.Issue("INV-1");
        invoice.ClearDomainEvents();
        return invoice;
    }
}
