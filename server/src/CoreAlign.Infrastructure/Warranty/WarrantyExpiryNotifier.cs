using System.Text.Json;
using CoreAlign.Application.Warranty.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Warranty;

/// <summary>
/// F3.1 daily job — emits one <c>WarrantyExpiringSoon</c> outbox message per
/// active contract whose end date falls within <see cref="ExpiryWindowDays"/>.
/// Runs at 08:00 Türkiye time so the notification subsystem can fan out e-mail
/// / SMS reminders during business hours. Tenant scoping is bypassed (uses
/// <c>IgnoreQueryFilters</c>) because the job processes all tenants in one pass.
/// </summary>
public sealed class WarrantyExpiryNotifier : BackgroundService
{
    public const int ExpiryWindowDays = 30;

    private static readonly TimeSpan TurkeyOffset = TimeSpan.FromHours(3);
    private static readonly TimeOnly DailyRunTime = new(8, 0);
    private static readonly TimeSpan FailureBackoff = TimeSpan.FromHours(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WarrantyExpiryNotifier> _logger;

    public WarrantyExpiryNotifier(IServiceProvider serviceProvider, ILogger<WarrantyExpiryNotifier> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ComputeDelayUntilNextRun(DateTime.UtcNow);
            try
            {
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException) { return; }

            if (stoppingToken.IsCancellationRequested) return;

            try
            {
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WarrantyExpiryNotifier iteration failed; backing off {Minutes} minutes.", FailureBackoff.TotalMinutes);
                try { await Task.Delay(FailureBackoff, stoppingToken).ConfigureAwait(false); }
                catch (TaskCanceledException) { return; }
            }
        }
    }

    internal static TimeSpan ComputeDelayUntilNextRun(DateTime nowUtc)
    {
        var trNow = nowUtc + TurkeyOffset;
        var todayRun = new DateTime(trNow.Year, trNow.Month, trNow.Day, DailyRunTime.Hour, DailyRunTime.Minute, 0);
        var nextRunTr = trNow >= todayRun ? todayRun.AddDays(1) : todayRun;
        var nextRunUtc = nextRunTr - TurkeyOffset;
        return nextRunUtc - nowUtc;
    }

    // WHY this runs here: WarrantyContract.MarkExpired had no caller, so a contract stayed
    // Active in every list and report long after its end date and the WarrantyExpired outbox
    // message was never emitted. Warranty DECISIONS were unaffected — IsValidAtDate compares the
    // dates rather than trusting the status — but the status itself was permanently wrong. The
    // sweep is bounded: once a contract is Expired it no longer matches.
    private async Task CloseElapsedContractsAsync(
        CoreAlignDbContext dbContext,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var elapsed = await dbContext.WarrantyContracts
            .IgnoreQueryFilters()
            .Where(c => !c.IsDeleted
                && c.Status == Domain.Enums.WarrantyContractStatus.Active
                && c.EndDate <= now)
            .ToListAsync(cancellationToken);

        if (elapsed.Count == 0) return;

        foreach (var contract in elapsed)
        {
            contract.MarkExpired(now);
            var payload = new WarrantyExpiredEvent(
                contract.TenantId, contract.Id, contract.CustomerId, contract.Number, contract.EndDate, now);
            var message = new OutboxMessage(
                WarrantyExpiredOutboxHandler.MessageTypeKey,
                JsonSerializer.Serialize(payload));
            message.TenantId = contract.TenantId;
            await dbContext.OutboxMessages.AddAsync(message, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("WarrantyExpiryNotifier closed {Count} elapsed warranty contracts.", elapsed.Count);
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

        var now = DateTime.UtcNow;
        var threshold = now.AddDays(ExpiryWindowDays);

        await CloseElapsedContractsAsync(dbContext, now, cancellationToken);

        var contracts = await dbContext.WarrantyContracts
            .IgnoreQueryFilters()
            .Where(c => !c.IsDeleted
                && c.Status == Domain.Enums.WarrantyContractStatus.Active
                && c.EndDate <= threshold
                && c.EndDate > now)
            .ToListAsync(cancellationToken);

        if (contracts.Count == 0)
        {
            _logger.LogDebug("WarrantyExpiryNotifier: no contracts expiring in the next {Days} days.", ExpiryWindowDays);
            return;
        }

        var emitted = 0;
        foreach (var contract in contracts)
        {
            var daysRemaining = (int)Math.Max(0, Math.Ceiling((contract.EndDate - now).TotalDays));
            var payload = new WarrantyExpiringSoonEvent(
                contract.TenantId,
                contract.Id,
                contract.CustomerId,
                contract.Number,
                contract.EndDate,
                daysRemaining,
                now);

            var message = new OutboxMessage(
                WarrantyExpiringNotificationOutboxHandler.MessageTypeKey,
                JsonSerializer.Serialize(payload));
            message.TenantId = contract.TenantId;

            await dbContext.OutboxMessages.AddAsync(message, cancellationToken);
            emitted++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("WarrantyExpiryNotifier emitted {Count} expiring-soon notifications.", emitted);
    }
}
