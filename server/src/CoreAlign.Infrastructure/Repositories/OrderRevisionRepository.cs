using CoreAlign.Domain.Entities.Sales;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class OrderRevisionRepository : IOrderRevisionRepository
{
    private readonly CoreAlignDbContext _context;

    public OrderRevisionRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<OrderRevision?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.OrderRevisions.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<OrderRevision>> GetByOrderAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        await _context.OrderRevisions
            .Where(r => r.OrderId == orderId)
            .OrderByDescending(r => r.RevisionNumber)
            .ToListAsync(cancellationToken);

    public Task<OrderRevision?> GetPendingForOrderAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        _context.OrderRevisions
            .FirstOrDefaultAsync(r => r.OrderId == orderId && r.Status == RevisionStatus.Proposed, cancellationToken);

    public async Task AddAsync(OrderRevision revision, CancellationToken cancellationToken = default)
    {
        await _context.OrderRevisions.AddAsync(revision, cancellationToken);
    }

    public void Update(OrderRevision revision)
    {
        _context.OrderRevisions.Update(revision);
    }
}
