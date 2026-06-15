using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Mrp;

public class PlannedProductionOrderTests
{
    private static PlannedProductionOrder NewOrder(decimal qty = 20m) => new(
        sourcePlanRunId: Guid.NewGuid(),
        productId: Guid.NewGuid(),
        lowLevelCode: 1,
        quantity: qty,
        dueDateUtc: new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
        releaseDateUtc: new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc),
        estimatedUnitCost: 4m,
        sourcePolicy: LotSizingPolicy.LotForLot,
        peggingParentProductId: Guid.NewGuid(),
        peggingSourceOrderLineId: Guid.NewGuid());

    [Fact]
    public void Ctor_normalizes_dates_to_utc_and_starts_planned()
    {
        var unspecified = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Unspecified);
        var order = new PlannedProductionOrder(
            Guid.NewGuid(), Guid.NewGuid(), 0, 10m, unspecified, unspecified, 1m,
            LotSizingPolicy.LotForLot, null, null);

        order.Status.Should().Be(PlannedProductionOrderStatus.Planned);
        order.DueDateUtc.Kind.Should().Be(DateTimeKind.Utc);
        order.ReleaseDateUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Ctor_rejects_non_positive_quantity()
    {
        var act = () => new PlannedProductionOrder(
            Guid.NewGuid(), Guid.NewGuid(), 0, 0m,
            DateTime.UtcNow, DateTime.UtcNow, 1m, LotSizingPolicy.LotForLot, null, null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Firm_from_planned_transitions_to_firm()
    {
        var order = NewOrder();

        order.Firm(null, null);

        order.Status.Should().Be(PlannedProductionOrderStatus.Firm);
    }

    [Fact]
    public void Firm_applies_quantity_and_due_date_overrides()
    {
        var order = NewOrder();
        var leadOffset = order.DueDateUtc - order.ReleaseDateUtc;
        var newDue = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        order.Firm(35m, newDue);

        order.Quantity.Should().Be(35m);
        order.DueDateUtc.Should().Be(newDue);
        order.ReleaseDateUtc.Should().Be(newDue - leadOffset);
    }

    [Fact]
    public void Firm_rejects_non_positive_override_quantity()
    {
        var order = NewOrder();

        var act = () => order.Firm(0m, null);

        act.Should().Throw<ArgumentOutOfRangeException>();
        order.Status.Should().Be(PlannedProductionOrderStatus.Planned);
    }

    [Fact]
    public void Release_from_planned_transitions_to_released()
    {
        var order = NewOrder();

        order.Release();

        order.Status.Should().Be(PlannedProductionOrderStatus.Released);
    }

    [Fact]
    public void Release_from_firm_transitions_to_released()
    {
        var order = NewOrder();
        order.Firm(null, null);

        order.Release();

        order.Status.Should().Be(PlannedProductionOrderStatus.Released);
    }

    [Fact]
    public void Close_from_released_transitions_to_closed()
    {
        var order = NewOrder();
        order.Release();

        order.Close();

        order.Status.Should().Be(PlannedProductionOrderStatus.Closed);
    }

    [Fact]
    public void Close_from_planned_is_allowed()
    {
        var order = NewOrder();

        order.Close();

        order.Status.Should().Be(PlannedProductionOrderStatus.Closed);
    }

    [Fact]
    public void Firm_after_release_is_rejected()
    {
        var order = NewOrder();
        order.Release();

        var act = () => order.Firm(null, null);

        act.Should().Throw<InvalidPlannedProductionOrderTransitionException>();
        order.Status.Should().Be(PlannedProductionOrderStatus.Released);
    }

    [Fact]
    public void Firm_when_already_firm_is_rejected()
    {
        var order = NewOrder();
        order.Firm(null, null);

        var act = () => order.Firm(null, null);

        act.Should().Throw<InvalidPlannedProductionOrderTransitionException>();
        order.Status.Should().Be(PlannedProductionOrderStatus.Firm);
    }

    [Fact]
    public void Release_when_already_released_is_rejected()
    {
        var order = NewOrder();
        order.Release();

        var act = () => order.Release();

        act.Should().Throw<InvalidPlannedProductionOrderTransitionException>();
        order.Status.Should().Be(PlannedProductionOrderStatus.Released);
    }

    [Fact]
    public void Any_transition_from_closed_is_rejected()
    {
        var order = NewOrder();
        order.Close();

        var firm = () => order.Firm(null, null);
        var release = () => order.Release();
        var close = () => order.Close();

        firm.Should().Throw<InvalidPlannedProductionOrderTransitionException>();
        release.Should().Throw<InvalidPlannedProductionOrderTransitionException>();
        close.Should().Throw<InvalidPlannedProductionOrderTransitionException>();
        order.Status.Should().Be(PlannedProductionOrderStatus.Closed);
    }
}
