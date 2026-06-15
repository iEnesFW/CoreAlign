using CoreAlign.Application.Mrp.Distribution;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.Mrp.Distribution;

public class DistributionPlannerTests
{
    private readonly DistributionPlanner _sut = new();

    private static DistributionWarehouseSnapshot Warehouse(
        Guid id,
        bool isDefault = false,
        WarehouseType type = WarehouseType.Main) =>
        new(id, isDefault, type);

    private static WarehouseStockSnapshot Stock(
        Guid productId,
        Guid warehouseId,
        decimal onHand,
        decimal reserved = 0m,
        decimal demand = 0m) =>
        new(productId, warehouseId, onHand, reserved, demand);

    private static DistributionInput Input(
        IReadOnlyList<Guid> productIds,
        IReadOnlyList<DistributionWarehouseSnapshot> warehouses,
        IReadOnlyList<WarehouseStockSnapshot> stock) =>
        new(
            productIds.Select(id => new DistributionProductSnapshot(id)).ToList(),
            warehouses,
            stock);

    [Fact]
    public void Surplus_warehouse_supplies_short_warehouse()
    {
        var product = Guid.NewGuid();
        var wSurplus = Guid.NewGuid();
        var wShort = Guid.NewGuid();

        var input = Input(
            new[] { product },
            new[] { Warehouse(wSurplus), Warehouse(wShort, isDefault: true) },
            new[]
            {
                Stock(product, wSurplus, onHand: 100m, demand: 40m),
                Stock(product, wShort, onHand: 10m, demand: 30m)
            });

        var plan = _sut.Plan(input);

        plan.Transfers.Should().ContainSingle();
        var transfer = plan.Transfers.Single();
        transfer.ProductId.Should().Be(product);
        transfer.FromWarehouseId.Should().Be(wSurplus);
        transfer.ToWarehouseId.Should().Be(wShort);
        transfer.Quantity.Should().Be(20m);
        plan.ExternalReplenishment.Should().BeEmpty();
    }

    [Fact]
    public void Transfer_never_exceeds_surplus_conservation()
    {
        var product = Guid.NewGuid();
        var wSurplus = Guid.NewGuid();
        var wShort = Guid.NewGuid();

        var input = Input(
            new[] { product },
            new[] { Warehouse(wSurplus), Warehouse(wShort, isDefault: true) },
            new[]
            {
                Stock(product, wSurplus, onHand: 15m),
                Stock(product, wShort, onHand: 0m, demand: 50m)
            });

        var plan = _sut.Plan(input);

        plan.Transfers.Should().ContainSingle();
        plan.Transfers.Single().Quantity.Should().Be(15m);
        plan.Transfers.Sum(t => t.Quantity).Should().BeLessOrEqualTo(15m);

        plan.ExternalReplenishment.Should().ContainSingle();
        var external = plan.ExternalReplenishment.Single();
        external.WarehouseId.Should().Be(wShort);
        external.Quantity.Should().Be(35m);
    }

    [Fact]
    public void Multi_warehouse_greedy_drains_largest_surplus_into_largest_shortfall_first()
    {
        var product = Guid.NewGuid();
        var bigSurplus = Guid.NewGuid();
        var smallSurplus = Guid.NewGuid();
        var bigShort = Guid.NewGuid();
        var smallShort = Guid.NewGuid();

        var input = Input(
            new[] { product },
            new[]
            {
                Warehouse(bigSurplus, isDefault: true),
                Warehouse(smallSurplus),
                Warehouse(bigShort),
                Warehouse(smallShort)
            },
            new[]
            {
                Stock(product, bigSurplus, onHand: 100m),
                Stock(product, smallSurplus, onHand: 20m),
                Stock(product, bigShort, onHand: 0m, demand: 80m),
                Stock(product, smallShort, onHand: 0m, demand: 10m)
            });

        var plan = _sut.Plan(input);

        var totalMoved = plan.Transfers.Sum(t => t.Quantity);
        totalMoved.Should().Be(90m);

        var fromBigToBig = plan.Transfers
            .First(t => t.FromWarehouseId == bigSurplus && t.ToWarehouseId == bigShort);
        fromBigToBig.Quantity.Should().Be(80m);

        plan.Transfers.Sum(t => t.Quantity)
            .Should().BeLessOrEqualTo(120m);
        plan.ExternalReplenishment.Should().BeEmpty();
    }

    [Fact]
    public void Never_suggests_self_transfer()
    {
        var product = Guid.NewGuid();
        var w = Guid.NewGuid();

        var input = Input(
            new[] { product },
            new[] { Warehouse(w, isDefault: true) },
            new[] { Stock(product, w, onHand: 50m, demand: 20m) });

        var plan = _sut.Plan(input);

        plan.Transfers.Should().BeEmpty();
        plan.Transfers.Should().NotContain(t => t.FromWarehouseId == t.ToWarehouseId);
    }

