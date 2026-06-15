using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly CoreAlignDbContext _context;
    public PurchaseOrderRepository(CoreAlignDbContext context) => _context = context;

    public Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> PoNumberExistsAsync(string poNumber, Guid? excludeId, CancellationToken cancellationToken = default) =>
        _context.PurchaseOrders.AnyAsync(
            p => p.PoNumber == poNumber && (excludeId == null || p.Id != excludeId), cancellationToken);

    public async Task<(IReadOnlyList<PurchaseOrder> Items, int Total)> SearchAsync(
        Guid? vendorId,
        PurchaseOrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders.AsNoTracking().Include(p => p.Lines).AsQueryable();
        if (vendorId.HasValue) query = query.Where(p => p.VendorId == vendorId.Value);
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.OrderDate)
            .ThenByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default) =>
        await _context.PurchaseOrders.AddAsync(purchaseOrder, cancellationToken);

    public void Update(PurchaseOrder purchaseOrder) => _context.PurchaseOrders.Update(purchaseOrder);
    public void Remove(PurchaseOrder purchaseOrder) => _context.PurchaseOrders.Remove(purchaseOrder);
}
