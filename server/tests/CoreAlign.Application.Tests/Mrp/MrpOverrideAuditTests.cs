using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Entities.Mrp;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.Mrp;

public class MrpOverrideAuditTests
{
    private static readonly DateTime Due = new(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Release = new(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

    private static MrpPlannedOrder NewBuyOrder() =>
        new(Guid.NewGuid(), 0, 100m, Due, Release, null, 5m, LotSizingPolicy.LotForLot);

    private static PlannedProductionOrder NewMakeOrder() =>
        new(Guid.NewGuid(), Guid.NewGuid(), 0, 100m, Due, Release, 5m, LotSizingPolicy.LotForLot, null, null);

    [Fact]
    public void Buy_firm_without_override_keeps_originals_null()
    {
        var order = NewBuyOrder();

        order.Firm(overrideQuantity: null, overrideDueDateUtc: null);

        order.IsFirmed.Should().BeTrue();
        order.Quantity.Should().Be(100m);
        order.OriginalQuantity.Should().BeNull();
        order.OriginalDueDateUtc.Should().BeNull();
        order.IsQuantityOverridden.Should().BeFalse();
        order.IsDueDateOverridden.Should().BeFalse();
    }

    [Fact]
    public void Buy_firm_with_quantity_override_captures_original()
    {
        var order = NewBuyOrder();

        order.Firm(overrideQuantity: 150m, overrideDueDateUtc: null);

        order.Quantity.Should().Be(150m);
        order.OriginalQuantity.Should().Be(100m);
        order.IsQuantityOverridden.Should().BeTrue();
        order.OriginalDueDateUtc.Should().BeNull();
    }

    [Fact]
    public void Buy_firm_with_date_override_captures_original_and_preserves_lead_offset()
    {
        var order = NewBuyOrder();
        var newDue = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

        order.Firm(overrideQuantity: null, overrideDueDateUtc: newDue);

        order.DueDateUtc.Should().Be(newDue);
        order.OriginalDueDateUtc.Should().Be(Due);
        order.IsDueDateOverridden.Should().BeTrue();
        (order.DueDateUtc - order.ReleaseDateUtc).Should().Be(Due - Release, "lead-time offset is preserved");
    }

    [Fact]
    public void Buy_firm_with_equal_override_is_not_treated_as_override()
    {
        var order = NewBuyOrder();

        order.Firm(overrideQuantity: 100m, overrideDueDateUtc: Due);

        order.OriginalQuantity.Should().BeNull("an override equal to the current value is a no-op");
        order.OriginalDueDateUtc.Should().BeNull();
    }

    [Fact]
    public void Buy_second_firm_does_not_overwrite_first_original()
    {
        var order = NewBuyOrder();

        order.Firm(overrideQuantity: 150m, overrideDueDateUtc: null);
        order.Firm(overrideQuantity: 200m, overrideDueDateUtc: null);

        order.Quantity.Should().Be(200m);
        order.OriginalQuantity.Should().Be(100m, "the very first pre-override value is retained");
    }

    [Fact]
    public void Make_firm_with_overrides_captures_originals()
    {
        var order = NewMakeOrder();
        var newDue = new DateTime(2026, 1, 25, 0, 0, 0, DateTimeKind.Utc);

        order.Firm(overrideQuantity: 250m, overrideDueDateUtc: newDue);

        order.Status.Should().Be(PlannedProductionOrderStatus.Firm);
        order.Quantity.Should().Be(250m);
        order.OriginalQuantity.Should().Be(100m);
        order.DueDateUtc.Should().Be(newDue);
        order.OriginalDueDateUtc.Should().Be(Due);
    }
}
