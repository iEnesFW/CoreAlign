using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IStockItemRepository
{
    Task<StockItem?> GetAsync(Guid productId, Guid warehouseId, Guid? lotId, CancellationToken cancellationToken = default);
    Task<StockItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockItem>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockItem>> GetByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    /// <summary>Live OnHand keyed by (product, lot) for one warehouse, scoped to the
    /// given products — one query instead of per-line GetAsync round-trips.</summary>
    Task<IReadOnlyDictionary<(Guid ProductId, Guid? LotId), decimal>> GetOnHandByProductLotAsync(
        Guid warehouseId,
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken = default);
    /// <summary>Slim list projection — see <see cref="StockItemSearchRow"/>.</summary>
    Task<IReadOnlyList<StockItemSearchRow>> SearchAsync(
        Guid? productId,
        Guid? warehouseId,
        bool onlyBelowReorder,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<int> CountAsync(Guid? productId, Guid? warehouseId, bool onlyBelowReorder, CancellationToken cancellationToken = default);
    Task<decimal> SumOnHandAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<decimal> SumReservedAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, (decimal OnHand, decimal Reserved)>> SumOnHandAndReservedByProductsAsync(
        IEnumerable<Guid> productIds,
        Guid? warehouseId,
        CancellationToken cancellationToken = default);
    Task<StockItem> GetOrCreateAsync(Guid productId, Guid warehouseId, Guid? lotId, CancellationToken cancellationToken = default);
    Task AddAsync(StockItem item, CancellationToken cancellationToken = default);
    void Update(StockItem item);
    void Remove(StockItem item);
}

public record StockItemSearchRow(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal? ProductReorderPoint,
    decimal? ProductMinStock,
    string ProductCurrency,
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    Guid? LotId,
    string? LotNumber,
    DateTime? LotExpiryDate,
    string? BinLocation,
    decimal OnHand,
    decimal Reserved,
    decimal AvgCost,
    DateTime? LastMovementAtUtc);

public interface IStockMovementRepository
{
    Task AddAsync(StockMovement movement, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<StockMovement> Items, int Total)> SearchAsync(
        Guid? productId,
        Guid? warehouseId,
        StockMovementType? type,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockMovement>> GetBySourceAsync(StockSourceDocumentType type, Guid sourceId, CancellationToken cancellationToken = default);
}

public interface IStockCostLayerRepository
{
    // Open layers (RemainingQuantity > 0) for a stock item, oldest-first (FIFO consumption order).
    Task<IReadOnlyList<StockCostLayer>> GetOpenByStockItemAsync(Guid stockItemId, CancellationToken cancellationToken = default);
    Task<decimal> SumRemainingByStockItemAsync(Guid stockItemId, CancellationToken cancellationToken = default);
    Task AddAsync(StockCostLayer layer, CancellationToken cancellationToken = default);
    void Update(StockCostLayer layer);
    // Serializes FIFO consumption per (product, warehouse, lot) with a transaction-scoped advisory
    // lock so concurrent issues cannot double-consume the same physical units (StockItem's token
    // guards OnHand arithmetic, not the correctness of the layer-selection plan). No-op off Npgsql.
    Task AcquireItemLockAsync(Guid productId, Guid warehouseId, Guid? lotId, CancellationToken cancellationToken = default);
}

public interface ISerialUnitRepository
{
    Task<SerialUnit?> GetBySerialAsync(Guid productId, string serialNumber, CancellationToken cancellationToken = default);
    // Where-used lookup by serial string alone (tenant-scoped; a serial string may exist under more
    // than one product, so returns all matches).
    Task<IReadOnlyList<SerialUnit>> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SerialUnit>> GetBySerialNumbersAsync(Guid productId, IEnumerable<string> serialNumbers, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SerialUnit>> GetChildrenAsync(Guid parentSerialUnitId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetExistingSerialNumbersAsync(Guid productId, IEnumerable<string> serialNumbers, CancellationToken cancellationToken = default);
    Task AddAsync(SerialUnit unit, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<SerialUnit> units, CancellationToken cancellationToken = default);
    void Update(SerialUnit unit);
}

public interface IStockAllocationRepository
{
    Task<StockAllocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockAllocation>> GetByOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockAllocation>> GetActiveByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task AddAsync(StockAllocation allocation, CancellationToken cancellationToken = default);
    void Update(StockAllocation allocation);
    void Remove(StockAllocation allocation);
}

public interface IStockReasonCodeRepository
{
    Task<StockReasonCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StockReasonCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockReasonCode>> ListAsync(StockReasonCategory? category = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task AddAsync(StockReasonCode reason, CancellationToken cancellationToken = default);
    void Update(StockReasonCode reason);
    void Remove(StockReasonCode reason);
}

public interface IStockCountRepository
{
    Task<StockCount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StockCount?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CountNumberExistsAsync(string countNumber, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<StockCountSearchRow> Items, int Total)> SearchAsync(
        Guid? warehouseId,
        StockCountStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(StockCount stockCount, CancellationToken cancellationToken = default);
    void Update(StockCount stockCount);
    void Remove(StockCount stockCount);
}

/// <summary>Slim list row — header + totals computed via correlated SUM subqueries,
/// so the list never joins/materializes stock_count_lines (a warehouse-wide count
/// carries ~20k lines). Full lines load only in the detail (GetWithLinesAsync).</summary>
public record StockCountSearchRow(
    Guid Id,
    string CountNumber,
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    StockCountStatus Status,
    DateTime PlannedAtUtc,
    DateTime? CountingStartedAtUtc,
    DateTime? ReconciledAtUtc,
    DateTime? PostedAtUtc,
    Guid? PlannedByUserId,
    Guid? PostedByUserId,
    string? Notes,
    decimal TotalVarianceQuantity,
    decimal TotalVarianceCost,
    int LineCount,
    DateTime CreatedAtUtc);

public interface ILotRepository
{
    Task<Lot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Lot?> GetByProductAndNumberAsync(Guid productId, string lotNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lot>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task AddAsync(Lot lot, CancellationToken cancellationToken = default);
    void Update(Lot lot);
    void Remove(Lot lot);
}
