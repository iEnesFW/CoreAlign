using CoreAlign.Application.Inventory.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Services;

namespace CoreAlign.Application.Tests.Inventory;

/// <summary>
/// INVENTORY T5 — cycle-count variances must be distinguishable from manual
/// adjustments in the audit trail. AllocationService.AdjustAsync defaults to
/// AdjustmentPositive/AdjustmentNegative (manual adjustments), but honours an
/// optional movement-type override so cycle-count posts emit
/// CountVariancePositive/CountVarianceNegative. The quantity, unit cost and GL
/// inputs are untouched — only the movement TYPE label changes.
/// </summary>
public class AdjustMovementTypeOverrideTests
{
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid WarehouseId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IStockItemRepository _stockItems = Substitute.For<IStockItemRepository>();
    private readonly IStockMovementRepository _movements = Substitute.For<IStockMovementRepository>();
    private readonly IStockAllocationRepository _allocations = Substitute.For<IStockAllocationRepository>();
    private readonly IWarehouseRepository _warehouses = Substitute.For<IWarehouseRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();

    private AllocationService BuildService() =>
        new(_stockItems, _movements, _allocations, _warehouses, _products,
            new StockOpeningBalanceBridge(_stockItems, _products, _movements),
            new InventoryCostingService(Substitute.For<CoreAlign.Domain.Interfaces.IStockCostLayerRepository>()));

    private StockItem SeedStock(decimal onHand, decimal avgCost)
    {
        var item = new StockItem(ProductId, WarehouseId) { Id = Guid.NewGuid(), TenantId = TenantId };
        if (onHand > 0m) item.SeedOpeningBalance(onHand, avgCost, DateTime.UtcNow);
        _stockItems.GetOrCreateAsync(ProductId, WarehouseId, null, Arg.Any<CancellationToken>()).Returns(item);
        return item;
    }

    [Fact]
    public async Task Adjust_without_override_emits_manual_adjustment_types()
    {
        SeedStock(onHand: 100m, avgCost: 5m);

        await BuildService().AdjustAsync(new StockAdjustmentRequest(
            ProductId, WarehouseId, Delta: -3m, UnitCost: null,
            StockSourceDocumentType.Adjustment, null, null, "Manual"));

        await _movements.Received(1).AddAsync(
            Arg.Is<StockMovement>(m => m.Type == StockMovementType.AdjustmentNegative),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Adjust_with_override_emits_count_variance_negative()
    {
        SeedStock(onHand: 100m, avgCost: 5m);

        await BuildService().AdjustAsync(new StockAdjustmentRequest(
            ProductId, WarehouseId, Delta: -3m, UnitCost: null,
            StockSourceDocumentType.CycleCount, null, null, "Sayım",
            PositiveMovementType: StockMovementType.CountVariancePositive,
            NegativeMovementType: StockMovementType.CountVarianceNegative));

        await _movements.Received(1).AddAsync(
            Arg.Is<StockMovement>(m =>
                m.Type == StockMovementType.CountVarianceNegative && m.Quantity == 3m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Adjust_with_override_emits_count_variance_positive()
    {
        SeedStock(onHand: 100m, avgCost: 5m);

        await BuildService().AdjustAsync(new StockAdjustmentRequest(
            ProductId, WarehouseId, Delta: 4m, UnitCost: 5m,
            StockSourceDocumentType.CycleCount, null, null, "Sayım",
            PositiveMovementType: StockMovementType.CountVariancePositive,
            NegativeMovementType: StockMovementType.CountVarianceNegative));

        await _movements.Received(1).AddAsync(
            Arg.Is<StockMovement>(m =>
                m.Type == StockMovementType.CountVariancePositive && m.Quantity == 4m),
            Arg.Any<CancellationToken>());
    }
}
