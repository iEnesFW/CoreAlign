using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Mrp.Planning;

namespace CoreAlign.Application.Tests.Mrp.Planning;

public class MrpPlanningEngineTests
{
    private readonly MrpPlanningEngine _sut = new(
        new LotSizingCalculator(),
        new DemandForecaster(),
        new ActionMessageGenerator());

    private static IndependentDemandSnapshot Demand(Guid productId, decimal qty, int dayOffset) =>
        new(productId, qty, MrpPlanningTestData.AsOf.AddDays(dayOffset), Guid.NewGuid());

    [Fact]
    public void Single_level_net_requirement_produces_planned_order()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "A", onHand: 10m, policy: LotSizingPolicy.LotForLot, leadTimeDays: 0);
        var snapshot = MrpPlanningTestData.Snapshot(
            new[] { product },
            demand: new[] { Demand(id, 30m, 5) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        var item = result.Items.Single();
        item.PlannedOrders.Should().ContainSingle();
        item.PlannedOrders[0].Quantity.Should().Be(20m);
    }

    [Fact]
    public void Multi_level_bom_assigns_low_level_codes_and_explodes()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var products = new[]
        {
            MrpPlanningTestData.Product(a, "A", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Make),
            MrpPlanningTestData.Product(b, "B", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Make),
            MrpPlanningTestData.Product(c, "C", policy: LotSizingPolicy.LotForLot)
        };
        var edges = new[]
        {
            new BomEdgeSnapshot(a, b, 2m),
            new BomEdgeSnapshot(b, c, 3m)
        };
        var snapshot = MrpPlanningTestData.Snapshot(products, edges, demand: new[] { Demand(a, 10m, 0) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        var itemA = result.Items.Single(i => i.ProductId == a);
        var itemB = result.Items.Single(i => i.ProductId == b);
        var itemC = result.Items.Single(i => i.ProductId == c);

        itemA.LowLevelCode.Should().Be(0);
        itemB.LowLevelCode.Should().Be(1);
        itemC.LowLevelCode.Should().Be(2);

        itemB.ProductionOrders.Single().Quantity.Should().Be(20m);
        itemC.PlannedOrders.Single().Quantity.Should().Be(60m);
    }

    [Fact]
    public void Diamond_bom_sums_dependent_demand_from_two_parents()
    {
        var top = Guid.NewGuid();
        var left = Guid.NewGuid();
        var right = Guid.NewGuid();
        var shared = Guid.NewGuid();
        var products = new[]
        {
            MrpPlanningTestData.Product(top, "TOP", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Make),
            MrpPlanningTestData.Product(left, "LEFT", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Make),
            MrpPlanningTestData.Product(right, "RIGHT", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Make),
            MrpPlanningTestData.Product(shared, "SHARED", policy: LotSizingPolicy.LotForLot)
        };
        var edges = new[]
        {
            new BomEdgeSnapshot(top, left, 1m),
            new BomEdgeSnapshot(top, right, 1m),
            new BomEdgeSnapshot(left, shared, 2m),
            new BomEdgeSnapshot(right, shared, 5m)
        };
        var snapshot = MrpPlanningTestData.Snapshot(products, edges, demand: new[] { Demand(top, 4m, 0) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        var sharedItem = result.Items.Single(i => i.ProductId == shared);
        sharedItem.LowLevelCode.Should().Be(2);
        sharedItem.PlannedOrders.Single().Quantity.Should().Be(28m);
    }

    [Fact]
    public void Lead_time_offset_sets_release_before_due()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "A", policy: LotSizingPolicy.LotForLot, leadTimeDays: 3);
        var snapshot = MrpPlanningTestData.Snapshot(new[] { product }, demand: new[] { Demand(id, 10m, 7) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 14);

        var order = result.Items.Single().PlannedOrders.Single();
        order.DueDateUtc.Should().Be(MrpPlanningTestData.AsOf.AddDays(7));
        order.ReleaseDateUtc.Should().Be(MrpPlanningTestData.AsOf.AddDays(4));
    }

    [Fact]
    public void Shortage_inside_lead_time_releases_in_bucket_zero_and_expedites()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "A", policy: LotSizingPolicy.LotForLot, leadTimeDays: 10);
        var snapshot = MrpPlanningTestData.Snapshot(new[] { product }, demand: new[] { Demand(id, 10m, 2) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 14);
        var item = result.Items.Single();

        item.Buckets[0].PlannedReleases.Should().Be(10m);
        item.Actions.Should().Contain(a => a.ActionType == MrpActionType.Expedite);
    }

    [Fact]
    public void Scheduled_receipt_offsets_gross_requirement()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "A", policy: LotSizingPolicy.LotForLot);
        var snapshot = MrpPlanningTestData.Snapshot(
            new[] { product },
            demand: new[] { Demand(id, 30m, 5) },
            receipts: new[] { new ScheduledReceiptSnapshot(id, 30m, MrpPlanningTestData.AsOf.AddDays(3), Guid.NewGuid()) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);
        var item = result.Items.Single();

        item.PlannedOrders.Should().BeEmpty();
        item.Buckets[3].ScheduledReceipts.Should().Be(30m);
    }

    [Fact]
    public void Projected_on_hand_goes_negative_raises_stockout_action()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "A", onHand: 5m, policy: LotSizingPolicy.LotForLot, leadTimeDays: 0, serviceLevelTarget: 0m);
        var snapshot = MrpPlanningTestData.Snapshot(new[] { product }, demand: new[] { Demand(id, 30m, 2) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);
        var item = result.Items.Single();

        item.Actions.Should().Contain(a => a.ActionType == MrpActionType.Release);
        item.Buckets.All(b => b.ProjectedOnHand >= 0m).Should().BeTrue();
    }

    [Fact]
    public void Pegging_links_component_to_parent_planned_order()
    {
        var parent = Guid.NewGuid();
        var child = Guid.NewGuid();
        var products = new[]
        {
            MrpPlanningTestData.Product(parent, "P", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Make),
            MrpPlanningTestData.Product(child, "C", policy: LotSizingPolicy.LotForLot)
        };
        var edges = new[] { new BomEdgeSnapshot(parent, child, 4m) };
        var snapshot = MrpPlanningTestData.Snapshot(products, edges, demand: new[] { Demand(parent, 5m, 0) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);
        var childItem = result.Items.Single(i => i.ProductId == child);

        childItem.Pegs.Should().Contain(p => p.SourceKind == "ProductionOrder" && p.SourceParentProductId == parent);
    }

    [Fact]
    public void On_hand_covers_demand_produces_no_planned_order()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "A", onHand: 100m, policy: LotSizingPolicy.LotForLot);
        var snapshot = MrpPlanningTestData.Snapshot(new[] { product }, demand: new[] { Demand(id, 30m, 5) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        result.Items.Single().PlannedOrders.Should().BeEmpty();
    }

    [Fact]
    public void Reserved_quantity_is_not_double_counted_against_committed_demand()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(
            id, "A", onHand: 30m, reserved: 30m, policy: LotSizingPolicy.LotForLot, leadTimeDays: 0);
        var snapshot = MrpPlanningTestData.Snapshot(
            new[] { product },
            demand: new[] { Demand(id, 30m, 5) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        var item = result.Items.Single();
        item.PlannedOrders.Should().BeEmpty();
        item.Buckets[5].ProjectedOnHand.Should().Be(0m);
    }

    [Fact]
    public void Reserved_does_not_inflate_net_requirement_when_shortfall_real()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(
            id, "A", onHand: 20m, reserved: 20m, policy: LotSizingPolicy.LotForLot, leadTimeDays: 0);
        var snapshot = MrpPlanningTestData.Snapshot(
            new[] { product },
            demand: new[] { Demand(id, 50m, 4) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        var item = result.Items.Single();
        item.PlannedOrders.Should().ContainSingle();
        item.PlannedOrders[0].Quantity.Should().Be(30m);
    }
}
