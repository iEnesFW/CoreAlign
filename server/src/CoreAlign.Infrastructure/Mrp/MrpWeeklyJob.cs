using CoreAlign.Application.Mrp;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Mrp;

/// <summary>
/// F3.4 weekly MRP run. Executes Monday at 06:00 UTC across every tenant,
/// invoking <see cref="IMrpService.GenerateRequisitionSuggestionsAsync"/> per
/// tenant scope so reorder candidates from each catalog produce purchase
/// requisitions.
/// </summary>
public sealed class MrpWeeklyJob : BackgroundService
{
    private static readonly TimeSpan FailureBackoff = TimeSpan.FromHours(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MrpWeeklyJob> _logger;

    public MrpWeeklyJob(IServiceProvider serviceProvider, ILogger<MrpWeeklyJob> logger)
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
                _logger.LogError(ex, "MrpWeeklyJob iteration failed; backing off {Minutes} minutes.", FailureBackoff.TotalMinutes);
                try { await Task.Delay(FailureBackoff, stoppingToken).ConfigureAwait(false); }
                catch (TaskCanceledException) { return; }
            }
        }
    }

    internal static TimeSpan ComputeDelayUntilNextRun(DateTime nowUtc)
    {
        var todayRun = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 6, 0, 0, DateTimeKind.Utc);
        var next = nowUtc < todayRun && nowUtc.DayOfWeek == DayOfWeek.Monday
            ? todayRun
            : NextMondayAt(todayRun);
        if (next <= nowUtc) next = next.AddDays(7);
        return next - nowUtc;
    }

    private static DateTime NextMondayAt(DateTime baseDate)
    {
        var daysToAdd = ((int)DayOfWeek.Monday - (int)baseDate.DayOfWeek + 7) % 7;
        if (daysToAdd == 0) daysToAdd = 7;
        return baseDate.AddDays(daysToAdd);
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

        var tenantIds = await dbContext.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.IsActive)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var asOf = DateTime.UtcNow;
        var totalRequisitions = 0;
        var totalLines = 0;

        foreach (var tenantId in tenantIds)
        {
            using var perTenantScope = _serviceProvider.CreateScope();
            var scopedTenant = perTenantScope.ServiceProvider.GetRequiredService<ITenantContext>();
            using (scopedTenant.PushScope(tenantId))
            {
                var mrp = perTenantScope.ServiceProvider.GetRequiredService<IMrpService>();
                var uow = perTenantScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                try
                {
                    var result = await mrp.GenerateRequisitionSuggestionsAsync(asOf, cancellationToken);
                    await uow.SaveChangesAsync(cancellationToken);
                    totalRequisitions += result.RequisitionsCreated;
                    totalLines += result.LinesCreated;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MRP run failed for tenant {TenantId}.", tenantId);
                }
            }
        }

        _logger.LogInformation(
            "MrpWeeklyJob completed: {TenantCount} tenants, {ReqCount} requisitions, {LineCount} lines.",
            tenantIds.Count, totalRequisitions, totalLines);
    }
}
