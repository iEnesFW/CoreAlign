using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CoreAlign.Infrastructure.Repositories;

public class TenantProviderConfigRepository : ITenantProviderConfigRepository
{
    private readonly CoreAlignDbContext _context;

    public TenantProviderConfigRepository(CoreAlignDbContext context) => _context = context;

    public Task<TenantProviderConfig?> GetByTenantAndCategoryAsync(
        Guid tenantId,
        ProviderCategory category,
        string providerName,
        CancellationToken cancellationToken = default)
        => _context.TenantProviderConfigs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                c => c.TenantId == tenantId && c.Category == category && c.ProviderName == providerName,
                cancellationToken);

    public Task<TenantProviderConfig?> GetDefaultForTenantAsync(
        Guid tenantId,
        ProviderCategory category,
        CancellationToken cancellationToken = default)
        => _context.TenantProviderConfigs
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId && c.Category == category && c.IsDefault && c.IsEnabled)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<TenantProviderConfig>> ListByTenantAsync(
        Guid tenantId,
        ProviderCategory? category,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TenantProviderConfigs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId);

        if (category.HasValue)
        {
            query = query.Where(c => c.Category == category.Value);
        }

        return await query
            .OrderBy(c => c.Category)
            .ThenByDescending(c => c.IsDefault)
            .ThenBy(c => c.ProviderName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TenantProviderConfig config, CancellationToken cancellationToken = default)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        await _context.TenantProviderConfigs.AddAsync(config, cancellationToken);
    }

    public void Update(TenantProviderConfig config) => _context.TenantProviderConfigs.Update(config);

    public void Remove(TenantProviderConfig config) => _context.TenantProviderConfigs.Remove(config);
}

public class ProviderWebhookInboxRepository : IProviderWebhookInboxRepository
{
    private readonly CoreAlignDbContext _context;

    public ProviderWebhookInboxRepository(CoreAlignDbContext context) => _context = context;

    public Task<bool> ExistsBySignatureAsync(string signatureHash, CancellationToken cancellationToken = default)
        => _context.ProviderWebhookInbox
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(w => w.SignatureHash == signatureHash, cancellationToken);

    public async Task AddAsync(ProviderWebhookInbox entry, CancellationToken cancellationToken = default)
    {
        if (entry is null) throw new ArgumentNullException(nameof(entry));
        await _context.ProviderWebhookInbox.AddAsync(entry, cancellationToken);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _context.Entry(entry).State = EntityState.Detached;
        }
    }

    public Task<ProviderWebhookInbox?> GetBySignatureAsync(string signatureHash, CancellationToken cancellationToken = default)
        => _context.ProviderWebhookInbox
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.SignatureHash == signatureHash, cancellationToken);

    public Task<ProviderWebhookInbox?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.ProviderWebhookInbox
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<ProviderWebhookInbox> Items, int Total)> ListAsync(
        Guid tenantId,
        string? providerName,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProviderWebhookInbox
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(w => w.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(providerName))
        {
            query = query.Where(w => w.ProviderName == providerName);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(w => w.ReceivedAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(w => w.ReceivedAtUtc <= toUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = status.Trim().ToLowerInvariant() switch
            {
                "pending" => query.Where(w => w.ProcessedAtUtc == null && w.ProcessingError == null),
                "processed" => query.Where(w => w.ProcessedAtUtc != null && w.ProcessingError == null),
                "failed" => query.Where(w => w.ProcessingError != null),
                _ => query,
            };
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(w => w.ReceivedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public void Update(ProviderWebhookInbox entry) => _context.ProviderWebhookInbox.Update(entry);

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg && pg.SqlState == "23505";
}
