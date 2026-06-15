using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class GlassWorkOrderRevisionRepository : IGlassWorkOrderRevisionRepository
{
    private readonly CoreAlignDbContext _context;

    public GlassWorkOrderRevisionRepository(CoreAlignDbContext context) => _context = context;

    public Task<GlassWorkOrderRevision?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassWorkOrderRevisions.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<GlassWorkOrderRevision>> ListByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default) =>
        await _context.GlassWorkOrderRevisions.AsNoTracking()
            .Where(r => r.WorkOrderId == workOrderId)
            .OrderBy(r => r.RevisionNumber)
            .ToListAsync(cancellationToken);

    public async Task<int> GetMaxRevisionNumberAsync(Guid workOrderId, CancellationToken cancellationToken = default) =>
        await _context.GlassWorkOrderRevisions.AsNoTracking()
            .Where(r => r.WorkOrderId == workOrderId)
            .Select(r => (int?)r.RevisionNumber)
            .MaxAsync(cancellationToken) ?? 0;

    public async Task<decimal> GetCumulativeSignedDeltaSinceLastApprovalAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        var lastApprovedNumber = await _context.GlassWorkOrderRevisions.AsNoTracking()
            .Where(r => r.WorkOrderId == workOrderId && r.Status == WorkOrderRevisionStatus.Approved)
            .OrderByDescending(r => r.RevisionNumber)
            .Select(r => (int?)r.RevisionNumber)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;

        return await _context.GlassWorkOrderRevisions.AsNoTracking()
            .Where(r => r.WorkOrderId == workOrderId
                        && r.RevisionNumber > lastApprovedNumber
                        && r.Status == WorkOrderRevisionStatus.SilentSnapshot)
            .SumAsync(r => r.DeltaPercent, cancellationToken);
    }

    public Task<bool> AnyOutstandingBlockingAsync(Guid workOrderId, Guid excludeRevisionId, CancellationToken cancellationToken = default) =>
        _context.GlassWorkOrderRevisions.AsNoTracking()
            .AnyAsync(r => r.WorkOrderId == workOrderId
                           && r.Id != excludeRevisionId
                           && (r.Status == WorkOrderRevisionStatus.PendingApproval || r.Status == WorkOrderRevisionStatus.Blocked),
                cancellationToken);

    public Task<GlassWorkOrderRevision?> GetLatestAsync(Guid workOrderId, CancellationToken cancellationToken = default) =>
        _context.GlassWorkOrderRevisions.AsNoTracking()
            .Where(r => r.WorkOrderId == workOrderId)
            .OrderByDescending(r => r.RevisionNumber)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, GlassWorkOrderRevision>> GetLatestByWorkOrderIdsAsync(IEnumerable<Guid> workOrderIds, CancellationToken cancellationToken = default)
    {
        var ids = workOrderIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, GlassWorkOrderRevision>();
        }

        var rows = await _context.GlassWorkOrderRevisions.AsNoTracking()
            .Where(r => ids.Contains(r.WorkOrderId))
            .GroupBy(r => r.WorkOrderId)
            .Select(g => g
                .OrderByDescending(r => r.RevisionNumber)
                .First())
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.WorkOrderId);
    }

    public async Task AddAsync(GlassWorkOrderRevision revision, CancellationToken cancellationToken = default) =>
        await _context.GlassWorkOrderRevisions.AddAsync(revision, cancellationToken);

    public void Update(GlassWorkOrderRevision revision) => _context.GlassWorkOrderRevisions.Update(revision);
}
