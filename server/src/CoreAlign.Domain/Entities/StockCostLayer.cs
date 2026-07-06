using CoreAlign.Domain.Common;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

// A FIFO cost layer: one received tranche of a StockItem at a known unit cost. Issues under the
// Fifo costing method consume layers oldest-first (ReceivedAtUtc, then Id — Id is UUIDv7 so ties
// break deterministically). Only created for products whose CostingMethod is Fifo. Σ of a stock
// item's open RemainingQuantity is kept in lockstep with StockItem.OnHand.
public class StockCostLayer : TenantEntity, IHasConcurrencyToken
{
    public Guid StockItemId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? LotId { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal OriginalQuantity { get; private set; }
    public decimal RemainingQuantity { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }

    // The receipt StockMovement that created this layer (audit + reversal linkage).
    public Guid? SourceMovementId { get; private set; }

    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    protected StockCostLayer() { }

    public StockCostLayer(
        Guid stockItemId,
        Guid productId,
        Guid warehouseId,
        Guid? lotId,
        decimal unitCost,
        decimal quantity,
        DateTime receivedAtUtc,
        Guid? sourceMovementId = null)
    {
        if (quantity <= 0m)
        {
            throw new StockMovementValidationException("Cost layer quantity must be positive.");
        }
        StockItemId = stockItemId;
        ProductId = productId;
        WarehouseId = warehouseId;
        LotId = lotId;
        UnitCost = unitCost;
        OriginalQuantity = quantity;
        RemainingQuantity = quantity;
        ReceivedAtUtc = receivedAtUtc;
        SourceMovementId = sourceMovementId;
    }

    // Draws down this layer during a FIFO issue; returns the quantity actually taken (never more
    // than what remains).
    public decimal Consume(decimal quantity)
    {
        if (quantity <= 0m || RemainingQuantity <= 0m)
        {
            return 0m;
        }
        var taken = Math.Min(quantity, RemainingQuantity);
        RemainingQuantity -= taken;
        UpdatedAtUtc = DateTime.UtcNow;
        return taken;
    }
}
