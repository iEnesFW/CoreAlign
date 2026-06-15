using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Customers.Statements;

public sealed class GetCustomerStatementQueryHandler : IRequestHandler<GetCustomerStatementQuery, CustomerStatementDto>
{
    private const int MaxLines = 5000;

    private readonly ICustomerRepository _customers;
    private readonly ICustomerLedgerRepository _ledger;
    private readonly ITenantContext _tenantContext;

    public GetCustomerStatementQueryHandler(
        ICustomerRepository customers,
        ICustomerLedgerRepository ledger,
        ITenantContext tenantContext)
    {
        _customers = customers;
        _ledger = ledger;
        _tenantContext = tenantContext;
    }

    public async Task<CustomerStatementDto> Handle(GetCustomerStatementQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();
        _tenantContext.EnsureSameTenant(customer.TenantId);

        var (from, to) = NormalizeRange(request.FromUtc, request.ToUtc);

        decimal openingBalance = 0m;
        if (from.HasValue)
        {
            var openingCutoff = from.Value.AddTicks(-1);
            openingBalance = await _ledger.GetBalanceAsOfAsync(customer.Id, openingCutoff, cancellationToken);
        }

        var rangeCount = await _ledger.CountByCustomerAsync(customer.Id, from, to, cancellationToken);
        if (rangeCount > MaxLines)
        {
            throw new CustomerStatementRangeTooLargeException(rangeCount, MaxLines);
        }

        var (rangeItems, _) = await _ledger.SearchByCustomerAsync(customer.Id, from, to, 1, MaxLines, cancellationToken);
        var ordered = rangeItems.OrderBy(e => e.OccurredAtUtc).ThenBy(e => e.Id).ToList();

        var running = openingBalance;
        var lines = new List<CustomerStatementLineDto>(ordered.Count);
        decimal totalDebit = 0m;
        decimal totalCredit = 0m;
        foreach (var entry in ordered)
        {
            var debit = entry.EntryType == LedgerEntryType.Debit ? entry.Amount : 0m;
            var credit = entry.EntryType == LedgerEntryType.Credit ? entry.Amount : 0m;
            running += entry.SignedAmount;
            totalDebit += debit;
            totalCredit += credit;
            lines.Add(new CustomerStatementLineDto(
                entry.OccurredAtUtc,
                entry.SourceType.ToString(),
                entry.SourceDocumentNumber ?? string.Empty,
                entry.Description,
                Math.Round(debit, 4),
                Math.Round(credit, 4),
                Math.Round(running, 4),
                entry.Currency));
        }

        return new CustomerStatementDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CustomerCode = customer.Code,
            Currency = customer.DefaultCurrency,
            FromUtc = from,
            ToUtc = to,
            OpeningBalance = Math.Round(openingBalance, 4),
            ClosingBalance = Math.Round(running, 4),
            TotalDebit = Math.Round(totalDebit, 4),
            TotalCredit = Math.Round(totalCredit, 4),
            Lines = lines,
        };
    }

    private static (DateTime? From, DateTime? To) NormalizeRange(DateTime? from, DateTime? to)
    {
        var f = from.HasValue ? DateTime.SpecifyKind(from.Value, DateTimeKind.Utc) : (DateTime?)null;
        var t = to.HasValue ? DateTime.SpecifyKind(to.Value, DateTimeKind.Utc) : (DateTime?)null;
        if (f.HasValue && t.HasValue && f > t)
        {
            (f, t) = (t, f);
        }
        return (f, t);
    }
}
