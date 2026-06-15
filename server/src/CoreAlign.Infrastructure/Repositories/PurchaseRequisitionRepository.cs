using CoreAlign.Domain.Entities.Purchasing;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class PurchaseRequisitionRepository : IPurchaseRequisitionRepository
{
    private readonly CoreAlignDbContext _context;
    public PurchaseRequisitionRepository(CoreAlignDbContext context) => _context = context;

    public Task<PurchaseRequisition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.PurchaseRequisitions
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> NumberExistsAsync(string number, Guid? excludeId, CancellationToken cancellationToken = default) =>
        _context.PurchaseRequisitions.AnyAsync(
            p => p.Number == number && (excludeId == null || p.Id != excludeId), cancellationToken);

    public async Task<(IReadOnlyList<PurchaseRequisition> Items, int Total)> SearchAsync(
        PurchaseRequisitionStatus? status,
        Guid? productId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseRequisitions
            .AsNoTracking()
            .Include(p => p.Lines)
            .AsQueryable();

        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        if (productId.HasValue) query = query.Where(p => p.Lines.Any(l => l.ProductId == productId.Value));
        if (fromUtc.HasValue) query = query.Where(p => p.RequestedAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(p => p.RequestedAtUtc <= toUtc.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.RequestedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(PurchaseRequisition requisition, CancellationToken cancellationToken = default) =>
        await _context.PurchaseRequisitions.AddAsync(requisition, cancellationToken);

    public void Update(PurchaseRequisition requisition) => _context.PurchaseRequisitions.Update(requisition);
    public void Remove(PurchaseRequisition requisition) => _context.PurchaseRequisitions.Remove(requisition);
}
