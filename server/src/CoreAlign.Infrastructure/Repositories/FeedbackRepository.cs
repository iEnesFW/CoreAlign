using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class FeedbackRepository : IFeedbackRepository
{
    private readonly CoreAlignDbContext _context;
    public FeedbackRepository(CoreAlignDbContext context) => _context = context;

    public Task<FeedbackTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.FeedbackTickets.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<IReadOnlyList<FeedbackTicket>> ListAsync(FeedbackStatus? status, FeedbackType? type, CancellationToken cancellationToken = default)
    {
        var query = _context.FeedbackTickets.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(f => f.Status == status.Value);
        if (type.HasValue) query = query.Where(f => f.Type == type.Value);
        return await query.OrderByDescending(f => f.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FeedbackTicket ticket, CancellationToken cancellationToken = default) =>
        await _context.FeedbackTickets.AddAsync(ticket, cancellationToken);

    public void Update(FeedbackTicket ticket) => _context.FeedbackTickets.Update(ticket);
}
