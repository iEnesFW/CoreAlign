using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Shipments;

public class ShipmentDomainTests
{
    private static Shipment NewDraft() => new(
        "SHP-1",
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        shippingAddressSnapshot: null);

    private static ShipmentLine NewLine() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Widget", quantity: 3m, unitCostSnapshot: 12m);

    [Fact]
    public void Constructor_initialises_status_as_draft()
    {
        var s = NewDraft();
        s.Status.Should().Be(ShipmentStatus.Draft);
        s.PickedAtUtc.Should().BeNull();
        s.PackedAtUtc.Should().BeNull();
        s.DispatchedAtUtc.Should().BeNull();
        s.DeliveredAtUtc.Should().BeNull();
        s.CancelledAtUtc.Should().BeNull();
    }

    [Fact]
    public void AddLine_only_allowed_in_draft_state()
    {
        var s = NewDraft();
        s.AddLine(NewLine());
        s.MarkPicked(Guid.NewGuid());

        var act = () => s.AddLine(NewLine());
        act.Should().Throw<InvalidShipmentStateException>();
    }

    [Fact]
    public void Happy_path_transitions_draft_to_delivered()
    {
        var s = NewDraft();
        s.AddLine(NewLine());

        s.MarkPicked(Guid.NewGuid());
        s.Status.Should().Be(ShipmentStatus.Picked);
        s.PickedAtUtc.Should().NotBeNull();

        s.MarkPacked();
        s.Status.Should().Be(ShipmentStatus.Packed);
        s.PackedAtUtc.Should().NotBeNull();

        s.Dispatch("UPS", "TRK-1", "https://x", 25m);
        s.Status.Should().Be(ShipmentStatus.Dispatched);
        s.CarrierName.Should().Be("UPS");
        s.TrackingNumber.Should().Be("TRK-1");
        s.ShippingCost.Should().Be(25m);

        s.MarkDelivered("Door", null);
        s.Status.Should().Be(ShipmentStatus.Delivered);
        s.DeliveredAtUtc.Should().NotBeNull();
        s.ReceivedBy.Should().Be("Door");
    }

    [Fact]
    public void MarkPacked_rejected_when_still_in_draft()
    {
        var s = NewDraft();
        s.AddLine(NewLine());

        var act = () => s.MarkPacked();
        act.Should().Throw<InvalidShipmentStateException>();
    }

    [Fact]
    public void Dispatch_rejected_when_not_packed()
    {
        var s = NewDraft();
        s.AddLine(NewLine());
        s.MarkPicked(Guid.NewGuid());

        var act = () => s.Dispatch(null, null, null, null);
        act.Should().Throw<InvalidShipmentStateException>();
    }

    [Fact]
    public void MarkDelivered_rejected_before_dispatch()
    {
        var s = NewDraft();
        s.AddLine(NewLine());
        s.MarkPicked(Guid.NewGuid());
        s.MarkPacked();

        var act = () => s.MarkDelivered(null, null);
        act.Should().Throw<InvalidShipmentStateException>();
    }

    [Fact]
    public void Cancel_from_draft_picked_or_packed_is_allowed()
    {
        var draft = NewDraft();
        draft.Cancel("change of mind");
        draft.Status.Should().Be(ShipmentStatus.Cancelled);
        draft.CancelledAtUtc.Should().NotBeNull();
        draft.CancelReason.Should().Be("change of mind");

        var picked = NewDraft();
        picked.AddLine(NewLine());
        picked.MarkPicked(Guid.NewGuid());
        picked.Cancel(null);
        picked.Status.Should().Be(ShipmentStatus.Cancelled);

        var packed = NewDraft();
        packed.AddLine(NewLine());
        packed.MarkPicked(Guid.NewGuid());
        packed.MarkPacked();
        packed.Cancel(null);
        packed.Status.Should().Be(ShipmentStatus.Cancelled);
    }

    [Fact]
    public void Cancel_rejected_when_already_delivered()
    {
        var s = NewDraft();
        s.AddLine(NewLine());
        s.MarkPicked(Guid.NewGuid());
        s.MarkPacked();
        s.Dispatch(null, null, null, null);
        s.MarkDelivered(null, null);

        var act = () => s.Cancel("oops");
        act.Should().Throw<InvalidShipmentStateException>();
    }

    [Fact]
    public void MarkDelivered_uses_provided_timestamp_when_supplied()
    {
        var s = NewDraft();
        s.AddLine(NewLine());
        s.MarkPicked(Guid.NewGuid());
        s.MarkPacked();
        s.Dispatch(null, null, null, null);
        var when = new DateTime(2026, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        s.MarkDelivered("Recipient", when);

        s.DeliveredAtUtc.Should().Be(when);
    }

    [Fact]
    public void UpdateMeta_replaces_notes_and_optionally_address()
    {
        var s = NewDraft();
        s.UpdateMeta("note v1", null);
        s.Notes.Should().Be("note v1");

        var addr = new AddressSnapshot { Line1 = "Line", City = "İstanbul", Country = "TR" };
        s.UpdateMeta("note v2", addr);
        s.Notes.Should().Be("note v2");
        s.ShippingAddressSnapshot.Should().NotBeNull();
    }
}
