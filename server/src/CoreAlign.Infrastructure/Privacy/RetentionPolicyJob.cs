using CoreAlign.Domain.Entities.Privacy;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Privacy;

public sealed class RetentionPolicyJob : BackgroundService
{
    private static readonly TimeSpan TurkeyOffset = TimeSpan.FromHours(3);
    private static readonly TimeOnly DailyRunTimeTr = new(4, 0);
    private static readonly TimeSpan FailureBackoff = TimeSpan.FromHours(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RetentionPolicyJob> _logger;

    public RetentionPolicyJob(IServiceProvider serviceProvider, ILogger<RetentionPolicyJob> logger)
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
            catch (TaskCanceledException)
            {
                return;
            }

            if (stoppingToken.IsCancellationRequested) return;

            try
            {
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RetentionPolicyJob iteration failed; backing off {Minutes} minutes.", FailureBackoff.TotalMinutes);
                try
                {
                    await Task.Delay(FailureBackoff, stoppingToken).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }
    }

    internal static TimeSpan ComputeDelayUntilNextRun(DateTime utcNow)
    {
        var trNow = utcNow + TurkeyOffset;
        var todayRunTr = trNow.Date.Add(DailyRunTimeTr.ToTimeSpan());
        var nextTr = trNow < todayRunTr ? todayRunTr : todayRunTr.AddDays(1);
        var nextUtc = nextTr - TurkeyOffset;
        var delay = nextUtc - utcNow;
        return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRetentionPolicyRepository>();
        var executor = scope.ServiceProvider.GetRequiredService<IRetentionPolicyExecutor>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var policies = await repository.ListAllEnabledAcrossTenantsAsync(cancellationToken);
        var policyByTenant = policies.GroupBy(p => p.TenantId).ToList();

        var totalAffected = 0;

        foreach (var group in policyByTenant)
        {
            using var perTenantScope = _serviceProvider.CreateScope();
            var tenantContext = perTenantScope.ServiceProvider.GetRequiredService<ITenantContext>();

            using (tenantContext.PushScope(group.Key))
            {
                foreach (var policy in group)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var affected = await executor.ExecuteAsync(policy, cancellationToken);
                        totalAffected += affected;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "RetentionPolicyJob: tenant {TenantId} policy {EntityType} failed.",
                            group.Key, policy.EntityType);
                    }
                }
            }
        }

        await uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("RetentionPolicyJob completed: {PolicyCount} policies, {AffectedCount} records affected.",
            policies.Count, totalAffected);
    }
}

public interface IRetentionPolicyExecutor
{
    Task<int> ExecuteAsync(RetentionPolicy policy, CancellationToken cancellationToken);
}
