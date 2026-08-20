using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Invoices;

// Generating an invoice from an order advances OrderLine.QuantityInvoiced, and ExistsForOrderAsync
// blocks a second invoice for the same order. Cancelling therefore left the order permanently
// unbillable: the shipped goods could never be charged for and the revenue was stranded.
public class CancelledInvoiceReleasesOrderTests
{
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly CancelInvoiceCommandHandler _sut;

    public CancelledInvoiceReleasesOrderTests()
    {
        _sut = new CancelInvoiceCommandHandler(_invoices, _orders, _uow);
    }

    private static (Order Order, OrderLine Line) InvoicedOrder(decimal quantity, decimal invoiced)
    {
        var order = new Order("SO-1", CustomerId, DateTime.UtcNow, "TRY") { Id = OrderId, TenantId = Guid.NewGuid() };
        var line = new OrderLine(Guid.NewGuid(), "SKU-A", "Widget", quantity, 100m) { Id = Guid.NewGuid() };
        order.Lines.Add(line);
        order.Submit();
        order.Approve(Guid.NewGuid());
        order.MarkAllocated(null);
        order.ChangeStatus(OrderStatus.Shipped);
        line.RecordShipment(quantity);
        line.RecordInvoice(invoiced);
        return (order, line);
    }

    private static Invoice IssuedInvoiceFor(Order order, OrderLine orderLine, decimal quantity)
    {
        var invoice = new Invoice("INV-1", CustomerId, "Acme", "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = order.TenantId,
        };
        var line = new InvoiceLine(orderLine.ProductId, orderLine.ProductSku, orderLine.ProductName, quantity, 100m);
        line.ApplyPricing(
            quantity: quantity, unitPrice: 100m, lineDiscountPercent: 0m, lineDiscountAmount: 0m,
            taxRatePercent: 0m, taxRateId: null, isTaxInclusive: false, withholdingRatePercent: 0m,
            uomId: null, uomCode: null, description: null, revenueAccountCode: null,
            costCenter: null, project: null, originOrderLineId: orderLine.Id);
        invoice.ReplaceLines(new[] { line });
        invoice.AttachToOrder(order.Id);
        invoice.Issue("INV-1");
        invoice.ClearDomainEvents();
        return invoice;
    }

    [Fact]
    public async Task Cancelling_a_from_order_invoice_gives_the_quantity_back_to_the_order()
    {
        var (order, line) = InvoicedOrder(quantity: 10m, invoiced: 10m);
        var invoice = IssuedInvoiceFor(order, line, 10m);
        _invoices.GetWithLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        _orders.GetWithLinesAsync(OrderId, Arg.Any<CancellationToken>()).Returns(order);

        await _sut.Handle(new CancelInvoiceCommand(invoice.Id), default);

        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
        line.QuantityInvoiced.Should().Be(0m);
        line.QuantityRemainingToInvoice.Should().Be(10m, "the shipped goods can be billed again");
        line.Status.Should().Be(OrderLineStatus.Shipped);
        _orders.Received(1).Update(order);
    }

    [Fact]
    public async Task Cancelling_a_partial_invoice_only_releases_what_it_had_claimed()
    {
        var (order, line) = InvoicedOrder(quantity: 10m, invoiced: 10m);
        var invoice = IssuedInvoiceFor(order, line, 4m);
        _invoices.GetWithLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        _orders.GetWithLinesAsync(OrderId, Arg.Any<CancellationToken>()).Returns(order);

        await _sut.Handle(new CancelInvoiceCommand(invoice.Id), default);

        line.QuantityInvoiced.Should().Be(6m);
        line.QuantityRemainingToInvoice.Should().Be(4m);
    }

    [Fact]
    public async Task A_standalone_invoice_touches_no_order()
    {
        var invoice = new Invoice("INV-2", CustomerId, "Acme", "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
        };
        _invoices.GetWithLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        await _sut.Handle(new CancelInvoiceCommand(invoice.Id), default);

        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
        await _orders.DidNotReceive().GetWithLinesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
