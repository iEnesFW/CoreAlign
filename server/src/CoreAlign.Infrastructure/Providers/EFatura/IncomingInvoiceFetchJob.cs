using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Providers.EFatura;

public sealed class IncomingInvoiceFetchJob
{
    private static readonly TimeSpan LookbackWindow = TimeSpan.FromDays(30);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IncomingInvoiceFetchJob> _logger;

    public IncomingInvoiceFetchJob(IServiceProvider serviceProvider, ILogger<IncomingInvoiceFetchJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        var tenantIds = await dbContext.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => t.IsActive)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var tenantId in tenantIds)
        {
            if (cancellationToken.IsCancellationRequested) break;
            await FetchForTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task FetchForTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        using var _ = tenantContext.PushScope(tenantId);

        var configRepository = scope.ServiceProvider.GetRequiredService<ITenantProviderConfigRepository>();
        var configs = await configRepository
            .ListByTenantAsync(tenantId, ProviderCategory.EFatura, cancellationToken)
            .ConfigureAwait(false);

        var listCapable = configs.FirstOrDefault(c =>
            c.IsEnabled && (c.EnabledCapabilities & (int)EFaturaProviderCapabilities.CanListReceived) != 0);
        if (listCapable is null)
        {
            return;
        }

        var dispatcher = scope.ServiceProvider.GetRequiredService<IEFaturaDispatcher>();
        var repository = scope.ServiceProvider.GetRequiredService<IIncomingInvoiceRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var toUtc = DateTime.UtcNow;
            var fromUtc = toUtc - LookbackWindow;
            var items = await dispatcher
                .ListReceivedAsync(fromUtc, toUtc, listCapable.ProviderName, cancellationToken)
                .ConfigureAwait(false);
            if (items.Count == 0)
            {
                return;
            }

            var existing = await repository
                .ExistingEttnsAsync(items.Select(i => i.Uuid), cancellationToken)
                .ConfigureAwait(false);
            var knownEttns = existing.Select(e => e.Ettn).ToHashSet(StringComparer.Ordinal);

            var added = 0;
            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Uuid) || knownEttns.Contains(item.Uuid))
                {
                    continue;
                }

                var invoice = new IncomingInvoice(
                    item.Uuid,
                    item.SenderVkn,
                    senderName: null,
                    item.DocumentNumber,
                    item.IssueDate,
                    listCapable.ProviderName,
                    item.Status);
                await repository.AddAsync(invoice, cancellationToken).ConfigureAwait(false);
                knownEttns.Add(item.Uuid);
                added++;
            }

            if (added > 0)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "Incoming invoice fetch for tenant {TenantId}: {Fetched} fetched, {Added} new.",
                tenantId, items.Count, added);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Incoming invoice fetch failed for tenant {TenantId}; continuing.", tenantId);
        }
    }
}
