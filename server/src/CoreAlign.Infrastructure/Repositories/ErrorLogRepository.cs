using CoreAlign.Domain.Entities.Observability;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class ErrorLogRepository : IErrorLogRepository
{
    private readonly CoreAlignDbContext _context;
    public ErrorLogRepository(CoreAlignDbContext context) => _context = context;

    public async Task AddAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default) =>
        await _context.ErrorLogs.AddAsync(entry, cancellationToken);

    public Task<ErrorLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.ErrorLogs.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<ErrorLogEntry> Items, int Total)> QueryAsync(ErrorLogQuery query, CancellationToken cancellationToken = default)
    {
        var q = _context.ErrorLogs.AsNoTracking().AsQueryable();

        if (query.TenantId.HasValue) q = q.Where(e => e.TenantId == query.TenantId);
        if (query.Severity.HasValue) q = q.Where(e => e.Severity == query.Severity);
        if (query.Source.HasValue) q = q.Where(e => e.Source == query.Source);
        if (query.StatusCode.HasValue) q = q.Where(e => e.StatusCode == query.StatusCode);
        if (!string.IsNullOrWhiteSpace(query.CorrelationId)) q = q.Where(e => e.CorrelationId == query.CorrelationId);
        if (!string.IsNullOrWhiteSpace(query.PathContains)) q = q.Where(e => e.Path != null && e.Path.Contains(query.PathContains));
        if (query.UserId.HasValue) q = q.Where(e => e.UserId == query.UserId);
        if (query.OnlyUnresolved == true) q = q.Where(e => !e.IsResolved);
        if (query.FromUtc.HasValue) q = q.Where(e => e.OccurredAtUtc >= query.FromUtc);
        if (query.ToUtc.HasValue) q = q.Where(e => e.OccurredAtUtc <= query.ToUtc);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search;
            q = q.Where(e => e.Message.Contains(term)
                || (e.ExceptionType != null && e.ExceptionType.Contains(term))
                || (e.ClientPage != null && e.ClientPage.Contains(term)));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(e => e.OccurredAtUtc)
            .Skip(query.Skip)
            .Take(query.Take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<int> DeleteOlderThanAsync(DateTime thresholdUtc, CancellationToken cancellationToken = default) =>
        _context.ErrorLogs
            .Where(e => e.OccurredAtUtc < thresholdUtc && e.IsResolved)
            .ExecuteDeleteAsync(cancellationToken);
}
