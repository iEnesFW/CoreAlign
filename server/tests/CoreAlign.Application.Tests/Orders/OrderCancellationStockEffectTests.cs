using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Orders;

public class OrderCancellationStockEffectTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    [Fact]
    public void Cancelling_allocated_order_does_not_raise_restore_event()
    {
        var order = BuildOrder();
        order.Submit();
        order.Approve(Guid.NewGuid());
        order.MarkAllocated(null);
        order.ClearDomainEvents();

        order.Cancel("test");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DomainEvents.OfType<OrderCancelledFromActiveEvent>().Should().BeEmpty();
    }

    [Fact]
    public void Cancelling_approved_order_does_not_raise_restore_event()
    {
        var order = BuildOrder();
        order.Submit();
        order.Approve(Guid.NewGuid());
        order.ClearDomainEvents();

        order.Cancel("test");

        order.DomainEvents.OfType<OrderCancelledFromActiveEvent>().Should().BeEmpty();
    }

    [Fact]
    public void Cancelling_confirmed_order_raises_restore_event()
    {
        var order = BuildOrder();
        order.ChangeStatus(OrderStatus.Confirmed);
        order.ClearDomainEvents();

        var effect = order.ChangeStatus(OrderStatus.Cancelled);

        effect.Should().Be(OrderStockEffect.Restore);
        order.DomainEvents.OfType<OrderCancelledFromActiveEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Confirmed_order_is_cancellable()
    {
        var order = BuildOrder();
        order.ChangeStatus(OrderStatus.Confirmed);

        order.IsCancellable.Should().BeTrue();
    }

    [Fact]
    public void Cancelling_confirmed_order_via_cancel_restores_stock()
    {
        var order = BuildOrder();
        order.ChangeStatus(OrderStatus.Confirmed);
        order.ClearDomainEvents();

        order.Cancel("customer changed mind");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DomainEvents.OfType<OrderCancelledFromActiveEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Cancelling_confirmed_order_excludes_service_lines_from_restore_snapshot()
    {
        var order = new Order("ORD-1", CustomerId, DateTime.UtcNow, "USD") { Id = Guid.NewGuid(), TenantId = TenantId };
        var stockLine = new OrderLine(ProductId, "SKU-A", "Widget", 10m, 5m) { Id = Guid.NewGuid(), TenantId = TenantId };
        var serviceLine = new OrderLine(Guid.Empty, "SERVICE", "Installation labor", 1m, 100m, isService: true)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId
        };
        order.ReplaceLines(new[] { stockLine, serviceLine });
        order.ChangeStatus(OrderStatus.Confirmed);
        order.ClearDomainEvents();

        order.Cancel("test");

        var restore = order.DomainEvents.OfType<OrderCancelledFromActiveEvent>().Single();
        restore.Lines.Should().ContainSingle();
        restore.Lines.Single().ProductId.Should().Be(ProductId);
    }

    [Fact]
    public void ChangeStatus_allocated_to_cancelled_yields_no_stock_effect()
    {
        var order = BuildOrder();
        order.Submit();
        order.Approve(Guid.NewGuid());
        order.MarkAllocated(null);
        order.ClearDomainEvents();

        var effect = order.ChangeStatus(OrderStatus.Cancelled);

        effect.Should().Be(OrderStockEffect.None);
        order.DomainEvents.OfType<OrderCancelledFromActiveEvent>().Should().BeEmpty();
    }

    [Fact]
    public void Allocated_order_cannot_jump_to_confirmed()
    {
        var order = BuildOrder();
        order.Submit();
        order.Approve(Guid.NewGuid());
        order.MarkAllocated(null);

        var act = () => order.ChangeStatus(OrderStatus.Confirmed);

        act.Should().Throw<InvalidOrderStatusTransitionException>();
    }

    [Fact]
    public void Confirmed_order_cannot_jump_to_allocated()
    {
        var order = BuildOrder();
        order.ChangeStatus(OrderStatus.Confirmed);

        var act = () => order.ChangeStatus(OrderStatus.Allocated);

        act.Should().Throw<InvalidOrderStatusTransitionException>();
    }

    private static Order BuildOrder()
    {
        var order = new Order("ORD-1", CustomerId, DateTime.UtcNow, "USD")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId
        };
        var line = new OrderLine(ProductId, "SKU-A", "Widget", 10m, 5m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId
        };
        order.ReplaceLines(new[] { line });
        return order;
    }
}
