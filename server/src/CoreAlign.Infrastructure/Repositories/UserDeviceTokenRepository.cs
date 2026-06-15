using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public sealed class UserDeviceTokenRepository : IUserDeviceTokenRepository
{
    private readonly CoreAlignDbContext _context;

    public UserDeviceTokenRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<UserDeviceToken?> GetByTokenAsync(Guid tenantId, string token, CancellationToken ct) =>
        _context.UserDeviceTokens.FirstOrDefaultAsync(
            t => t.TenantId == tenantId && t.Token == token,
            ct);

    public async Task<IReadOnlyList<UserDeviceToken>> ListActiveByUserAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        return await _context.UserDeviceTokens
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.UserId == userId && t.IsActive)
            .OrderByDescending(t => t.LastSeenAtUtc)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<UserDeviceToken>> ListActiveByCustomerAsync(Guid tenantId, Guid customerId, CancellationToken ct)
    {
        var query =
            from token in _context.UserDeviceTokens.AsNoTracking()
            join membership in _context.CustomerUsers.AsNoTracking()
                on new { token.TenantId, token.UserId } equals new { membership.TenantId, membership.UserId }
            where token.TenantId == tenantId
                && membership.CustomerId == customerId
                && token.IsActive
            orderby token.LastSeenAtUtc descending
            select token;

        return await query.ToListAsync(ct);
    }

    public Task AddAsync(UserDeviceToken entity, CancellationToken ct) =>
        _context.UserDeviceTokens.AddAsync(entity, ct).AsTask();

    public async Task<bool> DeactivateAsync(Guid tenantId, Guid userId, string token, DateTime utcNow, CancellationToken ct)
    {
        var existing = await _context.UserDeviceTokens.FirstOrDefaultAsync(
            t => t.TenantId == tenantId && t.UserId == userId && t.Token == token,
            ct);
        if (existing is null) return false;
        existing.Deactivate(utcNow);
        return true;
    }

    public async Task<bool> MarkLastUsedAsync(Guid tenantId, Guid tokenId, DateTime utcNow, CancellationToken ct)
    {
        var existing = await _context.UserDeviceTokens.FirstOrDefaultAsync(
            t => t.TenantId == tenantId && t.Id == tokenId,
            ct);
        if (existing is null) return false;
        existing.MarkLastUsed(utcNow);
        return true;
    }
}
