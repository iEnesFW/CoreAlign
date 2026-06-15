using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Inventory.Services;

public class StockOpeningBalanceBridge : IStockOpeningBalanceBridge
{
    private readonly IStockItemRepository _stockItems;
    private readonly IProductRepository _products;
    private readonly IStockMovementRepository _movements;

    public StockOpeningBalanceBridge(
        IStockItemRepository stockItems,
        IProductRepository products,
        IStockMovementRepository movements)
    {
        _stockItems = stockItems;
        _products = products;
        _movements = movements;
    }

    public async Task EnsureMaterializedAsync(StockItem item, CancellationToken cancellationToken = default)
    {
        if (item.OnHand != 0m || item.Reserved != 0m || item.LastMovementAtUtc is not null)
        {
            return;
        }

        var siblings = await _stockItems.GetByProductAsync(item.ProductId, cancellationToken);
        var hasStockElsewhere = siblings.Any(s => s.Id != item.Id && (s.OnHand != 0m || s.Reserved != 0m));
        if (hasStockElsewhere)
        {
            return;
        }

        var product = await _products.GetByIdAsync(item.ProductId, cancellationToken);
        if (product is null || product.StockQuantity <= 0m)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var openingCost = product.AverageCost > 0m ? product.AverageCost : product.StandardCost;
        item.SeedOpeningBalance(product.StockQuantity, openingCost, now);
        await _movements.AddAsync(new StockMovement(
            productId: item.ProductId,
            warehouseId: item.WarehouseId,
            type: StockMovementType.OpeningBalance,
            quantity: product.StockQuantity,
            unitCost: openingCost,
            onHandAfter: item.OnHand,
            avgCostAfter: item.AvgCost,
            occurredAtUtc: now,
            sourceDocumentType: StockSourceDocumentType.OpeningBalance,
            notes: "Açılış bakiyesi (ürün stoğundan otomatik)"), cancellationToken);
    }
}
