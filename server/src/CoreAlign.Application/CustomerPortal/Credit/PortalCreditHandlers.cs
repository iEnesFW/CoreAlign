using CoreAlign.Application.B2B;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.CustomerPortal.Credit;

public static class CreditSnapshotFactory
{
    public const decimal SoftLimitThresholdPercent = 80m;
    public const decimal HardLimitThresholdPercent = 100m;

    public static CreditSnapshotDto Build(Customer customer, decimal currentBalance)
    {
        var limit = customer.CreditLimit;
        var outstanding = Math.Max(0m, currentBalance);
        var available = limit > 0m ? Math.Max(0m, limit - outstanding) : 0m;
        var usagePercent = limit > 0m
            ? Math.Round((outstanding / limit) * 100m, 2)
            : 0m;
        return new CreditSnapshotDto(
            CustomerId: customer.Id,
            Currency: customer.DefaultCurrency,
            Limit: limit,
            Outstanding: outstanding,
            Available: available,
            UsagePercent: usagePercent,
            IsSoftLimitReached: limit > 0m && usagePercent >= SoftLimitThresholdPercent && usagePercent < HardLimitThresholdPercent,
            IsHardLimitReached: limit > 0m && usagePercent >= HardLimitThresholdPercent);
    }

    public static decimal ResolveCurrentBalance(Customer customer, decimal ledgerBalance)
    {
        if (ledgerBalance == 0m && customer.CurrentBalance != 0m)
        {
            return customer.CurrentBalance;
        }
        return ledgerBalance;
    }
}

public class GetPortalCreditSnapshotHandler : IRequestHandler<GetPortalCreditSnapshotQuery, CreditSnapshotDto>
{
    private readonly IPortalScopeService _scope;
    private readonly ICustomerRepository _customers;
    private readonly ICustomerLedgerRepository _ledger;

    public GetPortalCreditSnapshotHandler(
        IPortalScopeService scope,
        ICustomerRepository customers,
        ICustomerLedgerRepository ledger)
    {
        _scope = scope;
        _customers = customers;
        _ledger = ledger;
    }

    public async Task<CreditSnapshotDto> Handle(GetPortalCreditSnapshotQuery request, CancellationToken cancellationToken)
    {
        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var customer = await _customers.GetByIdAsync(customerId, cancellationToken)
            ?? throw new CustomerNotFoundException();
        var ledgerBalance = await _ledger.GetCurrentBalanceAsync(customerId, cancellationToken);
        var balance = CreditSnapshotFactory.ResolveCurrentBalance(customer, ledgerBalance);
        return CreditSnapshotFactory.Build(customer, balance);
    }
}

public class GetDealerCustomerCreditSnapshotHandler : IRequestHandler<GetDealerCustomerCreditSnapshotQuery, CreditSnapshotDto>
{
    private readonly IPortalScopeService _scope;
    private readonly ICustomerRepository _customers;
    private readonly ICustomerLedgerRepository _ledger;

    public GetDealerCustomerCreditSnapshotHandler(
        IPortalScopeService scope,
        ICustomerRepository customers,
        ICustomerLedgerRepository ledger)
    {
        _scope = scope;
        _customers = customers;
        _ledger = ledger;
    }

    public async Task<CreditSnapshotDto> Handle(GetDealerCustomerCreditSnapshotQuery request, CancellationToken cancellationToken)
    {
        await _scope.GetCurrentDealerAccountIdAsync(cancellationToken);
        var allowed = await _scope.GetDealerAllowedCustomerIdsAsync(cancellationToken);
        if (!allowed.Contains(request.CustomerId))
        {
            throw new DealerCustomerNotAuthorizedException();
        }

        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();
        var ledgerBalance = await _ledger.GetCurrentBalanceAsync(request.CustomerId, cancellationToken);
        var balance = CreditSnapshotFactory.ResolveCurrentBalance(customer, ledgerBalance);
        return CreditSnapshotFactory.Build(customer, balance);
    }
}
