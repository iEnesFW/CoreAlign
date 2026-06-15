using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Mrp.Planning;

namespace CoreAlign.Application.Tests.Mrp.Planning;

public class MrpMakeVsBuyRoutingTests
{
    private readonly MrpPlanningEngine _sut = new(
        new LotSizingCalculator(),
        new DemandForecaster(),
        new ActionMessageGenerator());

    private static IndependentDemandSnapshot Demand(Guid productId, decimal qty, int dayOffset, Guid? orderLineId = null) =>
        new(productId, qty, MrpPlanningTestData.AsOf.AddDays(dayOffset), orderLineId ?? Guid.NewGuid());

    [Fact]
    public void Buy_item_routes_to_planned_order_and_never_to_production()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "BUY", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Buy);
        var snapshot = MrpPlanningTestData.Snapshot(new[] { product }, demand: new[] { Demand(id, 25m, 4) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);
        var item = result.Items.Single();

        item.PlannedOrders.Should().ContainSingle();
        item.PlannedOrders[0].Quantity.Should().Be(25m);
        item.ProductionOrders.Should().BeEmpty();
    }

    [Fact]
    public void Make_item_routes_to_production_order_and_never_to_requisition()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "MAKE", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Make);
        var snapshot = MrpPlanningTestData.Snapshot(new[] { product }, demand: new[] { Demand(id, 25m, 4) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);
        var item = result.Items.Single();

        item.ProductionOrders.Should().ContainSingle();
        item.ProductionOrders[0].Quantity.Should().Be(25m);
        item.PlannedOrders.Should().BeEmpty();
    }

    [Fact]
    public void Make_item_with_bom_explodes_and_generates_component_demand()
    {
        var assembly = Guid.NewGuid();
        var component = Guid.NewGuid();
        var products = new[]
        {
            MrpPlanningTestData.Product(assembly, "ASM", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Make),
            MrpPlanningTestData.Product(component, "CMP", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Buy)
        };
        var edges = new[] { new BomEdgeSnapshot(assembly, component, 3m) };
        var snapshot = MrpPlanningTestData.Snapshot(products, edges, demand: new[] { Demand(assembly, 10m, 0) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        var asm = result.Items.Single(i => i.ProductId == assembly);
        var cmp = result.Items.Single(i => i.ProductId == component);

        asm.ProductionOrders.Should().ContainSingle();
        asm.ProductionOrders[0].Quantity.Should().Be(10m);

        cmp.PlannedOrders.Should().ContainSingle();
        cmp.PlannedOrders[0].Quantity.Should().Be(30m);
    }

    [Fact]
    public void Make_item_without_bom_produces_production_order_but_no_explosion()
    {
        var id = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(id, "MAKE-LEAF", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Make);
        var snapshot = MrpPlanningTestData.Snapshot(new[] { product }, demand: new[] { Demand(id, 12m, 3) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);
        var item = result.Items.Single();

        item.ProductionOrders.Should().ContainSingle();
        item.Pegs.Should().OnlyContain(p => p.SourceKind == "SalesOrder");
    }

    [Fact]
    public void Buy_item_with_bom_stops_descending_no_component_demand()
    {
        var purchased = Guid.NewGuid();
        var component = Guid.NewGuid();
        var products = new[]
        {
            MrpPlanningTestData.Product(purchased, "BUY-ASM", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Buy),
            MrpPlanningTestData.Product(component, "CMP", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Buy)
        };
        var edges = new[] { new BomEdgeSnapshot(purchased, component, 5m) };
        var snapshot = MrpPlanningTestData.Snapshot(products, edges, demand: new[] { Demand(purchased, 8m, 0) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        var asm = result.Items.Single(i => i.ProductId == purchased);
        var cmp = result.Items.Single(i => i.ProductId == component);

        asm.PlannedOrders.Should().ContainSingle();
        cmp.PlannedOrders.Should().BeEmpty();
        cmp.ProductionOrders.Should().BeEmpty();
        cmp.Buckets.Should().OnlyContain(b => b.GrossRequirements == 0m);
    }

    [Fact]
    public void Recursive_make_make_buy_chain_routes_each_level_correctly()
    {
        var top = Guid.NewGuid();
        var sub = Guid.NewGuid();
        var raw = Guid.NewGuid();
        var products = new[]
        {
            MrpPlanningTestData.Product(top, "TOP", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Make),
            MrpPlanningTestData.Product(sub, "SUB", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Make),
            MrpPlanningTestData.Product(raw, "RAW", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Buy)
        };
        var edges = new[]
        {
            new BomEdgeSnapshot(top, sub, 2m),
            new BomEdgeSnapshot(sub, raw, 3m)
        };
        var snapshot = MrpPlanningTestData.Snapshot(products, edges, demand: new[] { Demand(top, 5m, 0) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        var topItem = result.Items.Single(i => i.ProductId == top);
        var subItem = result.Items.Single(i => i.ProductId == sub);
        var rawItem = result.Items.Single(i => i.ProductId == raw);

        topItem.ProductionOrders.Single().Quantity.Should().Be(5m);
        subItem.ProductionOrders.Single().Quantity.Should().Be(10m);
        rawItem.PlannedOrders.Single().Quantity.Should().Be(30m);

        topItem.PlannedOrders.Should().BeEmpty();
        subItem.PlannedOrders.Should().BeEmpty();
        rawItem.ProductionOrders.Should().BeEmpty();
    }

    [Fact]
    public void Make_to_buy_assembly_does_not_explode_buy_subassembly()
    {
        var top = Guid.NewGuid();
        var boughtSub = Guid.NewGuid();
        var deepRaw = Guid.NewGuid();
        var products = new[]
        {
            MrpPlanningTestData.Product(top, "TOP", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Make),
            MrpPlanningTestData.Product(boughtSub, "SUB", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Buy),
            MrpPlanningTestData.Product(deepRaw, "RAW", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Buy)
        };
        var edges = new[]
        {
            new BomEdgeSnapshot(top, boughtSub, 2m),
            new BomEdgeSnapshot(boughtSub, deepRaw, 4m)
        };
        var snapshot = MrpPlanningTestData.Snapshot(products, edges, demand: new[] { Demand(top, 6m, 0) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        var subItem = result.Items.Single(i => i.ProductId == boughtSub);
        var rawItem = result.Items.Single(i => i.ProductId == deepRaw);

        subItem.PlannedOrders.Single().Quantity.Should().Be(12m);
        rawItem.PlannedOrders.Should().BeEmpty();
        rawItem.Buckets.Should().OnlyContain(b => b.GrossRequirements == 0m);
    }

    [Fact]
    public void Component_pegging_records_parent_chain_for_recursive_make()
    {
        var top = Guid.NewGuid();
        var sub = Guid.NewGuid();
        var raw = Guid.NewGuid();
        var products = new[]
        {
            MrpPlanningTestData.Product(top, "TOP", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Make),
            MrpPlanningTestData.Product(sub, "SUB", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Make),
            MrpPlanningTestData.Product(raw, "RAW", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Buy)
        };
        var edges = new[]
        {
            new BomEdgeSnapshot(top, sub, 1m),
            new BomEdgeSnapshot(sub, raw, 1m)
        };
        var orderLineId = Guid.NewGuid();
        var snapshot = MrpPlanningTestData.Snapshot(products, edges, demand: new[] { Demand(top, 9m, 0, orderLineId) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        var topItem = result.Items.Single(i => i.ProductId == top);
        var subItem = result.Items.Single(i => i.ProductId == sub);
        var rawItem = result.Items.Single(i => i.ProductId == raw);

        topItem.Pegs.Should().Contain(p => p.SourceKind == "SalesOrder" && p.SourceOrderLineId == orderLineId);
        subItem.Pegs.Should().Contain(p => p.SourceKind == "ProductionOrder" && p.SourceParentProductId == top);
        rawItem.Pegs.Should().Contain(p => p.SourceKind == "ProductionOrder" && p.SourceParentProductId == sub);

        subItem.ProductionOrders.Single().PeggingParentProductId.Should().Be(top);
        rawItem.PlannedOrders.Single().PeggingParentProductId.Should().Be(sub);
    }

    [Fact]
    public void Top_level_make_order_pegs_directly_to_sales_order_line()
    {
        var top = Guid.NewGuid();
        var orderLineId = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(top, "TOP", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Make);
        var snapshot = MrpPlanningTestData.Snapshot(new[] { product }, demand: new[] { Demand(top, 4m, 0, orderLineId) });

        var result = _sut.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        var order = result.Items.Single().ProductionOrders.Single();
        order.PeggingSourceOrderLineId.Should().Be(orderLineId);
    }
}
