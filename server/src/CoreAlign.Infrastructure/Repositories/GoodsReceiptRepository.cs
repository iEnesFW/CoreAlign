using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class GoodsReceiptRepository : IGoodsReceiptRepository
{
    private readonly CoreAlignDbContext _context;
    public GoodsReceiptRepository(CoreAlignDbContext context) => _context = context;

    public Task<GoodsReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GoodsReceipts
            .Include(g => g.Lines)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<GoodsReceipt?> GetByIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default) =>
        _context.GoodsReceipts
            .Include(g => g.Lines)
            .FirstOrDefaultAsync(g => g.IdempotencyKey == key, cancellationToken);

    public async Task<(IReadOnlyList<GoodsReceipt> Items, int Total)> SearchAsync(
        Guid? purchaseOrderId,
        Guid? vendorId,
        GoodsReceiptStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.GoodsReceipts.AsNoTracking().Include(g => g.Lines).AsQueryable();
        if (purchaseOrderId.HasValue) query = query.Where(g => g.PurchaseOrderId == purchaseOrderId.Value);
        if (vendorId.HasValue) query = query.Where(g => g.VendorId == vendorId.Value);
        if (status.HasValue) query = query.Where(g => g.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(g => g.ReceiptDateUtc)
            .ThenByDescending(g => g.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(GoodsReceipt goodsReceipt, CancellationToken cancellationToken = default) =>
        await _context.GoodsReceipts.AddAsync(goodsReceipt, cancellationToken);

    public void Update(GoodsReceipt goodsReceipt) => _context.GoodsReceipts.Update(goodsReceipt);
}
