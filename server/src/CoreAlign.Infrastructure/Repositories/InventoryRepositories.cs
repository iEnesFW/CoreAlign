using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class StockItemRepository : IStockItemRepository
{
    private readonly CoreAlignDbContext _context;
    public StockItemRepository(CoreAlignDbContext context) => _context = context;

    public Task<StockItem?> GetAsync(Guid productId, Guid warehouseId, Guid? lotId, CancellationToken ct = default) =>
        _context.StockItems.FirstOrDefaultAsync(s =>
            s.ProductId == productId && s.WarehouseId == warehouseId && s.LotId == lotId, ct);

    public Task<StockItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.StockItems.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<StockItem>> GetByProductAsync(Guid productId, CancellationToken ct = default) =>
        await _context.StockItems
            .Include(s => s.Warehouse)
            .Include(s => s.Lot)
            .AsNoTracking()
            .Where(s => s.ProductId == productId)
            .OrderBy(s => s.Warehouse.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StockItem>> GetByWarehouseAsync(Guid warehouseId, CancellationToken ct = default) =>
        await _context.StockItems
            .Include(s => s.Product)
            .AsNoTracking()
            .Where(s => s.WarehouseId == warehouseId)
            .OrderBy(s => s.Product.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StockItem>> SearchAsync(
        Guid? productId, Guid? warehouseId, bool onlyBelowReorder, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.StockItems
            .Include(s => s.Product)
            .Include(s => s.Warehouse)
            .Include(s => s.Lot)
            .AsNoTracking();
        if (productId.HasValue) query = query.Where(s => s.ProductId == productId.Value);
        if (warehouseId.HasValue) query = query.Where(s => s.WarehouseId == warehouseId.Value);
        if (onlyBelowReorder) query = query.Where(s => s.OnHand - s.Reserved <= s.Product.ReorderPoint);
        return await query
            .OrderByDescending(s => s.LastMovementAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public Task<int> CountAsync(Guid? productId, Guid? warehouseId, bool onlyBelowReorder, CancellationToken ct = default)
    {
        var query = _context.StockItems.AsNoTracking();
        if (productId.HasValue) query = query.Where(s => s.ProductId == productId.Value);
        if (warehouseId.HasValue) query = query.Where(s => s.WarehouseId == warehouseId.Value);
        if (onlyBelowReorder) query = query.Where(s => s.OnHand - s.Reserved <= s.Product.ReorderPoint);
        return query.CountAsync(ct);
    }

    public async Task<decimal> SumOnHandAsync(Guid productId, CancellationToken ct = default) =>
        await _context.StockItems.Where(s => s.ProductId == productId).SumAsync(s => (decimal?)s.OnHand, ct) ?? 0m;

    public async Task<decimal> SumReservedAsync(Guid productId, CancellationToken ct = default) =>
        await _context.StockItems.Where(s => s.ProductId == productId).SumAsync(s => (decimal?)s.Reserved, ct) ?? 0m;

    public async Task<StockItem> GetOrCreateAsync(Guid productId, Guid warehouseId, Guid? lotId, CancellationToken ct = default)
    {
        var existing = await GetAsync(productId, warehouseId, lotId, ct);
        if (existing is not null) return existing;
        var created = new StockItem(productId, warehouseId, lotId);
        await _context.StockItems.AddAsync(created, ct);
        return created;
    }

    public async Task AddAsync(StockItem item, CancellationToken ct = default) =>
        await _context.StockItems.AddAsync(item, ct);
    public void Update(StockItem item) => _context.StockItems.Update(item);
    public void Remove(StockItem item) => _context.StockItems.Remove(item);
}

public class StockMovementRepository : IStockMovementRepository
{
    private readonly CoreAlignDbContext _context;
    public StockMovementRepository(CoreAlignDbContext context) => _context = context;

    public async Task AddAsync(StockMovement movement, CancellationToken ct = default) =>
        await _context.StockMovements.AddAsync(movement, ct);

    public async Task<(IReadOnlyList<StockMovement> Items, int Total)> SearchAsync(
        Guid? productId, Guid? warehouseId, StockMovementType? type, DateTime? fromUtc, DateTime? toUtc,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.StockMovements
            .Include(m => m.Product)
            .Include(m => m.Warehouse)
            .Include(m => m.Lot)
            .Include(m => m.ReasonCode)
            .AsNoTracking();
        if (productId.HasValue) query = query.Where(m => m.ProductId == productId.Value);
        if (warehouseId.HasValue) query = query.Where(m => m.WarehouseId == warehouseId.Value);
        if (type.HasValue) query = query.Where(m => m.Type == type.Value);
        if (fromUtc.HasValue) query = query.Where(m => m.OccurredAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(m => m.OccurredAtUtc <= toUtc.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(m => m.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyList<StockMovement>> GetBySourceAsync(StockSourceDocumentType type, Guid sourceId, CancellationToken ct = default) =>
        await _context.StockMovements.AsNoTracking()
            .Where(m => m.SourceDocumentType == type && m.SourceDocumentId == sourceId)
            .OrderByDescending(m => m.OccurredAtUtc)
            .ToListAsync(ct);
}

public class StockAllocationRepository : IStockAllocationRepository
{
    private readonly CoreAlignDbContext _context;
    public StockAllocationRepository(CoreAlignDbContext context) => _context = context;

    public Task<StockAllocation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.StockAllocations.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<StockAllocation>> GetByOrderAsync(Guid orderId, CancellationToken ct = default) =>
        await _context.StockAllocations
            .Include(a => a.Product)
            .Include(a => a.Warehouse)
            .Include(a => a.Lot)
            .Where(a => a.OrderId == orderId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StockAllocation>> GetActiveByProductAsync(Guid productId, CancellationToken ct = default) =>
        await _context.StockAllocations
            .Where(a => a.ProductId == productId && (a.Status == AllocationStatus.Active || a.Status == AllocationStatus.PartiallyConsumed))
            .ToListAsync(ct);

    public async Task AddAsync(StockAllocation allocation, CancellationToken ct = default) =>
        await _context.StockAllocations.AddAsync(allocation, ct);
    public void Update(StockAllocation allocation) => _context.StockAllocations.Update(allocation);
    public void Remove(StockAllocation allocation) => _context.StockAllocations.Remove(allocation);
}

public class StockReasonCodeRepository : IStockReasonCodeRepository
{
    private readonly CoreAlignDbContext _context;
    public StockReasonCodeRepository(CoreAlignDbContext context) => _context = context;

    public Task<StockReasonCode?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.StockReasonCodes.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<StockReasonCode?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _context.StockReasonCodes.FirstOrDefaultAsync(r => r.Code == code, ct);

    public async Task<IReadOnlyList<StockReasonCode>> ListAsync(StockReasonCategory? category = null, bool? isActive = null, CancellationToken ct = default)
    {
        var query = _context.StockReasonCodes.AsNoTracking();
        if (category.HasValue) query = query.Where(r => r.Category == category.Value);
        if (isActive.HasValue) query = query.Where(r => r.IsActive == isActive.Value);
        return await query.OrderBy(r => r.Name).ToListAsync(ct);
    }

    public async Task AddAsync(StockReasonCode reason, CancellationToken ct = default) =>
        await _context.StockReasonCodes.AddAsync(reason, ct);
    public void Update(StockReasonCode reason) => _context.StockReasonCodes.Update(reason);
    public void Remove(StockReasonCode reason) => _context.StockReasonCodes.Remove(reason);
}

public class LotRepository : ILotRepository
{
    private readonly CoreAlignDbContext _context;
    public LotRepository(CoreAlignDbContext context) => _context = context;

    public Task<Lot?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Lots.FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task<Lot?> GetByProductAndNumberAsync(Guid productId, string lotNumber, CancellationToken ct = default) =>
        _context.Lots.FirstOrDefaultAsync(l => l.ProductId == productId && l.LotNumber == lotNumber, ct);

    public async Task<IReadOnlyList<Lot>> GetByProductAsync(Guid productId, CancellationToken ct = default) =>
        await _context.Lots.AsNoTracking()
            .Where(l => l.ProductId == productId)
            .OrderBy(l => l.ExpiryDate ?? DateTime.MaxValue)
            .ToListAsync(ct);

    public async Task AddAsync(Lot lot, CancellationToken ct = default) =>
        await _context.Lots.AddAsync(lot, ct);
    public void Update(Lot lot) => _context.Lots.Update(lot);
    public void Remove(Lot lot) => _context.Lots.Remove(lot);
}
