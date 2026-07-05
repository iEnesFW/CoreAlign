using CoreAlign.Application.Shipments.EDespatch;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Shipments;

public class IssueEDespatchCommandTests
{
    private readonly IShipmentRepository _shipments = Substitute.For<IShipmentRepository>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IEDespatchSubmissionOutbox _outbox = Substitute.For<IEDespatchSubmissionOutbox>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IssueEDespatchCommandHandler _sut;

    public IssueEDespatchCommandTests()
    {
        _tenant.RequireTenantId().Returns(Guid.NewGuid());
        _sut = new IssueEDespatchCommandHandler(_shipments, _orders, _outbox, _tenant);
    }

    private static Shipment Dispatched()
    {
        var s = new Shipment("SHP-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null) { Id = Guid.NewGuid() };
        s.AddLine(new ShipmentLine(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Widget", 5m, 10m));
        s.MarkPicked(null);
        s.MarkPacked();
        s.Dispatch("Aras", "TRK", null, 0m);
        return s;
    }

    [Fact]
    public async Task Issuing_sets_carrier_fields_queues_status_and_enqueues_submission()
    {
        var shipment = Dispatched();
        _shipments.GetWithLinesAsync(shipment.Id, Arg.Any<CancellationToken>()).Returns(shipment);

        await _sut.Handle(new IssueEDespatchCommand(shipment.Id, "1234567890", "34ABC123", "Ahmet", "12345678901"), default);

        shipment.CarrierVkn.Should().Be("1234567890");
        shipment.VehiclePlate.Should().Be("34ABC123");
        shipment.DriverTckn.Should().Be("12345678901");
        shipment.EDespatchProfile.Should().Be("TEMELIRSALIYE");
        shipment.EDespatchStatus.Should().Be("Queued");
        await _outbox.Received(1).EnqueueSubmissionAsync(Arg.Any<EDespatchSubmissionRequestedPayload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Issuing_on_non_dispatched_shipment_is_rejected()
    {
        var draft = new Shipment("SHP-2", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null) { Id = Guid.NewGuid() };
        _shipments.GetWithLinesAsync(draft.Id, Arg.Any<CancellationToken>()).Returns(draft);

        Func<Task> act = () => _sut.Handle(new IssueEDespatchCommand(draft.Id), default);

        await act.Should().ThrowAsync<InvalidShipmentStateException>();
        await _outbox.DidNotReceiveWithAnyArgs().EnqueueSubmissionAsync(default!, default);
    }

    [Fact]
    public async Task Issuing_when_already_issued_is_rejected()
    {
        var shipment = Dispatched();
        shipment.RegisterEDespatch("ETTN-1", "Submitted");
        _shipments.GetWithLinesAsync(shipment.Id, Arg.Any<CancellationToken>()).Returns(shipment);

        Func<Task> act = () => _sut.Handle(new IssueEDespatchCommand(shipment.Id), default);

        await act.Should().ThrowAsync<EDespatchAlreadyIssuedException>();
        await _outbox.DidNotReceiveWithAnyArgs().EnqueueSubmissionAsync(default!, default);
    }

    [Fact]
    public async Task Issuing_while_a_submission_is_already_queued_is_rejected()
    {
        var shipment = Dispatched();
        shipment.RegisterEDespatch(null, "Queued");
        _shipments.GetWithLinesAsync(shipment.Id, Arg.Any<CancellationToken>()).Returns(shipment);

        Func<Task> act = () => _sut.Handle(new IssueEDespatchCommand(shipment.Id), default);

        await act.Should().ThrowAsync<EDespatchAlreadyIssuedException>();
        await _outbox.DidNotReceiveWithAnyArgs().EnqueueSubmissionAsync(default!, default);
    }

    [Fact]
    public async Task Issuing_after_a_failed_attempt_is_allowed_for_recovery()
    {
        var shipment = Dispatched();
        shipment.RegisterEDespatch(null, "Failed");
        _shipments.GetWithLinesAsync(shipment.Id, Arg.Any<CancellationToken>()).Returns(shipment);

        await _sut.Handle(new IssueEDespatchCommand(shipment.Id, "1234567890", null, null, null), default);

        shipment.CarrierVkn.Should().Be("1234567890");
        await _outbox.Received(1).EnqueueSubmissionAsync(Arg.Any<EDespatchSubmissionRequestedPayload>(), Arg.Any<CancellationToken>());
    }
}
