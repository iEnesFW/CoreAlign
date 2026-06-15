using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Inventory.Services;

public interface IStockOpeningBalanceBridge
{
    /// <summary>
    /// The first time a product is stocked in a warehouse, materializes its
    /// recorded global on-hand (<c>Product.StockQuantity</c>) as a per-warehouse
    /// opening balance so existing stock becomes available to the per-warehouse
    /// ledger. Guarded to a fresh stock item with no stock anywhere else, so it can
    /// never double-count across warehouses; a no-op once the item has any movement.
    /// </summary>
    Task EnsureMaterializedAsync(StockItem item, CancellationToken cancellationToken = default);
}
