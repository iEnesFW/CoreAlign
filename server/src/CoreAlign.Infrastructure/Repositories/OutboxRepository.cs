using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly CoreAlignDbContext _context;
    public OutboxRepository(CoreAlignDbContext context) => _context = context;

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default) =>
        await _context.OutboxMessages.AddAsync(message, cancellationToken);

    public void Update(OutboxMessage message) => _context.OutboxMessages.Update(message);

    public Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.OutboxMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int max, CancellationToken cancellationToken = default) =>
        await _context.OutboxMessages
            .Where(m => m.Status == OutboxStatus.Pending)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(max)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OutboxMessage>> ListAsync(OutboxStatus? status, int max, CancellationToken cancellationToken = default)
    {
        var query = _context.OutboxMessages.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(m => m.Status == status.Value);
        return await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(max)
            .ToListAsync(cancellationToken);
    }
}
