using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class PasswordHistoryRepository : IPasswordHistoryRepository
{
    private readonly CoreAlignDbContext _context;

    public PasswordHistoryRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PasswordHistory>> ListRecentByUserAsync(Guid userId, int take, CancellationToken cancellationToken = default)
    {
        return await _context.PasswordHistories
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PasswordHistory entry, CancellationToken cancellationToken = default)
    {
        await _context.PasswordHistories.AddAsync(entry, cancellationToken);
    }

    public async Task RemoveOlderThanAsync(Guid userId, int keep, CancellationToken cancellationToken = default)
    {
        var idsToKeep = await _context.PasswordHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAtUtc)
            .Take(keep)
            .Select(h => h.Id)
            .ToListAsync(cancellationToken);

        await _context.PasswordHistories
            .Where(h => h.UserId == userId && !idsToKeep.Contains(h.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
