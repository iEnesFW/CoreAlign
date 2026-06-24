using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.CustomerPortal.Credit;

public interface ICreditLimitGuard
{
    Task EnsureWithinLimitAsync(Customer customer, decimal additionalExposure, CancellationToken cancellationToken = default);
}

public class CreditLimitGuard : ICreditLimitGuard
{
    private readonly ICustomerLedgerRepository _ledger;

    public CreditLimitGuard(ICustomerLedgerRepository ledger)
    {
        _ledger = ledger;
    }

    public async Task EnsureWithinLimitAsync(Customer customer, decimal additionalExposure, CancellationToken cancellationToken = default)
    {
        if (customer.CreditLimit <= 0m)
        {
            return;
        }
        var ledgerBalance = await _ledger.GetCurrentBalanceAsync(customer.Id, cancellationToken);
        var currentBalance = CreditSnapshotFactory.ResolveCurrentBalance(customer, ledgerBalance);
        var projected = Math.Max(0m, currentBalance) + additionalExposure;
        if (projected > customer.CreditLimit)
        {
            throw new CreditLimitExceededException(customer.CreditLimit, projected);
        }
    }
}
