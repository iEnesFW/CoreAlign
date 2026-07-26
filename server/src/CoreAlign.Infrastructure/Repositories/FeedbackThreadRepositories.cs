using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class FeedbackCommentRepository : IFeedbackCommentRepository
{
    private readonly CoreAlignDbContext _context;
    public FeedbackCommentRepository(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<FeedbackTicketComment>> ListByTicketAsync(
        Guid ticketId,
        bool includeInternal,
        CancellationToken cancellationToken = default)
    {
        var query = _context
            .Set<FeedbackTicketComment>()
            .AsNoTracking()
            .Where(c => c.FeedbackTicketId == ticketId);
        if (!includeInternal) query = query.Where(c => !c.IsInternal);
        return await query.OrderBy(c => c.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FeedbackTicketComment comment, CancellationToken cancellationToken = default) =>
        await _context.Set<FeedbackTicketComment>().AddAsync(comment, cancellationToken);
}

public class FeedbackAttachmentRepository : IFeedbackAttachmentRepository
{
    private readonly CoreAlignDbContext _context;
    public FeedbackAttachmentRepository(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<FeedbackAttachment>> ListByTicketAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default) =>
        await _context
            .Set<FeedbackAttachment>()
            .AsNoTracking()
            .Where(a => a.FeedbackTicketId == ticketId)
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<FeedbackAttachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Set<FeedbackAttachment>().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<int> CountByTicketAsync(Guid ticketId, CancellationToken cancellationToken = default) =>
        _context.Set<FeedbackAttachment>().CountAsync(a => a.FeedbackTicketId == ticketId, cancellationToken);

    public async Task AddAsync(FeedbackAttachment attachment, CancellationToken cancellationToken = default) =>
        await _context.Set<FeedbackAttachment>().AddAsync(attachment, cancellationToken);

    public void Remove(FeedbackAttachment attachment) =>
        _context.Set<FeedbackAttachment>().Remove(attachment);
}
