using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Orders.Commands;
using CoreAlign.Application.Orders.Handlers;
using CoreAlign.Application.Shipments.Commands;
using CoreAlign.Application.Shipments.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Orders;

// WHY these two guards belong together: cancelling an order released its reservations while the
// already-packed shipment stayed dispatchable. Dispatching it then consumed nothing, posted no
// COGS and left MarkFullyShipped a silent no-op — the goods left with inventory untouched.
public class DispatchAfterCancelTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IShipmentRepository _shipments = Substitute.For<IShipmentRepository>();
    private readonly IAllocationService _allocator = Substitute.For<IAllocationService>();
    private readonly IGLPostingOutbox _glOutbox = Substitute.For<IGLPostingOutbox>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Cancelling_an_order_with_a_packed_shipment_is_refused()
    {
        var (order, shipment) = BuildAllocatedOrderWithPackedShipment();
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _shipments.GetByOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(new[] { shipment });

        var sut = new CancelOrderHandler(_orders, _shipments, _allocator, _uow);
        var act = async () => await sut.Handle(new CancelOrderCommand(order.Id, "customer changed their mind"), default);

        await act.Should().ThrowAsync<OrderCancelBlockedException>();
        await _allocator.DidNotReceive().ReleaseByOrderAsync(order.Id, Arg.Any<CancellationToken>());
        order.Status.Should().Be(OrderStatus.Allocated);
    }

    [Fact]
    public async Task Cancelling_an_order_whose_only_shipment_is_cancelled_still_works()
    {
        var (order, shipment) = BuildAllocatedOrderWithPackedShipment();
        shipment.Cancel("picked the wrong pallet");
        _orders.GetWithLinesAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _shipments.GetByOrderAsync(order.Id, Arg.Any<CancellationToken>()).Returns(new[] { shipment });

        var sut = new CancelOrderHandler(_orders, _shipments, _allocator, _uow);
        await sut.Handle(new CancelOrderCommand(order.Id, "customer changed their mind"), default);

        order.Status.Should().Be(OrderStatus.Cancelled);
        await _allocator.Received(1).ReleaseByOrderAsync(order.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Dispatching_a_shipment_on_a_cancelled_order_is_refused()
    {
        var (order, shipment) = BuildAllocatedOrderWithPackedShipment();
        order.Cancel("cancelled behind the warehouse's back");
        _shipments.GetWithLinesAsync(shipment.Id, Arg.Any<CancellationToken>()).Returns(shipment);
        _orders.GetWithLinesAndShipmentsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var sut = new DispatchShipmentHandler(_shipments, _orders, _allocator, _glOutbox, _uow);
        var act = async () => await sut.Handle(new DispatchShipmentCommand(shipment.Id, "DHL", null, null, null), default);

        await act.Should().ThrowAsync<ShipmentOrderNotDispatchableException>();
        shipment.Status.Should().Be(ShipmentStatus.Packed);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _glOutbox.DidNotReceive().EnqueueAsync(Arg.Any<GLPostingRequest>(), Arg.Any<CancellationToken>());
    }

    private static (Order Order, Shipment Shipment) BuildAllocatedOrderWithPackedShipment()
    {
        var order = new Order("ORD-1", CustomerId, DateTime.UtcNow, "TRY", null)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        var line = new OrderLine(ProductId, "SKU-A", "Widget", 10m, 100m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        order.ReplaceLines(new[] { line });
        order.ChangeStatus(OrderStatus.Submitted);
        order.ChangeStatus(OrderStatus.Approved);
        order.ChangeStatus(OrderStatus.Allocated);

        var shipment = new Shipment("SHP-1", order.Id, CustomerId, Guid.NewGuid(), shippingAddressSnapshot: null)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        shipment.AddLine(new ShipmentLine(line.Id, ProductId, "SKU-A", "Widget", 10m, 7m));
        shipment.MarkPicked(null);
        shipment.MarkPacked();
        return (order, shipment);
    }
}
