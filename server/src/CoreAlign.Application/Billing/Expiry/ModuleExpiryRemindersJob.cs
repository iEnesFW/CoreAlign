using CoreAlign.Application.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Billing.Expiry;

/// <summary>
/// Warns a company that a purchased module is about to lapse, starting fifteen days out.
/// </summary>
public sealed class ModuleExpiryRemindersJob
{
    private const int MaxBatch = 500;
    private const string Locale = "tr";

    private static readonly NotificationChannel[] Channels =
    [
        NotificationChannel.InApp,
        NotificationChannel.Email,
    ];

    private readonly IModuleExpiryDataSource _data;
    private readonly ITenantContext _tenant;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<ModuleExpiryRemindersJob> _logger;

    public ModuleExpiryRemindersJob(
        IModuleExpiryDataSource data,
        ITenantContext tenant,
        INotificationDispatcher dispatcher,
        ILogger<ModuleExpiryRemindersJob> logger)
    {
        _data = data;
        _tenant = tenant;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiring = await _data
            .GetExpiringAsync(now, ModuleExpiryThresholds.WindowDays, MaxBatch, cancellationToken)
            .ConfigureAwait(false);

        if (expiring.Count == 0)
        {
            _logger.LogDebug("ModuleExpiryRemindersJob found nothing expiring within {Days} days.", ModuleExpiryThresholds.WindowDays);
            return;
        }

        var dispatched = 0;
        var skipped = 0;

        foreach (var group in expiring.GroupBy(e => e.TenantId))
        {
            // Without the scope the dispatcher's dedup read resolves to Guid.Empty, never finds the
            // previous message, and the filtered unique index throws 23505 into this job's catch.
            using var scope = _tenant.PushScope(group.Key);

            var recipients = await _data.GetTenantAdminUserIdsAsync(group.Key, cancellationToken).ConfigureAwait(false);
            if (recipients.Count == 0)
            {
                _logger.LogWarning(
                    "ModuleExpiryRemindersJob found no TenantAdmin recipient for tenant {TenantId}; {Count} expiring module(s) go unannounced.",
                    group.Key, group.Count());
                continue;
            }

            foreach (var item in group)
            {
                var threshold = ModuleExpiryThresholds.ResolveThreshold(now, item.EndUtc);
                if (threshold is null)
                {
                    skipped++;
                    continue;
                }

                var payload = ModuleExpiryThresholds.BuildPayload(
                    item.ModuleCode, item.ModuleName, item.EndUtc, threshold.Value);

                foreach (var userId in recipients)
                {
                    try
                    {
                        await _dispatcher
                            .DispatchAsync(
                                new NotificationRequest(
                                    group.Key,
                                    userId,
                                    null,
                                    ModuleExpiryTemplateKeys.CategoryKey,
                                    ModuleExpiryTemplateKeys.Expiring,
                                    Locale,
                                    payload,
                                    Channels),
                                cancellationToken)
                            .ConfigureAwait(false);
                        dispatched++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "ModuleExpiryRemindersJob failed to warn user {UserId} about {ModuleCode} in tenant {TenantId}.",
                            userId, item.ModuleCode, group.Key);
                    }
                }
            }
        }

        _logger.LogInformation(
            "ModuleExpiryRemindersJob examined {Total} grant(s): {Dispatched} notification(s) dispatched, {Skipped} outside a threshold.",
            expiring.Count, dispatched, skipped);
    }
}
