using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Invoices;

public class MarkInvoicePaidCommandHandlerTests
{
    private readonly IInvoiceRepository _invoiceRepository = Substitute.For<IInvoiceRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly MarkInvoiceAsPaidCommandHandler _sut;

    public MarkInvoicePaidCommandHandlerTests()
    {
        _sut = new MarkInvoiceAsPaidCommandHandler(_invoiceRepository, _unitOfWork);
    }

    [Fact]
    public async Task Marks_issued_invoice_paid_and_raises_event()
    {
        var invoice = BuildIssuedInvoice();
        _invoiceRepository.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await _sut.Handle(new MarkInvoiceAsPaidCommand(invoice.Id), default);

        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidAtUtc.Should().NotBeNull();
        invoice.DomainEvents.OfType<InvoicePaidEvent>().Should().ContainSingle();
        _invoiceRepository.Received(1).Update(invoice);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Throws_when_already_paid()
    {
        var invoice = BuildIssuedInvoice();
        invoice.ClearDomainEvents();
        invoice.MarkAsPaid(DateTime.UtcNow);
        _invoiceRepository.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        Func<Task> act = () => _sut.Handle(new MarkInvoiceAsPaidCommand(invoice.Id), default);
        await act.Should().ThrowAsync<InvoiceStatusTransitionException>();
    }

    [Fact]
    public async Task Throws_when_invoice_not_found()
    {
        _invoiceRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Invoice?)null);
        Func<Task> act = () => _sut.Handle(new MarkInvoiceAsPaidCommand(Guid.NewGuid()), default);
        await act.Should().ThrowAsync<InvoiceNotFoundException>();
    }

    private static Invoice BuildIssuedInvoice()
    {
        var invoice = new Invoice("INV-1", Guid.NewGuid(), "Acme", "USD")
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
        };
        invoice.Lines.Add(new InvoiceLine(Guid.NewGuid(), "SKU", "Item", 1m, 100m));
        invoice.Issue("INV-1");
        invoice.ClearDomainEvents();
        return invoice;
    }
}
