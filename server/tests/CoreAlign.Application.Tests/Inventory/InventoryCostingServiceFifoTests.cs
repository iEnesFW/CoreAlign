using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Services;

namespace CoreAlign.Application.Tests.Inventory;

// FIFO costing: an issue consumes the oldest cost layers first and its cost is the exact summed
// layer cost. A shortfall (layers exhausted) is a HARD error — never a silent AvgCost fallback,
// which would strand a 153 residual and hide layer/OnHand drift. WeightedAverage never touches
// layers (byte-identical to the historical AvgCost path).
public class InventoryCostingServiceFifoTests
{
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();
    private static readonly Guid ItemId = Guid.NewGuid();
    private static readonly DateTime T1 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T2 = new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IStockCostLayerRepository _layers = Substitute.For<IStockCostLayerRepository>();

    private static StockItem Item(decimal onHand, decimal avgCost)
    {
        var item = new StockItem(ProductId, WarehouseId) { Id = ItemId };
        item.SeedOpeningBalance(onHand, avgCost, T1);
        return item;
    }

    private static Product FifoProduct()
    {
        var p = new Product("SKU-F", "Glass", "m2", 10m, "TRY") { Id = ProductId };
        p.SetCostingMethod(CostingMethod.Fifo);
        return p;
    }

    private static Product AverageProduct() =>
        new("SKU-A", "Widget", "pcs", 10m, "TRY") { Id = ProductId };

    private static StockCostLayer Layer(decimal unitCost, decimal quantity, DateTime receivedAt) =>
        new(ItemId, ProductId, WarehouseId, null, unitCost, quantity, receivedAt);

