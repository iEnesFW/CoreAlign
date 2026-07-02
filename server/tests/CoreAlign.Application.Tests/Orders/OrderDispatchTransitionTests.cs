using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Orders;

public class OrderDispatchTransitionTests
{
    [Fact]
    public void MarkFullyShipped_from_allocated_transitions_to_shipped_and_emits_event()
    {
        var order = AllocatedOrder();
        order.ClearDomainEvents();

        order.MarkFullyShipped(Guid.NewGuid(), "SHP-1", isPartial: false);

        order.Status.Should().Be(OrderStatus.Shipped);
        order.DomainEvents.OfType<OrderShippedEvent>().Should().ContainSingle();
    }

    [Fact]
    public void MarkFullyShipped_partial_from_allocated_transitions_to_partially_shipped()
    {
        var order = AllocatedOrder();

        order.MarkFullyShipped(Guid.NewGuid(), "SHP-1", isPartial: true);

        order.Status.Should().Be(OrderStatus.PartiallyShipped);
    }

    [Fact]
    public void ChangeStatus_allocated_to_shipped_is_allowed()
    {
        var order = AllocatedOrder();

        var act = () => order.ChangeStatus(OrderStatus.Shipped);

        act.Should().NotThrow();
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void MarkFullyShipped_on_delivered_order_does_not_revert_to_shipped()
    {
        var order = AllocatedOrder();
        order.ChangeStatus(OrderStatus.Shipped);
        order.ChangeStatus(OrderStatus.Delivered);
        order.ClearDomainEvents();

        order.MarkFullyShipped(Guid.NewGuid(), "SHP-2", isPartial: false);

        order.Status.Should().Be(OrderStatus.Delivered);
        order.DomainEvents.OfType<OrderShippedEvent>().Should().BeEmpty();
    }

    [Fact]
    public void ChangeStatus_delivered_to_shipped_is_rejected()
    {
        var order = AllocatedOrder();
        order.ChangeStatus(OrderStatus.Shipped);
        order.ChangeStatus(OrderStatus.Delivered);

        var act = () => order.ChangeStatus(OrderStatus.Shipped);

        act.Should().Throw<InvalidOrderStatusTransitionException>();
    }

    private static Order AllocatedOrder()
    {
        var order = new Order("ORD-1", Guid.NewGuid(), DateTime.UtcNow, "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
        };
        order.Lines.Add(new OrderLine(Guid.NewGuid(), "SKU", "Item", 2m, 50m));
        order.Submit();
        order.Approve(Guid.NewGuid());
        order.MarkAllocated(null);
        return order;
    }
}
