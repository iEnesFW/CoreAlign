using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class ActivityLogRepository : IActivityLogRepository
{
    private readonly CoreAlignDbContext _context;

    public ActivityLogRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ActivityLog log, CancellationToken cancellationToken = default)
    {
        await _context.ActivityLogs.AddAsync(log, cancellationToken);
    }

    public async Task<(IReadOnlyList<ActivityLog> Items, int Total)> GetRecentAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.ActivityLogs.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