    [Fact]
    public async Task Fifo_consumes_oldest_layers_first_and_sums_their_cost()
    {
        var layers = new List<StockCostLayer>
        {
            Layer(unitCost: 8m, quantity: 6m, receivedAt: T1),
            Layer(unitCost: 10m, quantity: 4m, receivedAt: T2),
        };
        _layers.GetOpenByStockItemAsync(ItemId, Arg.Any<CancellationToken>()).Returns(layers);
        var sut = new InventoryCostingService(_layers, Substitute.For<IStockItemRepository>());

        // Issue 8: consume 6 @ 8 = 48 from the oldest layer, then 2 @ 10 = 20 → 68 total.
        var costing = await sut.ResolveIssueCostAsync(Item(10m, 99m), FifoProduct(), 8m, T2);

        costing.TotalCost.Should().Be(68m);
        costing.UnitCost.Should().Be(8.5m); // 68 / 8
        layers[0].RemainingQuantity.Should().Be(0m);
        layers[1].RemainingQuantity.Should().Be(2m);
        await _layers.Received(1).AcquireItemLockAsync(ProductId, WarehouseId, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fifo_shortfall_throws_hard_error_never_falls_back_to_avgcost()
    {
        var layers = new List<StockCostLayer> { Layer(unitCost: 8m, quantity: 5m, receivedAt: T1) };
        _layers.GetOpenByStockItemAsync(ItemId, Arg.Any<CancellationToken>()).Returns(layers);
        var sut = new InventoryCostingService(_layers, Substitute.For<IStockItemRepository>());

        // Only 5 units of layers exist; issuing 8 must FAIL loudly, not value the 3-unit tail at
        // the item's AvgCost (99).
        var act = () => sut.ResolveIssueCostAsync(Item(8m, 99m), FifoProduct(), 8m, T2);

        await act.Should().ThrowAsync<StockMovementValidationException>();
    }

    [Fact]
    public async Task WeightedAverage_uses_avgcost_and_never_reads_layers()
    {
        var sut = new InventoryCostingService(_layers, Substitute.For<IStockItemRepository>());

        var costing = await sut.ResolveIssueCostAsync(Item(10m, 7m), AverageProduct(), 4m, T2);

        costing.UnitCost.Should().Be(7m);
        costing.TotalCost.Should().Be(28m);
        await _layers.DidNotReceive().GetOpenByStockItemAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _layers.DidNotReceive().AcquireItemLockAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordReceiptLayer_pushes_a_layer_for_fifo_products_only()
    {
        var sut = new InventoryCostingService(_layers, Substitute.For<IStockItemRepository>());

        await sut.RecordReceiptLayerAsync(Item(0m, 0m), FifoProduct(), 5m, 12m, T2, Guid.NewGuid());
        await _layers.Received(1).AddAsync(
            Arg.Is<StockCostLayer>(l => l.OriginalQuantity == 5m && l.RemainingQuantity == 5m && l.UnitCost == 12m),
            Arg.Any<CancellationToken>());

        await sut.RecordReceiptLayerAsync(Item(0m, 0m), AverageProduct(), 5m, 12m, T2, Guid.NewGuid());
        // No second push: weighted-average products keep no layers.
        await _layers.Received(1).AddAsync(Arg.Any<StockCostLayer>(), Arg.Any<CancellationToken>());
    }
}

// Switching an already-stocked product to FIFO leaves it with no cost layers, so the very next
// issue hit the exhausted-layer hard error and the product silently stopped being sellable. The
// on-hand has to enter the queue at its weighted-average cost — the only basis on record for it.
public class FifoOpeningLayerSeedTests
{
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid WarehouseA = Guid.NewGuid();
    private static readonly Guid WarehouseB = Guid.NewGuid();
    private static readonly DateTime Seeded = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Now = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IStockCostLayerRepository _layers = Substitute.For<IStockCostLayerRepository>();
    private readonly IStockItemRepository _items = Substitute.For<IStockItemRepository>();
    private readonly List<StockCostLayer> _added = new();
    private readonly InventoryCostingService _sut;

    public FifoOpeningLayerSeedTests()
    {
        _layers.AddAsync(Arg.Do<StockCostLayer>(l => _added.Add(l)), Arg.Any<CancellationToken>());
        _layers.GetOpenByStockItemAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StockCostLayer>());
        _sut = new InventoryCostingService(_layers, _items);
    }

    private static Product FifoProduct()
    {
        var p = new Product("SKU-F", "Glass", "m2", 10m, "TRY") { Id = ProductId };
        p.SetCostingMethod(CostingMethod.Fifo);
        return p;
    }

    private static StockItem Stocked(Guid warehouseId, decimal onHand, decimal avgCost)
    {
        var item = new StockItem(ProductId, warehouseId) { Id = Guid.NewGuid() };
        item.SeedOpeningBalance(onHand, avgCost, Seeded);
        return item;
    }

    [Fact]
    public async Task Every_stocked_warehouse_gets_an_opening_layer_at_its_average_cost()
    {
        _items.GetByProductAsync(ProductId, Arg.Any<CancellationToken>())
            .Returns(new[] { Stocked(WarehouseA, 10m, 7m), Stocked(WarehouseB, 4m, 9m) });

        await _sut.SeedOpeningLayersAsync(FifoProduct(), Now);

        _added.Should().HaveCount(2);
        _added.Should().Contain(l => l.WarehouseId == WarehouseA && l.OriginalQuantity == 10m && l.UnitCost == 7m);
        _added.Should().Contain(l => l.WarehouseId == WarehouseB && l.OriginalQuantity == 4m && l.UnitCost == 9m);
        _added.Should().OnlyContain(l => l.ReceivedAtUtc == Seeded, "seeded stock stays oldest in the queue");
    }

    [Fact]
    public async Task An_empty_warehouse_is_skipped()
    {
        _items.GetByProductAsync(ProductId, Arg.Any<CancellationToken>())
            .Returns(new[] { Stocked(WarehouseA, 0m, 7m) });

        await _sut.SeedOpeningLayersAsync(FifoProduct(), Now);

        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task A_warehouse_that_already_has_a_layer_is_not_seeded_again()
    {
        var item = Stocked(WarehouseA, 10m, 7m);
        _items.GetByProductAsync(ProductId, Arg.Any<CancellationToken>()).Returns(new[] { item });
        _layers.GetOpenByStockItemAsync(item.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { new StockCostLayer(item.Id, ProductId, WarehouseA, null, 7m, 10m, Seeded) });

        await _sut.SeedOpeningLayersAsync(FifoProduct(), Now);

        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task A_weighted_average_product_is_a_no_op()
    {
        _items.GetByProductAsync(ProductId, Arg.Any<CancellationToken>())
            .Returns(new[] { Stocked(WarehouseA, 10m, 7m) });

        await _sut.SeedOpeningLayersAsync(new Product("SKU-A", "Widget", "pcs", 10m, "TRY") { Id = ProductId }, Now);

        _added.Should().BeEmpty();
        await _items.DidNotReceive().GetByProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
