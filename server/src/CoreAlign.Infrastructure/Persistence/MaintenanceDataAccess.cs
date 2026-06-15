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

    public async Task<int> AnonymizeLoginAuditLogsOlderThanAsync(DateTime cutoffUtc, Func<string, string> ipHasher, CancellationToken cancellationToken = default)
    {
        var rows = await _context.LoginAuditLogs
            .Where(l => l.AttemptedAtUtc < cutoffUtc
                && l.IpAddress != null
                && l.IpAddressHash == null)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0) return 0;

        foreach (var row in rows)
        {
            row.IpAddressHash = ipHasher(row.IpAddress!);
            row.IpAddress = null;
        }
        await _context.SaveChangesAsync(cancellationToken);
        return rows.Count;
    }

    public async Task<int> AnonymizeActivityLogsOlderThanAsync(DateTime cutoffUtc, Func<string, string> ipHasher, Func<string, string> uaHasher, CancellationToken cancellationToken = default)
    {
        var rows = await _context.ActivityLogs
            .IgnoreQueryFilters()
            .Where(l => l.CreatedAtUtc < cutoffUtc
                && ((l.IpAddress != null && l.IpAddressHash == null)
                    || (l.UserAgent != null && l.UserAgentHash == null)))
            .ToListAsync(cancellationToken);

        if (rows.Count == 0) return 0;

        foreach (var row in rows)
        {
            if (!string.IsNullOrEmpty(row.IpAddress) && string.IsNullOrEmpty(row.IpAddressHash))
            {
                row.IpAddressHash = ipHasher(row.IpAddress);
                row.IpAddress = null;
            }
            if (!string.IsNullOrEmpty(row.UserAgent) && string.IsNullOrEmpty(row.UserAgentHash))
            {
                row.UserAgentHash = uaHasher(row.UserAgent);
                row.UserAgent = null;
            }
        }
        await _context.SaveChangesAsync(cancellationToken);
        return rows.Count;
    }
}
