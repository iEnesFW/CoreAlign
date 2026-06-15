using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Mrp.Planning;

namespace CoreAlign.Application.Tests.Mrp.Planning;

public class MrpChangeImpactAnalyzerTests
{
    private readonly MrpPlanningEngine _engine = new(
        new LotSizingCalculator(),
        new DemandForecaster(),
        new ActionMessageGenerator());

    private readonly MrpChangeImpactAnalyzer _sut = new();

    private static IndependentDemandSnapshot Demand(Guid productId, decimal qty, int dayOffset, Guid orderLineId) =>
        new(productId, qty, MrpPlanningTestData.AsOf.AddDays(dayOffset), orderLineId);

    [Fact]
    public void Trace_returns_full_downstream_supply_chain_for_sales_order_line()
    {
        var top = Guid.NewGuid();
        var sub = Guid.NewGuid();
        var raw = Guid.NewGuid();
        var orderLineId = Guid.NewGuid();
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
        var snapshot = MrpPlanningTestData.Snapshot(products, edges, demand: new[] { Demand(top, 5m, 0, orderLineId) });
        var plan = _engine.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        var impact = _sut.Trace(plan, orderLineId);

        impact.RootProductId.Should().Be(top);
        impact.DownstreamSupply.Should().HaveCount(3);
        impact.DownstreamSupply.Should().Contain(s => s.ProductId == top && s.SinkKind == OrderSinkKind.ProductionOrder && s.Quantity == 5m);
        impact.DownstreamSupply.Should().Contain(s => s.ProductId == sub && s.SinkKind == OrderSinkKind.ProductionOrder && s.Quantity == 10m);
        impact.DownstreamSupply.Should().Contain(s => s.ProductId == raw && s.SinkKind == OrderSinkKind.PurchaseRequisition && s.Quantity == 30m);
    }

    [Fact]
    public void Trace_orders_downstream_supply_by_low_level_code()
    {
        var top = Guid.NewGuid();
        var sub = Guid.NewGuid();
        var raw = Guid.NewGuid();
        var orderLineId = Guid.NewGuid();
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
        var snapshot = MrpPlanningTestData.Snapshot(products, edges, demand: new[] { Demand(top, 7m, 0, orderLineId) });
        var plan = _engine.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        var impact = _sut.Trace(plan, orderLineId);

        impact.DownstreamSupply.Select(s => s.LowLevelCode).Should().BeInAscendingOrder();
        impact.DownstreamSupply.First().ProductId.Should().Be(top);
        impact.DownstreamSupply.Last().ProductId.Should().Be(raw);
    }

    [Fact]
    public void Trace_unknown_order_line_returns_empty_impact()
    {
        var top = Guid.NewGuid();
        var product = MrpPlanningTestData.Product(top, "TOP", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Make);
        var snapshot = MrpPlanningTestData.Snapshot(new[] { product }, demand: new[] { Demand(top, 5m, 0, Guid.NewGuid()) });
        var plan = _engine.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        var impact = _sut.Trace(plan, Guid.NewGuid());

        impact.RootProductId.Should().Be(Guid.Empty);
        impact.DownstreamSupply.Should().BeEmpty();
    }

    [Fact]
    public void Trace_buy_root_with_bom_does_not_include_components()
    {
        var purchased = Guid.NewGuid();
        var component = Guid.NewGuid();
        var orderLineId = Guid.NewGuid();
        var products = new[]
        {
            MrpPlanningTestData.Product(purchased, "BUY", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Buy),
            MrpPlanningTestData.Product(component, "CMP", policy: LotSizingPolicy.LotForLot, procurementType: ProcurementType.Buy)
        };
        var edges = new[] { new BomEdgeSnapshot(purchased, component, 4m) };
        var snapshot = MrpPlanningTestData.Snapshot(products, edges, demand: new[] { Demand(purchased, 6m, 0, orderLineId) });
        var plan = _engine.Run(snapshot, MrpBucketKind.Day, horizonDays: 10);

        var impact = _sut.Trace(plan, orderLineId);

        impact.RootProductId.Should().Be(purchased);
        impact.DownstreamSupply.Should().ContainSingle();
        impact.DownstreamSupply.Single().ProductId.Should().Be(purchased);
        impact.DownstreamSupply.Single().SinkKind.Should().Be(OrderSinkKind.PurchaseRequisition);
    }
}
