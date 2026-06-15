using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Application.Providers.EFatura.Events;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAlign.Infrastructure.Providers.EFatura;

public sealed class EFaturaReconciliationJob : BackgroundService
{
    private const string StatusChangedMessageType = "EFaturaStatusChanged";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(24);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EFaturaReconciliationJob> _logger;
    private readonly TimeSpan _interval;

    public EFaturaReconciliationJob(IServiceProvider serviceProvider, ILogger<EFaturaReconciliationJob> logger)
        : this(serviceProvider, logger, DefaultInterval)
    {
    }

    public EFaturaReconciliationJob(IServiceProvider serviceProvider, ILogger<EFaturaReconciliationJob> logger, TimeSpan interval)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _interval = interval <= TimeSpan.Zero ? DefaultInterval : interval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunReconciliationAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EFatura reconciliation iteration failed.");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    private async Task RunReconciliationAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        var tenantIds = await dbContext.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(t => t.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var tenantId in tenantIds)
        {
            if (cancellationToken.IsCancellationRequested) break;
            await ReconcileTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ReconcileTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        using var _ = tenantContext.PushScope(tenantId);

        var configRepository = scope.ServiceProvider.GetRequiredService<ITenantProviderConfigRepository>();
        var configs = await configRepository
            .ListByTenantAsync(tenantId, ProviderCategory.EFatura, cancellationToken)
            .ConfigureAwait(false);

        if (configs.Count == 0 || configs.All(c => !c.IsEnabled))
        {
            return;
        }

        var dispatcher = scope.ServiceProvider.GetRequiredService<IEFaturaDispatcher>();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var outboxSignal = scope.ServiceProvider.GetRequiredService<IOutboxSignal>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var changedCount = 0;
        try
        {
            var pending = await LoadPendingTrackedSubmissionsAsync(scope.ServiceProvider, tenantId, cancellationToken).ConfigureAwait(false);
            foreach (var entry in pending)
            {
                if (cancellationToken.IsCancellationRequested) break;
                try
                {
                    var status = await dispatcher
                        .GetStatusAsync(entry.Ettn, entry.ProviderName, cancellationToken)
                        .ConfigureAwait(false);

                    if (!string.Equals(status.Status, entry.LastKnownStatus, StringComparison.OrdinalIgnoreCase))
                    {
                        await RaiseStatusChangedAsync(
                            outboxRepository,
                            outboxSignal,
                            tenantId,
                            entry.Ettn,
                            entry.ProviderName,
                            entry.LastKnownStatus,
                            status.Status,
                            cancellationToken).ConfigureAwait(false);
                        changedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "EFatura status query failed for tenant {TenantId} ettn {Ettn}; continuing.",
                        tenantId,
                        entry.Ettn);
                }
            }

            if (changedCount > 0)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "EFatura reconciliation completed for tenant {TenantId}: scanned {Count}, changed {Changed}.",
                tenantId,
                pending.Count,
                changedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EFatura reconciliation failed for tenant {TenantId}.", tenantId);
        }
    }

    private static Task<IReadOnlyList<TrackedSubmission>> LoadPendingTrackedSubmissionsAsync(
        IServiceProvider services,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        // F2.1 scope: the EFaturaSubmission/EFaturaInvoice tracking tables are not
        // yet wired into the domain — once the Phase51 schema lands and the
        // repository ships, this query reads from there. For now we return an
        // empty set so the job loop still exercises end-to-end without blocking
        // the rest of F2.1.
        _ = services;
        _ = tenantId;
        _ = cancellationToken;
        return Task.FromResult<IReadOnlyList<TrackedSubmission>>(Array.Empty<TrackedSubmission>());
    }

    private static async Task RaiseStatusChangedAsync(
        IOutboxRepository outboxRepository,
        IOutboxSignal outboxSignal,
        Guid tenantId,
        string ettn,
        string providerName,
        string? previousStatus,
        string currentStatus,
        CancellationToken cancellationToken)
    {
        var evt = new EFaturaStatusChangedEvent(
            tenantId,
            ettn,
            providerName,
            previousStatus,
            currentStatus,
            DateTime.UtcNow);

        var payload = JsonSerializer.Serialize(evt, JsonOptions);
        await outboxRepository
            .AddAsync(new OutboxMessage(StatusChangedMessageType, payload), cancellationToken)
            .ConfigureAwait(false);
        outboxSignal.MarkPending();
    }

    private sealed record TrackedSubmission(string Ettn, string ProviderName, string LastKnownStatus);
}
