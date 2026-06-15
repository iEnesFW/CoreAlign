using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Mrp.Planning;

namespace CoreAlign.Application.Tests.Mrp.Planning;

public class MrpFirmedSupplyTests
{
    private readonly MrpPlanningEngine _sut = new(
        new LotSizingCalculator(),
        new DemandForecaster(),
        new ActionMessageGenerator());

    private static IndependentDemandSnapshot Demand(Guid productId, decimal qty, int dayOffset, Guid? orderLineId = null) =>
        new(productId, qty, MrpPlanningTestData.AsOf.AddDays(dayOffset), orderLineId ?? Guid.NewGuid());

    private static FirmedSupplySnapshot FirmedBuy(Guid productId, decimal qty, int dueDayOffset) =>
        new(productId, qty, MrpPlanningTestData.AsOf.AddDays(dueDayOffset), MrpPlanningTestData.AsOf.AddDays(dueDayOffset), ProcurementType.Buy, Guid.NewGuid());

    private static FirmedSupplySnapshot FirmedMake(Guid productId, decimal qty, int dueDayOffset, int releaseDayOffset) =>
        new(productId, qty, MrpPlanningTestData.AsOf.AddDays(dueDayOffset), MrpPlanningTestData.AsOf.AddDays(releaseDayOffset), ProcurementType.Make, Guid.NewGuid());

    [Fact]
    public void Firmed_buy_order_covering_demand_suppresses_new_planned_order()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "BUY", procurementType: ProcurementType.Buy);
        var snapshot = MrpPlanningTestData.Snapshot(
            new[] { product },
            demand: new[] { Demand(id, 25m, 4) },
            firmedSupply: new[] { FirmedBuy(id, 25m, 4) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);
        var item = result.Items.Single();

        item.PlannedOrders.Should().BeEmpty("the firmed order already covers the full requirement");
        item.Buckets[4].ScheduledReceipts.Should().Be(25m);
        item.Buckets[4].ProjectedOnHand.Should().Be(0m);
    }

    [Fact]
    public void Firmed_buy_order_partially_covering_only_plans_the_shortfall()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "BUY", procurementType: ProcurementType.Buy);
        var snapshot = MrpPlanningTestData.Snapshot(
            new[] { product },
            demand: new[] { Demand(id, 25m, 4) },
            firmedSupply: new[] { FirmedBuy(id, 10m, 4) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);
        var item = result.Items.Single();

        item.PlannedOrders.Should().ContainSingle();
        item.PlannedOrders[0].Quantity.Should().Be(15m, "25 demand minus 10 firmed supply");
    }

    [Fact]
    public void Regenerating_without_firm_replans_full_quantity()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "BUY", procurementType: ProcurementType.Buy);
        var snapshot = MrpPlanningTestData.Snapshot(
            new[] { product },
            demand: new[] { Demand(id, 25m, 4) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        result.Items.Single().PlannedOrders.Single().Quantity.Should().Be(25m);
    }

    [Fact]
    public void Firmed_make_order_suppresses_new_production_but_still_explodes_components()
    {
        var assembly = Guid.NewGuid();
        var component = Guid.NewGuid();
        var products = new[]
        {
            MrpPlanningTestData.Product(assembly, "ASM", procurementType: ProcurementType.Make),
            MrpPlanningTestData.Product(component, "CMP", procurementType: ProcurementType.Buy)
        };
        var edges = new[] { new BomEdgeSnapshot(assembly, component, 3m) };
        var snapshot = MrpPlanningTestData.Snapshot(
            products,
            edges,
            demand: new[] { Demand(assembly, 10m, 5) },
            firmedSupply: new[] { FirmedMake(assembly, 10m, dueDayOffset: 5, releaseDayOffset: 5) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        var asm = result.Items.Single(i => i.ProductId == assembly);
        var cmp = result.Items.Single(i => i.ProductId == component);

        asm.ProductionOrders.Should().BeEmpty("the firmed production order already covers assembly demand");
        cmp.PlannedOrders.Should().ContainSingle("the firmed production order's dependent demand must still be planned");
        cmp.PlannedOrders[0].Quantity.Should().Be(30m, "10 firmed assembly x 3 per");
    }

    [Fact]
    public void Firmed_supply_beyond_horizon_clamps_to_last_bucket_without_throwing()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "BUY", procurementType: ProcurementType.Buy);
        var snapshot = MrpPlanningTestData.Snapshot(
            new[] { product },
            demand: new[] { Demand(id, 5m, 2) },
            firmedSupply: new[] { FirmedBuy(id, 5m, dueDayOffset: 999) });

        var act = () => _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        act.Should().NotThrow();
        var item = act().Items.Single();
        item.PlannedOrders.Should().ContainSingle("out-of-horizon firmed supply does not cover near-term demand");
    }
}
