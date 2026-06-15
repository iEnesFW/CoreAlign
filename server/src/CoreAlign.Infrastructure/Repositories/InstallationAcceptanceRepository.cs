using CoreAlign.Domain.Entities.Installation;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class InstallationAcceptanceRepository : IInstallationAcceptanceRepository
{
    private readonly CoreAlignDbContext _context;
    public InstallationAcceptanceRepository(CoreAlignDbContext context) => _context = context;

    public Task<InstallationAcceptance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.InstallationAcceptances.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<InstallationAcceptance?> GetByWorkOrderIdAsync(Guid workOrderId, CancellationToken cancellationToken = default) =>
        _context.InstallationAcceptances.FirstOrDefaultAsync(a => a.WorkOrderId == workOrderId, cancellationToken);

    public Task<InstallationAcceptance?> GetByAcceptIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Task.FromResult<InstallationAcceptance?>(null);
        }
        var key = idempotencyKey.Trim();
        return _context.InstallationAcceptances.FirstOrDefaultAsync(a => a.AcceptIdempotencyKey == key, cancellationToken);
    }

    public async Task<IReadOnlyList<InstallationAcceptance>> ListByInspectorAsync(
        Guid inspectorUserId,
        InstallationAcceptanceStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InstallationAcceptances.AsNoTracking()
            .Where(a => a.InspectorUserId == inspectorUserId);
        if (status.HasValue) query = query.Where(a => a.Status == status.Value);
        return await query.OrderByDescending(a => a.StartedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InstallationAcceptance>> ListPendingAsync(CancellationToken cancellationToken = default) =>
        await _context.InstallationAcceptances
            .AsNoTracking()
            .Where(a => a.Status == InstallationAcceptanceStatus.Draft
                     || a.Status == InstallationAcceptanceStatus.InProgress
                     || a.Status == InstallationAcceptanceStatus.SignedByCustomer)
            .OrderBy(a => a.StartedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(InstallationAcceptance entity, CancellationToken cancellationToken = default) =>
        await _context.InstallationAcceptances.AddAsync(entity, cancellationToken);

    public void Update(InstallationAcceptance entity) => _context.InstallationAcceptances.Update(entity);
}

public class PunchListRepository : IPunchListRepository
{
    private readonly CoreAlignDbContext _context;
    public PunchListRepository(CoreAlignDbContext context) => _context = context;

    public Task<PunchListItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.PunchListItems.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PunchListItem>> ListByAcceptanceAsync(Guid acceptanceId, CancellationToken cancellationToken = default) =>
        await _context.PunchListItems
            .AsNoTracking()
            .Where(p => p.AcceptanceId == acceptanceId)
            .OrderBy(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PunchListItem>> ListByStatusAsync(PunchListItemStatus status, CancellationToken cancellationToken = default) =>
        await _context.PunchListItems
            .AsNoTracking()
            .Where(p => p.Status == status)
            .OrderBy(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PunchListItem entity, CancellationToken cancellationToken = default) =>
        await _context.PunchListItems.AddAsync(entity, cancellationToken);

    public void Update(PunchListItem entity) => _context.PunchListItems.Update(entity);
}
