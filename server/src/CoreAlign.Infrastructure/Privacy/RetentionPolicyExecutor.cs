using CoreAlign.Domain.Entities.Privacy;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Privacy;

public class RetentionPolicyExecutor : IRetentionPolicyExecutor
{
    private const string EntityNotification = "Notification";
    private const string EntityAuditLog = "AuditLog";
    private const string EntityActivityLog = "ActivityLog";
    private const string EntityUserSession = "UserSession";

    private readonly CoreAlignDbContext _context;
    private readonly IRetentionPolicyRepository _repository;
    private readonly ILogger<RetentionPolicyExecutor> _logger;

    public RetentionPolicyExecutor(
        CoreAlignDbContext context,
        IRetentionPolicyRepository repository,
        ILogger<RetentionPolicyExecutor> logger)
    {
        _context = context;
        _repository = repository;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(RetentionPolicy policy, CancellationToken cancellationToken)
    {
        if (!policy.IsEnabled) return 0;

        var cutoffUtc = DateTime.UtcNow.AddDays(-policy.RetentionDays);
        int affected;

        switch (policy.EntityType)
        {
            case EntityNotification:
                affected = await ApplyNotificationRetentionAsync(policy, cutoffUtc, cancellationToken);
                break;
            case EntityActivityLog:
                affected = await ApplyActivityLogRetentionAsync(cutoffUtc, cancellationToken);
                break;
            case EntityAuditLog:
                affected = await ApplyAuditLogRetentionAsync(policy, cutoffUtc, cancellationToken);
                break;
            case EntityUserSession:
                affected = await ApplyUserSessionRetentionAsync(cutoffUtc, cancellationToken);
                break;
            default:
                _logger.LogWarning(
                    "RetentionPolicyExecutor: unknown EntityType '{EntityType}' for tenant {TenantId}.",
                    policy.EntityType, policy.TenantId);
                return 0;
        }

        policy.RecordRun(DateTime.UtcNow, affected);
        _repository.Update(policy);
        return affected;
    }

    private async Task<int> ApplyNotificationRetentionAsync(RetentionPolicy policy, DateTime cutoffUtc, CancellationToken cancellationToken)
    {
        var rows = await _context.NotificationMessages
            .Where(n => n.CreatedAtUtc < cutoffUtc)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            if (policy.ActionOnExpiry == RetentionActionOnExpiry.Delete)
            {
                _context.NotificationMessages.Remove(row);
            }
            else
            {
                row.MarkDeleted(null, "Privacy.RetentionExpiry", now);
            }
        }
        return rows.Count;
    }

    private async Task<int> ApplyActivityLogRetentionAsync(DateTime cutoffUtc, CancellationToken cancellationToken)
    {
        return await _context.ActivityLogs
            .Where(a => a.CreatedAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<int> ApplyAuditLogRetentionAsync(RetentionPolicy policy, DateTime cutoffUtc, CancellationToken cancellationToken)
    {
        if (policy.KeepFinancialTrail)
        {
            _logger.LogInformation(
                "RetentionPolicyExecutor: AuditLog policy for tenant {TenantId} preserved due to KeepFinancialTrail.",
                policy.TenantId);
            return 0;
        }

        return await _context.EntityAuditLogs
            .Where(a => a.ChangedAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<int> ApplyUserSessionRetentionAsync(DateTime cutoffUtc, CancellationToken cancellationToken)
    {
        return await _context.UserSessions
            .Where(s => s.ExpiresAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
