using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Providers.Payment;
using CoreAlign.Application.Providers.Payment.Events;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Payments;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Providers.Payment;

/// <summary>
/// Polls open payment transactions (Pending / Authorized) on a 1-hour cadence
/// and syncs their status with the originating provider. Idempotent — only
/// emits outbox events when the local status actually changes.
/// </summary>
public sealed class PaymentReconciliationJob : BackgroundService
{
    private const string SucceededMessageType = "PaymentSucceeded";
    private const string FailedMessageType = "PaymentFailed";
    private const int MaxTransactionsPerTenant = 200;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentReconciliationJob> _logger;
    private readonly TimeSpan _interval;

    public PaymentReconciliationJob(IServiceProvider serviceProvider, ILogger<PaymentReconciliationJob> logger)
        : this(serviceProvider, logger, DefaultInterval)
    {
    }

    public PaymentReconciliationJob(IServiceProvider serviceProvider, ILogger<PaymentReconciliationJob> logger, TimeSpan interval)
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
                await RunIterationAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment reconciliation iteration failed.");
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

    private async Task RunIterationAsync(CancellationToken cancellationToken)
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
            .ListByTenantAsync(tenantId, ProviderCategory.Payment, cancellationToken)
            .ConfigureAwait(false);
        if (configs.Count == 0 || configs.All(c => !c.IsEnabled))
        {
            return;
        }

        var transactionRepository = scope.ServiceProvider.GetRequiredService<IPaymentTransactionRepository>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IPaymentDispatcher>();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var outboxSignal = scope.ServiceProvider.GetRequiredService<IOutboxSignal>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var pending = await transactionRepository
            .ListPendingForTenantAsync(tenantId, MaxTransactionsPerTenant, cancellationToken)
            .ConfigureAwait(false);
        if (pending.Count == 0) return;

        var changedCount = 0;
        foreach (var transaction in pending)
        {
            if (cancellationToken.IsCancellationRequested) break;
            if (string.IsNullOrWhiteSpace(transaction.ExternalTransactionId)) continue;

            try
            {
                var info = await dispatcher
                    .GetTransactionAsync(transaction.ExternalTransactionId!, cancellationToken)
                    .ConfigureAwait(false);

                if (!StatusChanged(transaction.Status, info.Status))
                {
                    continue;
                }

                if (info.Status.Equals("Succeeded", StringComparison.OrdinalIgnoreCase)
                    || info.Status.Equals(PaymentTransactionStatus.Captured.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    transaction.MarkCaptured(transaction.ExternalTransactionId!, info.RawProviderJson);
                    transactionRepository.Update(transaction);
                    await EnqueueOutboxAsync(outboxRepository, outboxSignal, SucceededMessageType,
                        new PaymentSucceededEvent(
                            tenantId,
                            transaction.Id,
                            transaction.ProviderName,
                            transaction.ExternalTransactionId!,
                            transaction.OrderReference,
                            transaction.Amount,
                            transaction.Currency,
                            DateTime.UtcNow),
                        cancellationToken).ConfigureAwait(false);
                    changedCount++;
                }
                else if (info.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase)
                    || info.Status.Equals(PaymentTransactionStatus.Failed.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    transaction.MarkFailed("RECONCILED_FAILED", info.Status, info.RawProviderJson);
                    transactionRepository.Update(transaction);
                    await EnqueueOutboxAsync(outboxRepository, outboxSignal, FailedMessageType,
                        new PaymentFailedEvent(
                            tenantId,
                            transaction.Id,
                            transaction.ProviderName,
                            transaction.ExternalTransactionId,
                            transaction.OrderReference,
                            transaction.Amount,
                            transaction.Currency,
                            "RECONCILED_FAILED",
                            info.Status,
                            DateTime.UtcNow),
                        cancellationToken).ConfigureAwait(false);
                    changedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Payment reconciliation query failed for tenant {TenantId} transaction {TransactionId}; continuing.",
                    tenantId,
                    transaction.Id);
            }
        }

        if (changedCount > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        _logger.LogInformation(
            "Payment reconciliation completed for tenant {TenantId}: scanned {Count}, changed {Changed}.",
            tenantId,
            pending.Count,
            changedCount);
    }

    private static bool StatusChanged(PaymentTransactionStatus current, string remoteStatus)
    {
        if (string.IsNullOrWhiteSpace(remoteStatus)) return false;
        return !current.ToString().Equals(remoteStatus, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task EnqueueOutboxAsync<TEvent>(
        IOutboxRepository outboxRepository,
        IOutboxSignal signal,
        string messageType,
        TEvent payload,
        CancellationToken cancellationToken) where TEvent : class
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await outboxRepository.AddAsync(new OutboxMessage(messageType, json), cancellationToken).ConfigureAwait(false);
        signal.MarkPending();
    }
}
