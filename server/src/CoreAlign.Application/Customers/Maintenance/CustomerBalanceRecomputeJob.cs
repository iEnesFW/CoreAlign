using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Customers.Maintenance;

public sealed class CustomerBalanceRecomputeJob
{
    private readonly ICustomerBalanceRecomputeDataSource _data;
    private readonly ISender _mediator;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CustomerBalanceRecomputeJob> _logger;

    public CustomerBalanceRecomputeJob(
        ICustomerBalanceRecomputeDataSource data,
        ISender mediator,
        ITenantContext tenantContext,
        ILogger<CustomerBalanceRecomputeJob> logger)
    {
        _data = data;
        _mediator = mediator;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var tenantIds = await _data.GetTenantIdsWithCustomersAsync(cancellationToken).ConfigureAwait(false);
        var healed = 0;
        var failed = 0;

        foreach (var tenantId in tenantIds)
        {
            using var scope = _tenantContext.PushScope(tenantId);
            try
            {
                var result = await _mediator
                    .Send(new RecomputeCustomerBalancesCommand(), cancellationToken)
                    .ConfigureAwait(false);
                healed += result.Recomputed;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "CustomerBalanceRecomputeJob failed for tenant {TenantId}", tenantId);
            }
        }

        _logger.LogInformation(
            "CustomerBalanceRecomputeJob healed {Healed} customer snapshot(s) across {TenantCount} tenant(s); {Failed} tenant(s) failed.",
            healed, tenantIds.Count, failed);
    }
}