    [Fact]
    public void All_short_no_surplus_yields_zero_transfers_and_flags_external_replenishment()
    {
        var product = Guid.NewGuid();
        var w1 = Guid.NewGuid();
        var w2 = Guid.NewGuid();

        var input = Input(
            new[] { product },
            new[] { Warehouse(w1, isDefault: true), Warehouse(w2) },
            new[]
            {
                Stock(product, w1, onHand: 0m, demand: 25m),
                Stock(product, w2, onHand: 5m, demand: 30m)
            });

        var plan = _sut.Plan(input);

        plan.Transfers.Should().BeEmpty();
        plan.ExternalReplenishment.Should().HaveCount(2);
        plan.ExternalReplenishment.Single(e => e.WarehouseId == w1).Quantity.Should().Be(25m);
        plan.ExternalReplenishment.Single(e => e.WarehouseId == w2).Quantity.Should().Be(25m);
    }

    [Fact]
    public void Null_warehouse_demand_is_attributed_to_default_warehouse()
    {
        var product = Guid.NewGuid();
        var wDefault = Guid.NewGuid();
        var wSurplus = Guid.NewGuid();

        var input = Input(
            new[] { product },
            new[] { Warehouse(wDefault, isDefault: true), Warehouse(wSurplus) },
            new[]
            {
                Stock(product, wDefault, onHand: 10m),
                Stock(product, wSurplus, onHand: 60m),
                Stock(product, Guid.Empty, onHand: 0m, demand: 25m)
            });

        var plan = _sut.Plan(input);

        var defaultNet = plan.NetPositions.Single(n => n.WarehouseId == wDefault);
        defaultNet.Demand.Should().Be(25m);
        defaultNet.Net.Should().Be(-15m);

        var transfer = plan.Transfers.Single();
        transfer.FromWarehouseId.Should().Be(wSurplus);
        transfer.ToWarehouseId.Should().Be(wDefault);
        transfer.Quantity.Should().Be(15m);
        plan.ExternalReplenishment.Should().BeEmpty();
    }

    [Fact]
    public void Null_warehouse_demand_falls_back_to_main_type_when_no_default()
    {
        var product = Guid.NewGuid();
        var wMain = Guid.NewGuid();
        var wSurplus = Guid.NewGuid();

        var input = Input(
            new[] { product },
            new[]
            {
                Warehouse(wMain, isDefault: false, type: WarehouseType.Main),
                Warehouse(wSurplus, isDefault: false, type: WarehouseType.Transit)
            },
            new[]
            {
                Stock(product, wMain, onHand: 0m),
                Stock(product, wSurplus, onHand: 40m),
                Stock(product, Guid.Empty, onHand: 0m, demand: 12m)
            });

        var plan = _sut.Plan(input);

        plan.NetPositions.Single(n => n.WarehouseId == wMain).Demand.Should().Be(12m);
        plan.Transfers.Single().ToWarehouseId.Should().Be(wMain);
        plan.Transfers.Single().Quantity.Should().Be(12m);
    }

    [Fact]
    public void Available_uses_physical_onhand_not_minus_reserved()
    {
        // Reserved must NOT be subtracted from available: Reserved already represents the
        // allocated open order lines (= the same quantity 'demand' carries), so subtracting
        // both double-counts committed demand and invents phantom shortfalls (MRP-BUG-6 class).
        var product = Guid.NewGuid();
        var wSurplus = Guid.NewGuid();
        var wShort = Guid.NewGuid();

        var input = Input(
            new[] { product },
            new[] { Warehouse(wSurplus, isDefault: true), Warehouse(wShort) },
            new[]
            {
                Stock(product, wSurplus, onHand: 100m, reserved: 70m),
                Stock(product, wShort, onHand: 0m, demand: 20m)
            });

        var plan = _sut.Plan(input);

        plan.NetPositions.Single(n => n.WarehouseId == wSurplus).Available.Should().Be(100m,
            "available = physical on-hand; Reserved is not subtracted (it would double-count demand)");
        plan.Transfers.Single().Quantity.Should().Be(20m);
        plan.ExternalReplenishment.Should().BeEmpty();
    }

    [Fact]
    public void Deterministic_tie_break_by_warehouse_id()
    {
        var product = Guid.NewGuid();
        var wSurplusA = new Guid("00000000-0000-0000-0000-0000000000aa");
        var wSurplusB = new Guid("00000000-0000-0000-0000-0000000000bb");
        var wShort = new Guid("00000000-0000-0000-0000-0000000000cc");

        var input = Input(
            new[] { product },
            new[] { Warehouse(wSurplusA), Warehouse(wSurplusB), Warehouse(wShort, isDefault: true) },
            new[]
            {
                Stock(product, wSurplusA, onHand: 50m),
                Stock(product, wSurplusB, onHand: 50m),
                Stock(product, wShort, onHand: 0m, demand: 30m)
            });

        var first = _sut.Plan(input).Transfers.Single();
        var second = _sut.Plan(input).Transfers.Single();

        first.FromWarehouseId.Should().Be(wSurplusA);
        second.FromWarehouseId.Should().Be(first.FromWarehouseId);
    }
}
