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

    public async Task<IReadOnlyList<ActivityLog>> StreamAsync(ActivityLogQueryFilter filter, int max, CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_context.ActivityLogs.AsNoTracking(), filter);
        return await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(max)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<ActivityLog> Items, int Total)> SearchAsync(ActivityLogQueryFilter filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_context.ActivityLogs.AsNoTracking(), filter);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    private static IQueryable<ActivityLog> ApplyFilter(IQueryable<ActivityLog> source, ActivityLogQueryFilter filter)
    {
        if (filter.UserId.HasValue) source = source.Where(l => l.UserId == filter.UserId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Method)) source = source.Where(l => l.Method == filter.Method);
        if (!string.IsNullOrWhiteSpace(filter.PathContains)) source = source.Where(l => l.Path.Contains(filter.PathContains));
        if (filter.StatusCode.HasValue) source = source.Where(l => l.StatusCode == filter.StatusCode.Value);
        if (filter.FromUtc.HasValue) source = source.Where(l => l.CreatedAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue) source = source.Where(l => l.CreatedAtUtc <= filter.ToUtc.Value);
        return source;
    }
}
