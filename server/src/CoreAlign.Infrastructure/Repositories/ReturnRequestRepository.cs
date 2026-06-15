using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class ReturnRequestRepository : IReturnRequestRepository
{
    private readonly CoreAlignDbContext _context;

    public ReturnRequestRepository(CoreAlignDbContext context) => _context = context;

    public Task<ReturnRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.ReturnRequests
            .Include(r => r.Order)
            .Include(r => r.Customer)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<ReturnRequest?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.ReturnRequests
            .Include(r => r.Order)
            .Include(r => r.Customer)
            .Include(r => r.Lines)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<bool> ReturnNumberExistsAsync(string returnNumber, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var query = _context.ReturnRequests.Where(r => r.ReturnNumber == returnNumber);
        if (excludeId.HasValue) query = query.Where(r => r.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<ReturnRequestSearchRow> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        Guid? orderId,
        ReturnRequestStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReturnRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = $"%{search.Trim().ToLower()}%";
            query = _context.Database.IsNpgsql()
                ? query.Where(r =>
                    EF.Functions.ILike(r.ReturnNumber, lower) ||
                    EF.Functions.ILike(r.CustomerNameSnapshot, lower))
                : query.Where(r =>
                    EF.Functions.Like(r.ReturnNumber.ToLower(), lower) ||
                    EF.Functions.Like(r.CustomerNameSnapshot.ToLower(), lower));
        }
        if (customerId.HasValue) query = query.Where(r => r.CustomerId == customerId.Value);
        if (orderId.HasValue) query = query.Where(r => r.OrderId == orderId.Value);
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.RequestedAtUtc)
            .ThenBy(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReturnRequestSearchRow(
                r.Id,
                r.ReturnNumber,
                r.Status,
                r.Reason.ToString(),
                r.OrderId,
                r.Order != null ? r.Order.OrderNumber : string.Empty,
                r.CustomerId,
                r.Customer != null ? r.Customer.Name : r.CustomerNameSnapshot,
                r.Currency,
                r.Lines.Sum(l => l.LineTotal),
                r.RequestedAtUtc,
                r.ReceivedAtUtc,
                r.CreditNoteId))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<ReturnRequest>> GetByOrderAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        await _context.ReturnRequests
            .AsNoTracking()
            .Include(r => r.Lines)
            .Where(r => r.OrderId == orderId)
            .OrderByDescending(r => r.RequestedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ReturnRequest entity, CancellationToken cancellationToken = default) =>
        await _context.ReturnRequests.AddAsync(entity, cancellationToken);

    public void Update(ReturnRequest entity) => _context.ReturnRequests.Update(entity);
}
