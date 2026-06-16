using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.API.HostedServices;

/// <summary>
/// Rolls RANGE-partitioned leaf tables forward: ensures the next <see cref="MonthsAhead"/>
/// months of monthly partitions exist so new rows land in a prunable partition instead of
/// the catch-all DEFAULT partition (whose unbounded growth defeats partition pruning).
/// Runs once at startup and then daily. Independent of Hangfire so it works even though the
/// recurring-job host is not yet wired.
///
/// The DB function <c>corealign_ensure_future_partitions</c> (created in Phase86) is
/// idempotent. Under the default config the app connects as the table owner, so the call
/// succeeds as-is; when RLS is enabled the app connects as a non-owner role and the function
/// must be made SECURITY DEFINER (see SCALE_READINESS_ROADMAP.md operational gaps).
/// </summary>
public sealed class PartitionMaintenanceHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private const int MonthsAhead = 6;

    // (table, timestamp partition-key column) — must mirror the Phase86 leaf tables.
    private static readonly (string Table, string TsColumn)[] PartitionedTables =
    {
        ("activity_logs", "created_at_utc"),
        ("login_audit_logs", "attempted_at_utc"),
        ("outbox_messages", "created_at_utc"),
        ("notification_messages", "created_at_utc"),
        ("stock_movements", "occurred_at_utc"),
        ("customer_transactions", "occurred_at_utc"),
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PartitionMaintenanceHostedService> _logger;

    public PartitionMaintenanceHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<PartitionMaintenanceHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await EnsurePartitionsAsync(stoppingToken);
            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task EnsurePartitionsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

            // The partition function is Postgres-only; the dev/test SQLite provider
            // has no partitioning, so skip there rather than erroring every cycle.
            if (!db.Database.IsNpgsql())
            {
                return;
            }

            foreach (var (table, tsColumn) in PartitionedTables)
            {
                await db.Database.ExecuteSqlRawAsync(
                    "SELECT corealign_ensure_future_partitions({0}, {1}, {2})",
                    new object[] { table, tsColumn, MonthsAhead },
                    ct);
            }

            _logger.LogInformation(
                "Partition maintenance ensured {Months} months ahead for {Count} partitioned tables.",
                MonthsAhead,
                PartitionedTables.Length);
        }
        catch (Exception ex)
        {
            // Maintenance must never take the host down; log and retry next cycle.
            _logger.LogError(ex, "Partition maintenance run failed; will retry next cycle.");
        }
    }
}
