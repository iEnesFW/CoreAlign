using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.BugHuntC;

/// <summary>
/// HUNTER C — C-3 (MEDIUM): Order.MarkFullyShipped() sets Status directly and never calls
/// EnsureTransitionAllowed. DispatchShipmentHandler invokes it without re-checking order
/// status. With a partially-shipped order whose first shipment was already Delivered, a
/// lingering second shipment dispatched afterwards reverts the terminal Delivered order
/// back to Shipped/PartiallyShipped — an illegal backward transition the FSM forbids — and
/// re-emits OrderShippedEvent. EnsureTransitionAllowed(Delivered → Shipped) is NOT allowed.
/// </summary>
public class OrderMarkFullyShippedBypassesFsmTests
{
    [Fact]
    public void MarkFullyShipped_OnDeliveredOrder_IllegallyRevertsToShipped()
    {
        var order = BuildDeliveredOrder();
        order.Status.Should().Be(OrderStatus.Delivered, "precondition: order reached a terminal-ish delivered state");
        order.ClearDomainEvents();

        // A stray second shipment is dispatched after delivery. Handler calls this directly,
        // bypassing ChangeStatus/EnsureTransitionAllowed.
        order.MarkFullyShipped(Guid.NewGuid(), "SH-2", isPartial: false);

        // Delivered → Shipped is an illegal backward transition. The FSM (EnsureTransitionAllowed)
        // would reject it, but MarkFullyShipped mutates Status with no guard. This assertion
        // documents the CORRECT expectation and therefore FAILS on current code.
        order.Status.Should().Be(OrderStatus.Delivered,
            "a delivered order must not silently revert to Shipped via an unguarded MarkFullyShipped");
    }

    private static Order BuildDeliveredOrder()
    {
        var order = new Order("ORD-1", Guid.NewGuid(), DateTime.UtcNow, "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
        };
        order.Lines.Add(new OrderLine(Guid.NewGuid(), "SKU", "Item", 2m, 50m));
        // Walk the legitimate FSM path to Delivered.
        order.ChangeStatus(OrderStatus.Confirmed);
        order.ChangeStatus(OrderStatus.Shipped);
        order.ChangeStatus(OrderStatus.Delivered);
        return order;
    }
}
