using CoreAlign.Domain.Entities.Mrp;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Mrp;

public class MrpPlanningEntityTests
{
    private static MrpPlannedOrder NewPlannedOrder(decimal qty = 40m) => new(
        productId: Guid.NewGuid(),
        lowLevelCode: 0,
        quantity: qty,
        dueDateUtc: new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
        releaseDateUtc: new DateTime(2026, 6, 26, 0, 0, 0, DateTimeKind.Utc),
        preferredSupplierId: Guid.NewGuid(),
        estimatedUnitCost: 5m,
        sourcePolicy: LotSizingPolicy.MinMax);

    [Fact]
    public void BuildIdempotencyKey_is_stable_for_same_inputs()
    {
        var asOf = new DateTime(2026, 6, 16, 9, 30, 0, DateTimeKind.Utc);
        var key1 = MrpPlanRun.BuildIdempotencyKey(asOf.Date, MrpBucketKind.Day, 60);
        var key2 = MrpPlanRun.BuildIdempotencyKey(asOf.Date, MrpBucketKind.Day, 60);
        var different = MrpPlanRun.BuildIdempotencyKey(asOf.Date, MrpBucketKind.Week, 60);

        key1.Should().Be(key2);
        key1.Should().Be("20260616:Day:60");
        different.Should().NotBe(key1);
    }

    [Fact]
    public void PlanRun_ctor_normalizes_asof_to_utc()
    {
        var unspecified = new DateTime(2026, 6, 16, 0, 0, 0, DateTimeKind.Unspecified);
        var run = new MrpPlanRun("MRP00001", unspecified, MrpBucketKind.Day, 60, Guid.NewGuid());

        run.AsOfDateUtc.Kind.Should().Be(DateTimeKind.Utc);
        run.Status.Should().Be(MrpPlanRunStatus.Committed);
        run.IdempotencyKey.Should().Be("20260616:Day:60");
    }

    [Fact]
    public void PlanRun_ctor_rejects_blank_number_and_nonpositive_horizon()
    {
        var act1 = () => new MrpPlanRun(" ", DateTime.UtcNow, MrpBucketKind.Day, 60, Guid.NewGuid());
        var act2 = () => new MrpPlanRun("MRP00001", DateTime.UtcNow, MrpBucketKind.Day, 0, Guid.NewGuid());

        act1.Should().Throw<ArgumentException>();
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PlanRun_add_children_updates_summary_counts()
    {
        var run = new MrpPlanRun("MRP00001", DateTime.UtcNow, MrpBucketKind.Day, 60, Guid.NewGuid());
        run.AddPlannedOrder(NewPlannedOrder());
        run.AddActionMessage(new MrpActionMessage(
            Guid.NewGuid(), MrpActionType.Release, MrpActionSeverity.Warning, 40m,
            null, null, null, null, 3, "Release"));
        run.SetSummary(7);

        run.ProductsEvaluated.Should().Be(7);
        run.PlannedOrderCount.Should().Be(1);
        run.ActionMessageCount.Should().Be(1);
    }

    [Fact]
    public void PlannedOrder_ctor_rejects_nonpositive_quantity()
    {
        var act = () => new MrpPlannedOrder(
            Guid.NewGuid(), 0, 0m,
            DateTime.UtcNow, DateTime.UtcNow, null, 1m, LotSizingPolicy.LotForLot);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PlannedOrder_firm_overrides_quantity_and_shifts_release_by_lead_offset()
    {
        var order = NewPlannedOrder();
        var newDue = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);

        order.Firm(overrideQuantity: 55m, overrideDueDateUtc: newDue);

        order.IsFirmed.Should().BeTrue();
        order.Quantity.Should().Be(55m);
        order.DueDateUtc.Should().Be(newDue);
        order.ReleaseDateUtc.Should().Be(new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void PlannedOrder_firm_rejects_nonpositive_override_quantity()
    {
        var order = NewPlannedOrder();
        var act = () => order.Firm(overrideQuantity: 0m, overrideDueDateUtc: null);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PlannedOrder_release_sets_requisition_and_blocks_second_release()
    {
        var order = NewPlannedOrder();
        var requisitionId = Guid.NewGuid();

        order.MarkReleased(requisitionId);

        order.IsReleased.Should().BeTrue();
        order.ConvertedRequisitionId.Should().Be(requisitionId);

        var act = () => order.MarkReleased(Guid.NewGuid());
        act.Should().Throw<MrpPlannedOrderAlreadyReleasedException>();
        order.ConvertedRequisitionId.Should().Be(requisitionId);
    }

    [Fact]
    public void PlannedOrder_firm_after_release_is_rejected()
    {
        var order = NewPlannedOrder();
        order.MarkReleased(Guid.NewGuid());

        var act = () => order.Firm(overrideQuantity: 10m, overrideDueDateUtc: null);
        act.Should().Throw<MrpPlannedOrderAlreadyReleasedException>();
    }

    [Fact]
    public void ActionMessage_dismiss_is_idempotent()
    {
        var message = new MrpActionMessage(
            Guid.NewGuid(), MrpActionType.ProjectedStockout, MrpActionSeverity.Critical, 10m,
            null, null, null, null, 0, "Stockout");
        var userId = Guid.NewGuid();

        message.Dismiss(userId);
        var firstDismissedAt = message.DismissedAtUtc;
        message.Dismiss(Guid.NewGuid());

        message.IsDismissed.Should().BeTrue();
        message.DismissedByUserId.Should().Be(userId);
        message.DismissedAtUtc.Should().Be(firstDismissedAt);
    }

    [Fact]
    public void ActionMessage_ctor_normalizes_dates_to_utc()
    {
        var unspecified = new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Unspecified);
        var message = new MrpActionMessage(
            Guid.NewGuid(), MrpActionType.RescheduleIn, MrpActionSeverity.Info, 1m,
            unspecified, unspecified, Guid.NewGuid(), null, 5, "Reschedule");

        message.CurrentDateUtc!.Value.Kind.Should().Be(DateTimeKind.Utc);
        message.SuggestedDateUtc!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }
}
