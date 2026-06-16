using CoreAlign.Application.Compliance.Audit;
using CoreAlign.Application.Platform.Tenants;
using CoreAlign.Application.Treasury.Fx;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Compliance;
using CoreAlign.Domain.Entities.Treasury;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public sealed class PlatformTenantRepository : IPlatformTenantRepository
{
    private readonly CoreAlignDbContext _context;
    public PlatformTenantRepository(CoreAlignDbContext context) => _context = context;

    public async Task<(IReadOnlyList<Tenant> Items, int Total)> SearchAsync(string? search, int page, int pageSize, bool includeArchived, CancellationToken ct)
    {
        var query = _context.Tenants.AsNoTracking().AsQueryable();
        if (!includeArchived)
        {
            query = query.Where(t => !t.IsArchived);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(t => EF.Functions.ILike(t.Name, pattern) || EF.Functions.ILike(t.Slug, pattern));
        }
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _context.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<bool> SlugExistsAsync(string slug, Guid excludingId, CancellationToken ct) =>
        _context.Tenants.AnyAsync(t => t.Slug == slug && t.Id != excludingId, ct);
}

public sealed class ExchangeRateRepository : IExchangeRateRepository
{
    private readonly CoreAlignDbContext _context;
    public ExchangeRateRepository(CoreAlignDbContext context) => _context = context;

    public Task<ExchangeRate?> GetAsync(string currency, DateTime validOnDate, CancellationToken ct) =>
        _context.ExchangeRates
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == Guid.Empty)
            .FirstOrDefaultAsync(r => r.Currency == currency && r.ValidOnDate == validOnDate, ct);

    public async Task<IReadOnlyList<ExchangeRate>> ListAsync(DateTime? from, DateTime? to, string? currency, CancellationToken ct)
    {
        var query = _context.ExchangeRates.AsNoTracking().IgnoreQueryFilters().Where(r => r.TenantId == Guid.Empty);
        if (from.HasValue) query = query.Where(r => r.ValidOnDate >= from.Value);
        if (to.HasValue) query = query.Where(r => r.ValidOnDate <= to.Value);
        if (!string.IsNullOrWhiteSpace(currency)) query = query.Where(r => r.Currency == currency);
        return await query.OrderByDescending(r => r.ValidOnDate).ThenBy(r => r.Currency).Take(500).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ExchangeRate>> GetLatestPerCurrencyOnOrBeforeAsync(DateTime asOf, CancellationToken ct)
    {
        var rows = await _context.ExchangeRates
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == Guid.Empty && r.ValidOnDate <= asOf)
            .ToListAsync(ct);
        return rows
            .GroupBy(r => r.Currency, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(r => r.ValidOnDate).First())
            .ToArray();
    }

    public Task AddAsync(ExchangeRate rate, CancellationToken ct) =>
        _context.ExchangeRates.AddAsync(rate, ct).AsTask();

    public void Update(ExchangeRate rate) => _context.ExchangeRates.Update(rate);
}

public sealed class EntityAuditLogRepository : IEntityAuditLogRepository
{
    private readonly CoreAlignDbContext _context;
    public EntityAuditLogRepository(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<EntityAuditLog>> GetTimelineAsync(string entityType, Guid entityId, CancellationToken ct)
    {
        return await _context.EntityAuditLogs
            .AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.ChangedAtUtc)
            .Take(500)
            .ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<EntityAuditLog> Items, int Total)> SearchAsync(
        Guid tenantId,
        IReadOnlyList<string>? entityTypes,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = _context.EntityAuditLogs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId);

        if (entityTypes is { Count: > 0 })
        {
            var allowedTypes = entityTypes.ToArray();
            query = query.Where(a => allowedTypes.Contains(a.EntityType));
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(a => a.ChangedAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(a => a.ChangedAtUtc <= toUtc.Value);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.ChangedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<(IReadOnlyList<EntityAuditLog> Items, int Total)> SearchAdvancedAsync(
        Guid tenantId,
        AuditLogSearchCriteria criteria,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = BuildAdvancedQuery(tenantId, criteria);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.ChangedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async IAsyncEnumerable<EntityAuditLog> StreamAsync(
        Guid tenantId,
        AuditLogSearchCriteria criteria,
        int batchSize,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        if (batchSize <= 0) batchSize = 500;

        // Keyset (seek) paging on (ChangedAtUtc, Sequence) — each batch reads only
        // batchSize rows from the last cursor position (index ix_entity_audit_logs_
        // tenant_id_changed_at), so a multi-million-row export is O(n), not the
        // O(n^2) that OFFSET/Skip incurs (batch k scanning+discarding k*batchSize rows).
        DateTime? cursorTs = null;
        long cursorSeq = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var query = BuildAdvancedQuery(tenantId, criteria);
            if (cursorTs.HasValue)
            {
                var ts = cursorTs.Value;
                var seq = cursorSeq;
                query = query.Where(a => a.ChangedAtUtc > ts || (a.ChangedAtUtc == ts && a.Sequence > seq));
            }

            var batch = await query
                .OrderBy(a => a.ChangedAtUtc)
                .ThenBy(a => a.Sequence)
                .Take(batchSize)
                .ToListAsync(ct);

            if (batch.Count == 0) yield break;
            foreach (var row in batch)
            {
                yield return row;
            }
            if (batch.Count < batchSize) yield break;

            var last = batch[^1];
            cursorTs = last.ChangedAtUtc;
            cursorSeq = last.Sequence;
        }
    }

    private IQueryable<EntityAuditLog> BuildAdvancedQuery(Guid tenantId, AuditLogSearchCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        var query = _context.EntityAuditLogs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId);

        if (criteria.EntityTypes is { Count: > 0 })
        {
            var allowedTypes = criteria.EntityTypes.ToArray();
            query = query.Where(a => allowedTypes.Contains(a.EntityType));
        }

        if (criteria.Actions is { Count: > 0 })
        {
            var allowedActions = criteria.Actions.ToArray();
            query = query.Where(a => allowedActions.Contains(a.Action));
        }

        if (criteria.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == criteria.UserId.Value);
        }

        if (criteria.EntityId.HasValue)
        {
            query = query.Where(a => a.EntityId == criteria.EntityId.Value);
        }

        if (criteria.FromUtc.HasValue)
        {
            query = query.Where(a => a.ChangedAtUtc >= criteria.FromUtc.Value);
        }

        if (criteria.ToUtc.HasValue)
        {
            query = query.Where(a => a.ChangedAtUtc <= criteria.ToUtc.Value);
        }

        return query;
    }
}
