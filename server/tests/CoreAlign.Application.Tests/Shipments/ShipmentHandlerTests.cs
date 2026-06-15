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
}
