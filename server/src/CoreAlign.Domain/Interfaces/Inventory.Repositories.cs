using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IStockItemRepository
{
    Task<StockItem?> GetAsync(Guid productId, Guid warehouseId, Guid? lotId, CancellationToken cancellationToken = default);
    Task<StockItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockItem>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockItem>> GetByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);
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

public interface ILotRepository
{
    Task<Lot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Lot?> GetByProductAndNumberAsync(Guid productId, string lotNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lot>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task AddAsync(Lot lot, CancellationToken cancellationToken = default);
    void Update(Lot lot);
    void Remove(Lot lot);
}
