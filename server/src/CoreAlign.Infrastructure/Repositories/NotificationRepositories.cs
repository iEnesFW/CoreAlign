using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public sealed class NotificationMessageRepository : INotificationMessageRepository
{
    private readonly CoreAlignDbContext _context;
    public NotificationMessageRepository(CoreAlignDbContext context) => _context = context;

    public Task<NotificationMessage?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct) =>
        _context.NotificationMessages.FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Id == id, ct);

    public async Task<IReadOnlyList<NotificationMessage>> ListAsync(Guid tenantId, NotificationStatus? status, string? categoryKey, NotificationChannel? channel, int skip, int take, CancellationToken ct)
    {
        var query = _context.NotificationMessages.AsNoTracking().Where(m => m.TenantId == tenantId);
        if (status.HasValue) query = query.Where(m => m.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(categoryKey)) query = query.Where(m => m.CategoryKey == categoryKey);
        if (channel.HasValue) query = query.Where(m => m.Channel == channel.Value);
        return await query.OrderByDescending(m => m.CreatedAtUtc).Skip(skip).Take(take).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<NotificationMessage>> ListForUserAsync(Guid tenantId, Guid userId, bool unreadOnly, int skip, int take, CancellationToken ct)
    {
        var query = _context.NotificationMessages.Where(m => m.TenantId == tenantId && m.UserId == userId);
        if (unreadOnly) query = query.Where(m => m.Status != NotificationStatus.Read);
        return await query.OrderByDescending(m => m.CreatedAtUtc).Skip(skip).Take(take).ToListAsync(ct);
    }

    public Task<int> CountUnreadAsync(Guid tenantId, Guid userId, CancellationToken ct) =>
        _context.NotificationMessages.CountAsync(m => m.TenantId == tenantId && m.UserId == userId && m.Status != NotificationStatus.Read, ct);

    public Task<NotificationMessage?> GetByProviderMessageIdAsync(Guid tenantId, string providerName, string providerMessageId, CancellationToken ct) =>
        _context.NotificationMessages.FirstOrDefaultAsync(
            m => m.TenantId == tenantId && m.ProviderUsed == providerName && m.ProviderMessageId == providerMessageId,
            ct);

    public Task<NotificationMessage?> GetByHashAsync(Guid tenantId, string idempotencyHash, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyHash))
        {
            return Task.FromResult<NotificationMessage?>(null);
        }
        return _context.NotificationMessages.FirstOrDefaultAsync(
            m => m.TenantId == tenantId && m.IdempotencyHash == idempotencyHash,
            ct);
    }

    public async Task AddAsync(NotificationMessage entity, CancellationToken ct) =>
        await _context.NotificationMessages.AddAsync(entity, ct).ConfigureAwait(false);

    public async Task UpsertAsync(NotificationMessage entity, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var entry = _context.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            await _context.NotificationMessages.AddAsync(entity, ct).ConfigureAwait(false);
        }
    }
}

public sealed class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly CoreAlignDbContext _context;
    public NotificationTemplateRepository(CoreAlignDbContext context) => _context = context;

    public Task<NotificationTemplate?> GetByKeyLocaleAsync(Guid? tenantId, string key, NotificationChannel channel, string locale, CancellationToken ct) =>
        _context.NotificationTemplates
            .IgnoreQueryFilters()
            .Where(t => t.TenantId == tenantId && t.Key == key && t.Channel == channel && t.Locale == locale && t.IsActive)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<NotificationTemplate>> ListAsync(Guid? tenantId, CancellationToken ct)
    {
        return await _context.NotificationTemplates
            .IgnoreQueryFilters()
            .Where(t => t.TenantId == tenantId)
            .OrderBy(t => t.Key).ThenBy(t => t.Locale).ThenBy(t => t.Channel)
            .ToListAsync(ct);
    }

    public async Task AddAsync(NotificationTemplate entity, CancellationToken ct) =>
        await _context.NotificationTemplates.AddAsync(entity, ct).ConfigureAwait(false);

    public Task<bool> ExistsAsync(Guid? tenantId, string key, NotificationChannel channel, string locale, CancellationToken ct) =>
        _context.NotificationTemplates
            .IgnoreQueryFilters()
            .AnyAsync(t => t.TenantId == tenantId && t.Key == key && t.Channel == channel && t.Locale == locale, ct);
}

public sealed class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly CoreAlignDbContext _context;
    public NotificationPreferenceRepository(CoreAlignDbContext context) => _context = context;

    public Task<NotificationPreference?> GetAsync(Guid tenantId, Guid userId, string categoryKey, NotificationChannel channel, CancellationToken ct) =>
        _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.UserId == userId && p.CategoryKey == categoryKey && p.Channel == channel, ct);

    public async Task<IReadOnlyList<NotificationPreference>> ListForUserAsync(Guid tenantId, Guid userId, CancellationToken ct) =>
        await _context.NotificationPreferences
            .Where(p => p.TenantId == tenantId && p.UserId == userId)
            .ToListAsync(ct);

    public async Task AddAsync(NotificationPreference entity, CancellationToken ct) =>
        await _context.NotificationPreferences.AddAsync(entity, ct).ConfigureAwait(false);
}
