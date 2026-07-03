using CoreAlign.Application.Orders.Commands;
using CoreAlign.Application.Orders.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Orders;

public class OrderRevertToDraftDomainTests
{
    [Fact]
    public void Reverting_submitted_order_returns_to_draft_and_clears_submission_stamp()
    {
        var order = BuildOrder();
        order.Submit();
        order.ClearDomainEvents();

        order.RevertToDraft();

        order.Status.Should().Be(OrderStatus.Draft);
        order.SubmittedAtUtc.Should().BeNull();
        order.DomainEvents.OfType<OrderStatusChangedEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Reverting_approved_order_returns_to_draft_and_clears_approval_stamp()
    {
        var order = BuildOrder();
        order.Submit();
        order.Approve(Guid.NewGuid());

        order.RevertToDraft();

        order.Status.Should().Be(OrderStatus.Draft);
        order.ApprovedByUserId.Should().BeNull();
        order.ApprovedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Reverting_allocated_order_returns_to_draft()
    {
        var order = BuildOrder();
        order.Submit();
        order.Approve(Guid.NewGuid());
        order.MarkAllocated(null);

        order.RevertToDraft();

        order.Status.Should().Be(OrderStatus.Draft);
    }

    [Fact]
    public void Reverting_shipped_order_is_rejected()
    {
        var order = BuildOrder();
        order.Submit();
        order.Approve(Guid.NewGuid());
        order.MarkAllocated(null);
        order.ChangeStatus(OrderStatus.Shipped);

        var act = () => order.RevertToDraft();

        act.Should().Throw<InvalidOrderStatusTransitionException>();
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void Reverting_confirmed_order_is_rejected()
    {
        var order = BuildOrder();
        order.ChangeStatus(OrderStatus.Confirmed);

        var act = () => order.RevertToDraft();

        act.Should().Throw<InvalidOrderStatusTransitionException>();
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public void Reverting_cancelled_order_is_rejected()
    {
        var order = BuildOrder();
        order.Cancel(null);

        var act = () => order.RevertToDraft();

        act.Should().Throw<InvalidOrderStatusTransitionException>();
    }

    private static Order BuildOrder()
    {
        var order = new Order("ORD-1", Guid.NewGuid(), DateTime.UtcNow, "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
        };
        order.Lines.Add(new OrderLine(Guid.NewGuid(), "SKU", "Item", 2m, 50m));
        return order;
    }
}

public class RevertOrderToDraftCommandHandlerTests
{
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IShipmentRepository _shipments = Substitute.For<IShipmentRepository>();
    private readonly IAllocationService _allocationService = Substitute.For<IAllocationService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly RevertOrderToDraftCommandHandler _sut;

    public RevertOrderToDraftCommandHandlerTests()
    {
        _shipments.GetByOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Shipment>());
        _sut = new RevertOrderToDraftCommandHandler(_orders, _invoices, _shipments, _allocationService, _uow);
    }

    [Fact]
    public async Task Reverting_approved_order_without_documents_returns_draft_dto()
    {
        var order = SubmittedOrder();
        order.Approve(Guid.NewGuid());
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var dto = await _sut.Handle(new RevertOrderToDraftCommand(order.Id), CancellationToken.None);

        dto.Status.Should().Be(OrderStatus.Draft);
        await _allocationService.DidNotReceive().ReleaseByOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reverting_allocated_order_releases_reservations_first()
    {
        var order = SubmittedOrder();
        order.Approve(Guid.NewGuid());
        order.MarkAllocated(null);
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var dto = await _sut.Handle(new RevertOrderToDraftCommand(order.Id), CancellationToken.None);

        dto.Status.Should().Be(OrderStatus.Draft);
        await _allocationService.Received(1).ReleaseByOrderAsync(order.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Active_invoice_blocks_revert()
    {
        var order = SubmittedOrder();
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _invoices.GetByOrderIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(new Invoice("INV-9", order.CustomerId, "Customer", "TRY"));

        var act = () => _sut.Handle(new RevertOrderToDraftCommand(order.Id), CancellationToken.None);

        await act.Should().ThrowAsync<OrderRevertBlockedException>();
        order.Status.Should().Be(OrderStatus.Submitted);
    }

    [Fact]
    public async Task Cancelled_invoice_does_not_block_revert()
    {
        var order = SubmittedOrder();
        var invoice = new Invoice("INV-9", order.CustomerId, "Customer", "TRY");
        invoice.Cancel(DateTime.UtcNow);
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _invoices.GetByOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var dto = await _sut.Handle(new RevertOrderToDraftCommand(order.Id), CancellationToken.None);

        dto.Status.Should().Be(OrderStatus.Draft);
    }

    [Fact]
    public async Task Active_shipment_blocks_revert()
    {
        var order = SubmittedOrder();
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _shipments.GetByOrderAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Shipment>
            {
                new("SHP-9", order.Id, order.CustomerId, Guid.NewGuid(), shippingAddressSnapshot: null),
            });

        var act = () => _sut.Handle(new RevertOrderToDraftCommand(order.Id), CancellationToken.None);

        await act.Should().ThrowAsync<OrderRevertBlockedException>();
    }

    [Fact]
    public async Task Cancelled_shipment_does_not_block_revert()
    {
        var order = SubmittedOrder();
        var shipment = new Shipment("SHP-9", order.Id, order.CustomerId, Guid.NewGuid(), shippingAddressSnapshot: null);
        shipment.Cancel(null);
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _shipments.GetByOrderAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Shipment> { shipment });

        var dto = await _sut.Handle(new RevertOrderToDraftCommand(order.Id), CancellationToken.None);

        dto.Status.Should().Be(OrderStatus.Draft);
    }

    [Fact]
    public async Task Missing_order_throws_not_found()
    {
        _orders.GetWithLinesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Order?)null);

        var act = () => _sut.Handle(new RevertOrderToDraftCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<OrderNotFoundException>();
    }

    private static Order SubmittedOrder()
    {
        var order = new Order("ORD-1", Guid.NewGuid(), DateTime.UtcNow, "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
        };
        order.Lines.Add(new OrderLine(Guid.NewGuid(), "SKU", "Item", 2m, 50m));
        order.Submit();
        return order;
    }
}
