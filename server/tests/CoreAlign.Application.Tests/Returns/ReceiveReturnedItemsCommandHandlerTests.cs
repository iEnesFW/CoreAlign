using CoreAlign.Application.B2B;
using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Application.Returns.Commands;
using CoreAlign.Application.Returns.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Tests.Returns;

public class ReceiveReturnedItemsCommandHandlerTests
{
    private readonly IReturnRequestRepository _returns = Substitute.For<IReturnRequestRepository>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IWarehouseRepository _warehouses = Substitute.For<IWarehouseRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ReceiveReturnedItemsCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();
    private static readonly Guid InvoiceId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public ReceiveReturnedItemsCommandHandlerTests()
    {
        _tenantContext.RequireTenantId().Returns(TenantId);
        _currentUser.UserIdOrThrow().Returns(UserId);
        _currentUser.UserId.Returns(UserId);
        _sut = new ReceiveReturnedItemsCommandHandler(
            _returns, _orders, _warehouses, _invoices, _mediator, _tenantContext, _currentUser, _uow);
    }

    [Fact]
    public async Task Receive_transitions_to_received_and_records_return_on_order_lines()
    {
        var (entity, order) = BuildApprovedReturnAndOrder();
        _returns.GetWithLinesAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);
        _orders.GetWithLinesAsync(OrderId, Arg.Any<CancellationToken>()).Returns(order);
        _warehouses.GetByIdAsync(WarehouseId, Arg.Any<CancellationToken>())
            .Returns(new Warehouse("WH-1", "Main") { Id = WarehouseId, TenantId = TenantId });

        var result = await _sut.Handle(
            new ReceiveReturnedItemsCommand(entity.Id, WarehouseId, AutoIssueCreditNote: false),
            default);

        result.Status.Should().Be(ReturnRequestStatus.Received);
        order.Lines.First().QuantityReturned.Should().Be(2m);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        var receivedEntity = await _returns.GetWithLinesAsync(entity.Id, default);
        receivedEntity!.ReceivedByUserId.Should().Be(UserId);
    }

    [Fact]
    public async Task Receive_with_auto_credit_note_dispatches_command_and_attaches_credit_note()
    {
        var (entity, order) = BuildApprovedReturnAndOrder(withSourceInvoice: true);
        var sourceInvoice = BuildSourceInvoice(order);
        _returns.GetWithLinesAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);
        _orders.GetWithLinesAsync(OrderId, Arg.Any<CancellationToken>()).Returns(order);
        _warehouses.GetByIdAsync(WarehouseId, Arg.Any<CancellationToken>())
            .Returns(new Warehouse("WH-1", "Main") { Id = WarehouseId, TenantId = TenantId });
        _invoices.GetWithLinesAsync(InvoiceId, Arg.Any<CancellationToken>()).Returns(sourceInvoice);

        var creditNoteId = Guid.NewGuid();
        _mediator.Send(Arg.Any<IssueCreditNoteCommand>(), Arg.Any<CancellationToken>())
            .Returns(new InvoiceDto { Id = creditNoteId, InvoiceNumber = "CN-1", Type = InvoiceType.CreditNote });

        var result = await _sut.Handle(
            new ReceiveReturnedItemsCommand(entity.Id, WarehouseId, AutoIssueCreditNote: true),
            default);

        result.Status.Should().Be(ReturnRequestStatus.CreditNoted);
        entity.CreditNoteId.Should().Be(creditNoteId);
        await _mediator.Received(1).Send(
            Arg.Is<IssueCreditNoteCommand>(c => c.InvoiceId == InvoiceId && c.ReturnRequestId == entity.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Receive_throws_when_warehouse_not_found()
    {
        var (entity, order) = BuildApprovedReturnAndOrder();
        _returns.GetWithLinesAsync(entity.Id, Arg.Any<CancellationToken>()).Returns(entity);
        _orders.GetWithLinesAsync(OrderId, Arg.Any<CancellationToken>()).Returns(order);
        _warehouses.GetByIdAsync(WarehouseId, Arg.Any<CancellationToken>()).Returns((Warehouse?)null);

        Func<Task> act = () => _sut.Handle(
            new ReceiveReturnedItemsCommand(entity.Id, WarehouseId, AutoIssueCreditNote: false), default);

        await act.Should().ThrowAsync<InvalidReturnRequestStateException>();
    }

    [Fact]
    public async Task Receive_throws_when_return_not_found()
    {
        _returns.GetWithLinesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ReturnRequest?)null);

        Func<Task> act = () => _sut.Handle(
            new ReceiveReturnedItemsCommand(Guid.NewGuid(), WarehouseId, AutoIssueCreditNote: false), default);

        await act.Should().ThrowAsync<ReturnRequestNotFoundException>();
    }

    private static (ReturnRequest Entity, Order Order) BuildApprovedReturnAndOrder(bool withSourceInvoice = false)
    {
        var customer = new Customer("Acme") { Id = CustomerId, TenantId = TenantId };
        var order = new Order("ORD-1", CustomerId, DateTime.UtcNow, "TRY")
        {
            Id = OrderId,
            TenantId = TenantId,
            Customer = customer,
        };
        var orderLine = new OrderLine(ProductId, "SKU-1", "Widget", 4m, 25m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        order.ReplaceLines(new[] { orderLine });
        order.ChangeStatus(OrderStatus.Confirmed);
        orderLine.RecordShipment(4m);
        order.ChangeStatus(OrderStatus.Shipped);

        var entity = new ReturnRequest(
            "RMA-1", order, ReturnReasonCode.Defective, "broken",
            requestedByUserId: null,
            sourceInvoiceId: withSourceInvoice ? InvoiceId : null,
            customerNotes: null)
        {
            Id = Guid.NewGuid(),
        };
        var rline = new ReturnRequestLine(orderLine, 2m, true, null)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        entity.ReplaceLines(new[] { rline });
        entity.Approve(Guid.NewGuid());
        return (entity, order);
    }

    private static Invoice BuildSourceInvoice(Order order)
    {
        var invoice = new Invoice("INV-1", order.CustomerId, "Acme", "TRY")
        {
            Id = InvoiceId,
            TenantId = TenantId,
            Customer = order.Customer,
        };
        var orderLine = order.Lines.First();
        var invLine = new InvoiceLine(orderLine.ProductId, orderLine.ProductSku, orderLine.ProductName, orderLine.Quantity, orderLine.UnitPrice)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        invLine.ApplyPricing(
            orderLine.Quantity, orderLine.UnitPrice, 0m, 0m, 0m, null, false, 0m,
            null, null, null, null, null, null, originOrderLineId: orderLine.Id);
        invoice.Lines.Add(invLine);
        invoice.Issue("INV-1");
        return invoice;
    }
}
