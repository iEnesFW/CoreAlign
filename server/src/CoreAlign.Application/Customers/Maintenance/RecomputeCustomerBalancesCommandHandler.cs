using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Customers.Maintenance;

public sealed class RecomputeCustomerBalancesCommandHandler
    : IRequestHandler<RecomputeCustomerBalancesCommand, RecomputeCustomerBalancesResult>
{
    private const string AccountsReceivableCode = "120";
    private const int PageSize = 200;

    private readonly ICustomerRepository _customers;
    private readonly ICustomerLedgerRepository _ledger;
    private readonly IReportRepository _reports;
    private readonly IJournalEntryRepository _journals;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public RecomputeCustomerBalancesCommandHandler(
        ICustomerRepository customers,
        ICustomerLedgerRepository ledger,
        IReportRepository reports,
        IJournalEntryRepository journals,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _customers = customers;
        _ledger = ledger;
        _reports = reports;
        _journals = journals;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<RecomputeCustomerBalancesResult> Handle(RecomputeCustomerBalancesCommand request, CancellationToken cancellationToken)
    {
        var customers = await ResolveCustomersAsync(request.CustomerId, cancellationToken);

        var today = DateTime.UtcNow.Date;
        var openInvoices = await _reports.GetOpenInvoicesAcrossCustomersAsync(cancellationToken);
        var overdueByCustomer = openInvoices
            .Where(r => r.DueDate.Date < today)
            .GroupBy(r => r.CustomerId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Outstanding));

        var drifts = new List<CustomerBalanceDrift>();
        var recomputed = 0;
        var ledgerTotal = 0m;

        foreach (var customer in customers)
        {
            var ledgerBalance = await _ledger.GetCurrentBalanceAsync(customer.Id, cancellationToken);
            var computedOverdue = overdueByCustomer.GetValueOrDefault(customer.Id, 0m);
            ledgerTotal += ledgerBalance;

            if (customer.CurrentBalance == ledgerBalance && customer.OverdueAmount == computedOverdue)
            {
                continue;
            }

            drifts.Add(new CustomerBalanceDrift(
                customer.Id, customer.Name,
                customer.CurrentBalance, ledgerBalance,
                customer.OverdueAmount, computedOverdue));

            if (!request.DryRun)
            {
                customer.RecalculateBalance(ledgerBalance, computedOverdue);
                _customers.Update(customer);
                recomputed++;
            }
        }

        var glControl = await ResolveArControlBalanceAsync(cancellationToken);

        if (recomputed > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new RecomputeCustomerBalancesResult(
            request.DryRun,
            customers.Count,
            drifts.Count,
            recomputed,
            ledgerTotal,
            glControl,
            ledgerTotal - glControl,
            drifts);
    }

    private async Task<List<Customer>> ResolveCustomersAsync(Guid? customerId, CancellationToken cancellationToken)
    {
        if (customerId is Guid id)
        {
            var single = await _customers.GetByIdAsync(id, cancellationToken)
                ?? throw new CustomerNotFoundException();
            _tenantContext.EnsureSameTenant(single.TenantId);
            return new List<Customer> { single };
        }

        var all = new List<Customer>();
        var page = 1;
        while (true)
        {
            var (items, total) = await _customers.SearchAsync(null, null, page, PageSize, cancellationToken);
            all.AddRange(items);
            if (items.Count == 0 || all.Count >= total)
            {
                break;
            }
            page++;
        }
        return all;
    }

    private async Task<decimal> ResolveArControlBalanceAsync(CancellationToken cancellationToken)
    {
        var balances = await _journals.GetAccountBalancesAsync(null, null, cancellationToken);
        var ar = balances.FirstOrDefault(b => b.AccountCode == AccountsReceivableCode);
        return ar is null ? 0m : ar.Debit - ar.Credit;
    }
}
