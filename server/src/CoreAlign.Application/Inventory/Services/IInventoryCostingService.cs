using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Inventory.Services;

/// <summary>
/// Single decision point for the cost relieved when stock is issued. Every issue site (order
/// confirm, shipment consume, direct issue) routes its cost through here so a product's costing
/// method (weighted-average today; FIFO / standard opt-in) is applied uniformly and, for FIFO,
/// cost layers are consumed exactly once. The returned <see cref="IssueCosting.TotalCost"/> is the
/// authoritative cost of record that flows to the COGS journal (movement.TotalCost).
/// </summary>
public interface IInventoryCostingService
{
    Task<IssueCosting> ResolveIssueCostAsync(
        StockItem item,
        Product product,
        decimal quantity,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a received tranche as a FIFO cost layer (no-op unless the product's costing method is
    /// Fifo). Called at every receipt point so a subsequent FIFO issue can consume it oldest-first.
    /// </summary>
    Task RecordReceiptLayerAsync(
        StockItem item,
        Product product,
        decimal quantity,
        decimal unitCost,
        DateTime occurredAtUtc,
        Guid? sourceMovementId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a FIFO layer for stock a product already holds when its costing method is switched to
    /// Fifo. Without it the first issue hits the exhausted-layer hard error and the product can no
    /// longer be sold — the existing on-hand enters FIFO at its weighted-average cost, which is the
    /// only basis on record for it. No-op unless the product is Fifo, and it never touches a
    /// stock item that already has an open layer.
    /// </summary>
    Task SeedOpeningLayersAsync(
        Product product,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default);
}

/// <summary>The unit cost to stamp on the issue movement and the exact total relieved. They agree
/// for weighted-average; for FIFO the total is the summed consumed-layer cost and the unit cost is
/// its 4dp quotient.</summary>
public readonly record struct IssueCosting(decimal UnitCost, decimal TotalCost);
