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
        var sut = new InventoryCostingService(_layers);

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
        var sut = new InventoryCostingService(_layers);

        // Only 5 units of layers exist; issuing 8 must FAIL loudly, not value the 3-unit tail at
        // the item's AvgCost (99).
        var act = () => sut.ResolveIssueCostAsync(Item(8m, 99m), FifoProduct(), 8m, T2);

        await act.Should().ThrowAsync<StockMovementValidationException>();
    }

    [Fact]
    public async Task WeightedAverage_uses_avgcost_and_never_reads_layers()
    {
        var sut = new InventoryCostingService(_layers);

        var costing = await sut.ResolveIssueCostAsync(Item(10m, 7m), AverageProduct(), 4m, T2);

        costing.UnitCost.Should().Be(7m);
        costing.TotalCost.Should().Be(28m);
        await _layers.DidNotReceive().GetOpenByStockItemAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _layers.DidNotReceive().AcquireItemLockAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordReceiptLayer_pushes_a_layer_for_fifo_products_only()
    {
        var sut = new InventoryCostingService(_layers);

        await sut.RecordReceiptLayerAsync(Item(0m, 0m), FifoProduct(), 5m, 12m, T2, Guid.NewGuid());
        await _layers.Received(1).AddAsync(
            Arg.Is<StockCostLayer>(l => l.OriginalQuantity == 5m && l.RemainingQuantity == 5m && l.UnitCost == 12m),
            Arg.Any<CancellationToken>());

        await sut.RecordReceiptLayerAsync(Item(0m, 0m), AverageProduct(), 5m, 12m, T2, Guid.NewGuid());
        // No second push: weighted-average products keep no layers.
        await _layers.Received(1).AddAsync(Arg.Any<StockCostLayer>(), Arg.Any<CancellationToken>());
    }
}
