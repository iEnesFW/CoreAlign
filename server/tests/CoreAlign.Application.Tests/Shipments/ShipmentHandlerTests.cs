using CoreAlign.Application.Shipments.Commands;
using CoreAlign.Application.Shipments.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Shipments;

public class ShipmentHandlerTests
{
    private readonly IShipmentRepository _shipments = Substitute.For<IShipmentRepository>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IAllocationService _allocator = Substitute.For<IAllocationService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private static Shipment Draft(Guid? id = null)
    {
        var s = new Shipment("SHP-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), shippingAddressSnapshot: null)
        {
            Id = id ?? Guid.NewGuid(),
        };
        return s;
    }

    [Fact]
    public async Task PickShipment_throws_when_shipment_not_found()
    {
        _shipments.GetWithLinesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Shipment?)null);
        var sut = new PickShipmentHandler(_shipments, _uow);

        Func<Task> act = () => sut.Handle(new PickShipmentCommand(Guid.NewGuid()), default);
        await act.Should().ThrowAsync<ShipmentNotFoundException>();
    }

    [Fact]
    public async Task PickShipment_transitions_status_and_persists()
    {
        var s = Draft();
        s.AddLine(new ShipmentLine(Guid.NewGuid(), Guid.NewGuid(), "SKU", "x", 1m, 1m));
        _shipments.GetWithLinesAsync(s.Id, Arg.Any<CancellationToken>()).Returns(s);
        var sut = new PickShipmentHandler(_shipments, _uow);

        var dto = await sut.Handle(new PickShipmentCommand(s.Id, Guid.NewGuid()), default);

        dto.Status.Should().Be(ShipmentStatus.Picked);
        _shipments.Received(1).Update(s);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PackShipment_throws_when_not_in_picked_state()
    {
        var s = Draft();
        s.AddLine(new ShipmentLine(Guid.NewGuid(), Guid.NewGuid(), "SKU", "x", 1m, 1m));
        _shipments.GetWithLinesAsync(s.Id, Arg.Any<CancellationToken>()).Returns(s);
        var sut = new PackShipmentHandler(_shipments, _uow);

        Func<Task> act = () => sut.Handle(new PackShipmentCommand(s.Id), default);
        await act.Should().ThrowAsync<InvalidShipmentStateException>();
    }

    [Fact]
    public async Task CancelShipment_succeeds_from_draft()
    {
        var s = Draft();
        _shipments.GetWithLinesAsync(s.Id, Arg.Any<CancellationToken>()).Returns(s);
        var sut = new CancelShipmentHandler(_shipments, _uow);

        var dto = await sut.Handle(new CancelShipmentCommand(s.Id, "test"), default);

        dto.Status.Should().Be(ShipmentStatus.Cancelled);
        _shipments.Received(1).Update(s);
    }

    [Fact]
    public async Task CancelShipment_throws_when_already_delivered_via_state_machine()
    {
        var s = Draft();
        s.AddLine(new ShipmentLine(Guid.NewGuid(), Guid.NewGuid(), "SKU", "x", 1m, 1m));
        s.MarkPicked(Guid.NewGuid());
        s.MarkPacked();
        s.Dispatch(null, null, null, null);
        s.MarkDelivered(null, null);
        _shipments.GetWithLinesAsync(s.Id, Arg.Any<CancellationToken>()).Returns(s);

        var sut = new CancelShipmentHandler(_shipments, _uow);
        Func<Task> act = () => sut.Handle(new CancelShipmentCommand(s.Id, null), default);
        await act.Should().ThrowAsync<InvalidShipmentStateException>();
    }

    [Fact]
    public async Task DeliverShipment_throws_when_not_dispatched()
    {
        var s = Draft();
        s.AddLine(new ShipmentLine(Guid.NewGuid(), Guid.NewGuid(), "SKU", "x", 1m, 1m));
        _shipments.GetWithLinesAsync(s.Id, Arg.Any<CancellationToken>()).Returns(s);
        var sut = new DeliverShipmentHandler(_shipments, _orders, _uow);

        Func<Task> act = () => sut.Handle(new DeliverShipmentCommand(s.Id, null, null), default);
        await act.Should().ThrowAsync<InvalidShipmentStateException>();
    }

    [Fact]
    public async Task An_undispatched_shipment_still_reserves_the_quantity_it_claimed()
    {
        var order = new Order("ORD-1", Guid.NewGuid(), DateTime.UtcNow, "TRY") { Id = Guid.NewGuid() };
        var line = new OrderLine(Guid.NewGuid(), "SKU-A", "Widget", 10m, 100m);
        order.ReplaceLines(new[] { line });
        order.ChangeStatus(OrderStatus.Confirmed);

        // Already packed but NOT dispatched, so QuantityShipped is still 0.
        var open = new Shipment("SHP-1", order.Id, order.CustomerId, Guid.NewGuid(), shippingAddressSnapshot: null)
        {
            Id = Guid.NewGuid(),
        };
        open.AddLine(new ShipmentLine(line.Id, line.ProductId, "SKU-A", "Widget", 10m, 1m));
        open.MarkPicked(null);
        open.MarkPacked();
        order.Shipments.Add(open);

        _orders.GetWithLinesAndShipmentsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        var sut = new CreateShipmentHandler(_orders, _shipments, _sequences, _uow);

        Func<Task> act = () => sut.Handle(
            new CreateShipmentCommand(order.Id, Guid.NewGuid(), new List<ShipmentLineInput>
            {
                new(line.Id, 10m),
            }),
            default);

        await act.Should().ThrowAsync<ShipmentLineQuantityExceededException>();
        await _shipments.DidNotReceive().AddAsync(Arg.Any<Shipment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_cancelled_shipment_releases_the_quantity_it_had_claimed()
    {
        var order = new Order("ORD-2", Guid.NewGuid(), DateTime.UtcNow, "TRY") { Id = Guid.NewGuid() };
        var line = new OrderLine(Guid.NewGuid(), "SKU-A", "Widget", 10m, 100m);
        order.ReplaceLines(new[] { line });
        order.ChangeStatus(OrderStatus.Confirmed);

        var cancelled = new Shipment("SHP-2", order.Id, order.CustomerId, Guid.NewGuid(), shippingAddressSnapshot: null)
        {
            Id = Guid.NewGuid(),
        };
        cancelled.AddLine(new ShipmentLine(line.Id, line.ProductId, "SKU-A", "Widget", 10m, 1m));
        cancelled.Cancel("test");
        order.Shipments.Add(cancelled);

        _orders.GetWithLinesAndShipmentsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _sequences.ConsumeAsync(DocumentSequenceType.ShipmentNumber, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("SHP-3");
        var sut = new CreateShipmentHandler(_orders, _shipments, _sequences, _uow);

        var dto = await sut.Handle(
            new CreateShipmentCommand(order.Id, Guid.NewGuid(), new List<ShipmentLineInput>
            {
                new(line.Id, 10m),
            }),
            default);

        dto.Should().NotBeNull();
        await _shipments.Received(1).AddAsync(Arg.Any<Shipment>(), Arg.Any<CancellationToken>());
    }
}
