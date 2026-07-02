using CoreAlign.Application.Reports.DTOs;
using CoreAlign.Application.Reports.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Reports.Handlers;

public class GetCashPositionQueryHandler : IRequestHandler<GetCashPositionQuery, CashPositionReportDto>
{
    private readonly IJournalEntryRepository _journals;
    private readonly IBankAccountRepository _bankAccounts;

    public GetCashPositionQueryHandler(
        IJournalEntryRepository journals,
        IBankAccountRepository bankAccounts)
    {
        _journals = journals;
        _bankAccounts = bankAccounts;
    }

    public async Task<CashPositionReportDto> Handle(GetCashPositionQuery request, CancellationToken ct)
    {
        var asOf = request.AsOfUtc ?? DateTime.UtcNow;
        var rows = await _journals.GetAccountBalancesAsOfAsync(asOf, ct);

        decimal debitNet(string prefix) =>
            rows.Where(r => r.AccountCode.StartsWith(prefix, StringComparison.Ordinal))
                .Sum(r => r.Debit - r.Credit);
        decimal creditNet(string prefix) =>
            rows.Where(r => r.AccountCode.StartsWith(prefix, StringComparison.Ordinal))
                .Sum(r => r.Credit - r.Debit);

        var cashOnHand = debitNet("100");
        var bankBalance = debitNet("102");
        var customerAdvances = creditNet("340");

        var accounts = await _bankAccounts.ListAsync(true, ct);

        return new CashPositionReportDto
        {
            AsOfUtc = asOf,
            Currency = "TRY",
            CashOnHand = cashOnHand,
            BankBalance = bankBalance,
            TotalCash = cashOnHand + bankBalance,
            CustomerAdvances = customerAdvances,
            Accounts = accounts
                .Select(a => new BankAccountSummaryDto
                {
                    Id = a.Id,
                    AccountName = a.AccountName,
                    BankName = a.BankName,
                    Iban = a.Iban,
                    Currency = a.Currency,
                    OpeningBalance = a.OpeningBalance,
                    IsPrimary = a.IsPrimary,
                })
                .ToList(),
        };
    }
}
