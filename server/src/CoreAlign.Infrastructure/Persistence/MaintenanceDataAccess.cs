using CoreAlign.Application.Jobs;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Persistence;

public sealed class MaintenanceDataAccess : IMaintenanceDataAccess
{
    private readonly CoreAlignDbContext _context;

    public MaintenanceDataAccess(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<int> DeleteRefreshTokensOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
        => _context.RefreshTokens
            .Where(t => t.ExpiresAtUtc < cutoffUtc || (t.RevokedAtUtc != null && t.RevokedAtUtc < cutoffUtc))
            .ExecuteDeleteAsync(cancellationToken);

    public Task<int> DeleteEmailVerificationTokensOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
        => _context.EmailVerificationTokens
            .Where(t => t.CreatedAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);

    public Task<int> DeletePasswordResetTokensOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
        => _context.PasswordResetTokens
            .Where(t => t.CreatedAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);

    public Task<int> DeleteTwoFactorChallengesOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
        => _context.TwoFactorChallenges
            .IgnoreQueryFilters()
            .Where(t => t.ExpiresAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);

    // WHY: set-based ExecuteUpdate (grouped by the value being hashed) instead of
    // load-track-modify-SaveChanges. login_audit_logs is a RANGE-partitioned table
    // whose EF-side key is only `id` while the DB key is composite (id, attempted_at_utc);
    // a per-row tracked UPDATE asserts "exactly 1 row affected" and throws
    // DbUpdateConcurrencyException on that mismatch. A bulk UPDATE performs no such
    // assertion, is idempotent (the *Hash == null guard), and cannot be poisoned by the
    // partition / reused-identity quirks.
    public async Task<int> AnonymizeLoginAuditLogsOlderThanAsync(DateTime cutoffUtc, Func<string, string> ipHasher, CancellationToken cancellationToken = default)
    {
        var addresses = await _context.LoginAuditLogs
            .Where(l => l.AttemptedAtUtc < cutoffUtc
                && l.IpAddress != null
                && l.IpAddressHash == null)
            .Select(l => l.IpAddress!)
            .Distinct()
            .ToListAsync(cancellationToken);

        var affected = 0;
        foreach (var ip in addresses)
        {
            var hash = ipHasher(ip);
            affected += await _context.LoginAuditLogs
                .Where(l => l.AttemptedAtUtc < cutoffUtc && l.IpAddress == ip && l.IpAddressHash == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.IpAddressHash, hash)
                    .SetProperty(l => l.IpAddress, (string?)null), cancellationToken);
        }
        return affected;
    }

    public async Task<int> AnonymizeActivityLogsOlderThanAsync(DateTime cutoffUtc, Func<string, string> ipHasher, Func<string, string> uaHasher, CancellationToken cancellationToken = default)
    {
        var affected = await _context.ActivityLogs
            .IgnoreQueryFilters()
            .CountAsync(l => l.CreatedAtUtc < cutoffUtc
                && ((l.IpAddress != null && l.IpAddressHash == null)
                    || (l.UserAgent != null && l.UserAgentHash == null)), cancellationToken);

        if (affected == 0) return 0;

        var addresses = await _context.ActivityLogs
            .IgnoreQueryFilters()
            .Where(l => l.CreatedAtUtc < cutoffUtc && l.IpAddress != null && l.IpAddressHash == null)
            .Select(l => l.IpAddress!)
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var ip in addresses)
        {
            var hash = ipHasher(ip);
            await _context.ActivityLogs
                .IgnoreQueryFilters()
                .Where(l => l.CreatedAtUtc < cutoffUtc && l.IpAddress == ip && l.IpAddressHash == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.IpAddressHash, hash)
                    .SetProperty(l => l.IpAddress, (string?)null), cancellationToken);
        }

        var agents = await _context.ActivityLogs
            .IgnoreQueryFilters()
            .Where(l => l.CreatedAtUtc < cutoffUtc && l.UserAgent != null && l.UserAgentHash == null)
            .Select(l => l.UserAgent!)
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var ua in agents)
        {
            var hash = uaHasher(ua);
            await _context.ActivityLogs
                .IgnoreQueryFilters()
                .Where(l => l.CreatedAtUtc < cutoffUtc && l.UserAgent == ua && l.UserAgentHash == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.UserAgentHash, hash)
                    .SetProperty(l => l.UserAgent, (string?)null), cancellationToken);
        }

        return affected;
    }
}
