using CoreAlign.Application.Inventory.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Infrastructure.Services;

// Resolves issue cost per the product's costing method. WeightedAverage (the default) is byte-
// identical to the prior inline `item.AvgCost`; Fifo consumes the oldest cost layers. Standard is
// added in a later slice; until then it falls through to weighted-average.
public class InventoryCostingService : IInventoryCostingService
{
    private readonly IStockCostLayerRepository _layers;
    private readonly IStockItemRepository _stockItems;

    public InventoryCostingService(IStockCostLayerRepository layers, IStockItemRepository stockItems)
    {
        _layers = layers;
        _stockItems = stockItems;
    }

    public async Task<IssueCosting> ResolveIssueCostAsync(
        StockItem item,
        Product product,
        decimal quantity,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (product.CostingMethod != CostingMethod.Fifo)
        {
            var uc = item.AvgCost;
            return new IssueCosting(uc, Math.Round(quantity * uc, 4));
        }

        // Serialize FIFO consumption per (product, warehouse, lot) BEFORE reading layers: the
        // StockItem token guards OnHand arithmetic but not the correctness of the layer plan, so two
        // concurrent issues could otherwise double-consume the same physical units.
        await _layers.AcquireItemLockAsync(item.ProductId, item.WarehouseId, item.LotId, cancellationToken);

        var open = await _layers.GetOpenByStockItemAsync(item.Id, cancellationToken);
        var remaining = quantity;
        var total = 0m;
        foreach (var layer in open)
        {
            if (remaining <= 0m) break;
            var taken = layer.Consume(remaining);
            if (taken <= 0m) continue;
            _layers.Update(layer);
            total += Math.Round(taken * layer.UnitCost, 4);
            remaining -= taken;
        }

        if (remaining > 0m)
        {
            // Hard error, never an AvgCost fallback: an exhausted-layer shortfall means the layer
            // stack has drifted from OnHand. Papering over it would strand a 153 residual and hide
            // the corruption; fail loudly instead.
            throw new StockMovementValidationException(
                $"FIFO cost layers exhausted for product {item.ProductId} at warehouse {item.WarehouseId} " +
                $"(short by {remaining}); cost layers are out of sync with on-hand.");
        }

        var unitCost = quantity > 0m ? Math.Round(total / quantity, 4) : 0m;
        return new IssueCosting(unitCost, total);
    }

    public async Task RecordReceiptLayerAsync(
        StockItem item,
        Product product,
        decimal quantity,
        decimal unitCost,
        DateTime occurredAtUtc,
        Guid? sourceMovementId = null,
        CancellationToken cancellationToken = default)
    {
        if (product.CostingMethod != CostingMethod.Fifo || quantity <= 0m)
        {
            return;
        }

        var layer = new StockCostLayer(
            stockItemId: item.Id,
            productId: item.ProductId,
            warehouseId: item.WarehouseId,
            lotId: item.LotId,
            unitCost: unitCost,
            quantity: quantity,
            receivedAtUtc: occurredAtUtc,
            sourceMovementId: sourceMovementId);
        await _layers.AddAsync(layer, cancellationToken);
    }

    public async Task SeedOpeningLayersAsync(
        Product product,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (product.CostingMethod != CostingMethod.Fifo)
        {
            return;
        }

        var items = await _stockItems.GetByProductAsync(product.Id, cancellationToken);
        foreach (var item in items)
        {
            if (item.OnHand <= 0m) continue;
            var open = await _layers.GetOpenByStockItemAsync(item.Id, cancellationToken);
            if (open.Count > 0) continue;

            // Dated at the item's last movement so the seeded stock stays OLDEST in the queue and a
            // receipt booked after the switch is consumed after it.
            await _layers.AddAsync(
                new StockCostLayer(
                    stockItemId: item.Id,
                    productId: item.ProductId,
                    warehouseId: item.WarehouseId,
                    lotId: item.LotId,
                    unitCost: item.AvgCost,
                    quantity: item.OnHand,
                    receivedAtUtc: item.LastMovementAtUtc ?? occurredAtUtc,
                    sourceMovementId: null),
                cancellationToken);
        }
    }
}
