using CoreAlign.Application.Accounting.DTOs;
using CoreAlign.Application.Accounting.Queries;
using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Accounting.Handlers;

/// <summary>
/// Shared building blocks for the server-side financial statements. A natural
/// balance is the account's signed position in its own normal direction: a
/// debit-normal account reads debit − credit, a credit-normal account the
/// inverse — so every account prints positive when it carries its natural side.
/// This is the exact convention <c>GetTrialBalanceHandler</c> uses.
/// </summary>
internal static class FinancialStatementMath
{
    /// <summary>Below this magnitude a line is dropped from a statement section.</summary>
    public const decimal LineEpsilon = 0.005m;

    internal sealed record NaturalRow(Guid AccountId, string AccountCode, string AccountName, AccountType Type, decimal Amount);

    /// <summary>
    /// Joins raw debit/credit aggregates to the chart and computes each account's
    /// signed natural balance, mirroring <c>GetTrialBalanceHandler</c> (banker's
    /// rounding to 4 places). Memorandum (Nazım 8xx-9xx) accounts are carried
    /// through; callers exclude them when building the printed sections.
    /// </summary>
    public static IReadOnlyList<NaturalRow> ToNaturalRows(
        IReadOnlyList<AccountBalanceRow> aggregates,
        IReadOnlyDictionary<Guid, GLAccount> accountsById)
    {
        var rows = new List<NaturalRow>(aggregates.Count);
        foreach (var r in aggregates)
        {
            accountsById.TryGetValue(r.AccountId, out var account);
            var debit = Math.Round(r.Debit, 4, MidpointRounding.ToEven);
            var credit = Math.Round(r.Credit, 4, MidpointRounding.ToEven);
            var amount = account?.NormalSide == NormalSide.Debit ? debit - credit : credit - debit;
            rows.Add(new NaturalRow(
                r.AccountId,
                r.AccountCode,
                r.AccountName,
                account?.Type ?? AccountType.Asset,
                amount));
        }
        return rows;
    }

    /// <summary>Builds a statement section for the given account types — filtered, sorted, totalled.</summary>
    public static StatementSectionDto SectionFor(IReadOnlyList<NaturalRow> rows, params AccountType[] types)
    {
        var lines = rows
            .Where(r => types.Contains(r.Type))
            .Where(r => Math.Abs(r.Amount) > LineEpsilon)
            .OrderBy(r => r.AccountCode, StringComparer.Ordinal)
            .Select(r => new StatementLineDto(r.AccountId, r.AccountCode, r.AccountName, r.Amount))
            .ToList();
        var total = lines.Sum(l => l.Amount);
        return new StatementSectionDto(lines, total);
    }

    /// <summary>
    /// Net income from natural rows: Σ(Revenue) − Σ(COGS) − Σ(Expense). Shared by
    /// the income-statement handler and the balance-sheet equity fold so the two
    /// reports cannot drift apart (CLAUDE.md — no logic duplication).
    /// </summary>
    public static decimal ComputeNetIncome(IReadOnlyList<NaturalRow> rows)
    {
        var revenue = rows.Where(r => r.Type == AccountType.Revenue).Sum(r => r.Amount);
        var cogs = rows.Where(r => r.Type == AccountType.CostOfGoodsSold).Sum(r => r.Amount);
        var opex = rows.Where(r => r.Type == AccountType.Expense).Sum(r => r.Amount);
        return revenue - cogs - opex;
    }
}

public class GetBalanceSheetHandler : IRequestHandler<GetBalanceSheetQuery, BalanceSheetReportDto>
{
    private readonly IJournalEntryRepository _journals;
    private readonly IGLAccountRepository _accounts;
    private readonly ITenantContext _tenant;

    private readonly IFiscalYearResolver _fiscalYear;

    public GetBalanceSheetHandler(
        IJournalEntryRepository journals,
        IGLAccountRepository accounts,
        ITenantContext tenant,
        IFiscalYearResolver fiscalYear)
    {
        _journals = journals;
        _accounts = accounts;
        _tenant = tenant;
        _fiscalYear = fiscalYear;
    }

    public async Task<BalanceSheetReportDto> Handle(GetBalanceSheetQuery q, CancellationToken ct)
    {
        var accounts = await _accounts.GetAllAsync(ct);
        var byId = accounts.ToDictionary(a => a.Id);
        var tenantId = _tenant.RequireTenantId();

        // Each fiscal year's books begin at its açılış (opening fiş, dated Jan 1).
        // If an opening exists for AsOf.Year, the balance sheet reads from that
        // opening forward — the açılış already carries every account's true opening
        // balance, so summing it together with the prior-year detail would
        // double-count. Without an opening (the first/legacy year) the cumulative
        // from-inception sum is the correct carry-forward.
        // The açılış/kapanış are keyed by FISCAL year, so the as-of date has to be mapped through
        // the tenant's start month — reading its calendar year would look up the wrong pair for a
        // tenant whose books do not start in January, and the equity fold would silently double
        // count or drop a year's result.
        var startMonth = await _fiscalYear.GetStartMonthAsync(ct);
        var fiscalYear = FiscalYear.Containing(q.AsOf, startMonth);
        var yearStart = fiscalYear.StartUtc;
        var openId = YearEnd.OpenId(tenantId, fiscalYear.Year);
        var openingExists = await _journals.ExistsForSourceAsync(JournalSourceType.Manual, openId, ct);

        var aggregates = openingExists
            ? await _journals.GetAccountBalancesAsync(yearStart, q.AsOf, ct)
            : await _journals.GetAccountBalancesAsOfAsync(q.AsOf, ct);
        var rows = FinancialStatementMath.ToNaturalRows(aggregates, byId);

        var assets = FinancialStatementMath.SectionFor(rows, AccountType.Asset);
        var liabilities = FinancialStatementMath.SectionFor(rows, AccountType.Liability);
        // Equity now naturally carries 570/580 (rolled retained, from the açılış)
        // and 590/591 of any partially-closed year as REAL lines, so they are no
        // longer a synthetic plug.
        var equity = FinancialStatementMath.SectionFor(rows, AccountType.Equity);

        // Fold the net income of EVERY still-unclosed year: a closed year's result
        // is in 590 → 570/580 inside equity.Total (and its 6xx are swept to 0), so it
        // never contributes here; an unclosed year's P&L still lives on its 6xx
        // accounts with no equity counterpart, so it must be folded or the sheet
        // strands that net income on the asset side. `rows` already carries the
        // correct scope: the year window when an açılış exists (prior years rolled to
        // 570/580), else the from-inception cumulative (closed years' 6xx = 0). So
        // ComputeNetIncome(rows) is exactly the unclosed-P&L fold in both branches —
        // keeping Assets == Liab + Equity at ANY as-of, before and after any close.
        var closeId = YearEnd.CloseId(tenantId, fiscalYear.Year);
        var closeExists = await _journals.GetActiveBySourceAsync(JournalSourceType.Manual, closeId, ct) is not null;

        var openYearEarnings = closeExists ? 0m : FinancialStatementMath.ComputeNetIncome(rows);

        var totalLiabilitiesAndEquity = liabilities.Total + equity.Total + openYearEarnings;
        var variance = Math.Round(assets.Total - totalLiabilitiesAndEquity, 4, MidpointRounding.ToEven);
        var isBalanced = Math.Abs(variance) < 0.01m;

        return new BalanceSheetReportDto(
            q.AsOf,
            assets,
            liabilities,
            equity,
            Math.Round(openYearEarnings, 4, MidpointRounding.ToEven),
            // 570/580 print as real equity lines now — no separate synthetic prior plug.
            0m,
            Math.Round(totalLiabilitiesAndEquity, 4, MidpointRounding.ToEven),
            isBalanced,
            variance);
    }
}

public class GetIncomeStatementHandler : IRequestHandler<GetIncomeStatementQuery, IncomeStatementReportDto>
{
    private readonly IJournalEntryRepository _journals;
    private readonly IGLAccountRepository _accounts;

    public GetIncomeStatementHandler(IJournalEntryRepository journals, IGLAccountRepository accounts)
    {
        _journals = journals;
        _accounts = accounts;
    }

    public async Task<IncomeStatementReportDto> Handle(GetIncomeStatementQuery q, CancellationToken ct)
    {
        // P&L over a range is correctly movement-only — revenue/expense reset each
        // period — so the existing period-bounded aggregate is the right source.
        var aggregates = await _journals.GetAccountBalancesAsync(q.FromDate, q.ToDate, ct);
        var accounts = await _accounts.GetAllAsync(ct);
        var byId = accounts.ToDictionary(a => a.Id);
        var rows = FinancialStatementMath.ToNaturalRows(aggregates, byId);

        var revenue = FinancialStatementMath.SectionFor(rows, AccountType.Revenue);
        var cogs = FinancialStatementMath.SectionFor(rows, AccountType.CostOfGoodsSold);
        var opex = FinancialStatementMath.SectionFor(rows, AccountType.Expense);

        var grossProfit = revenue.Total - cogs.Total;
        var netIncome = grossProfit - opex.Total;

        return new IncomeStatementReportDto(
            q.FromDate,
            q.ToDate,
            revenue,
            cogs,
            opex,
            grossProfit,
            netIncome);
    }
}

public class GetSubledgerReconciliationHandler : IRequestHandler<GetSubledgerReconciliationQuery, ReconciliationReportDto>
{
    private readonly IJournalEntryRepository _journals;
    private readonly ICustomerLedgerRepository _customerLedger;
    private readonly IVendorLedgerRepository _vendorLedger;
    private readonly IGLAccountRepository _accounts;

    public GetSubledgerReconciliationHandler(
        IJournalEntryRepository journals,
        ICustomerLedgerRepository customerLedger,
        IVendorLedgerRepository vendorLedger,
        IGLAccountRepository accounts)
    {
        _journals = journals;
        _customerLedger = customerLedger;
        _vendorLedger = vendorLedger;
        _accounts = accounts;
    }

    public async Task<ReconciliationReportDto> Handle(GetSubledgerReconciliationQuery q, CancellationToken ct)
    {
        var arCode = GLPostingDefaults.CodeFor(GLPostingKey.AccountsReceivable) ?? "120";
        var apCode = GLPostingDefaults.CodeFor(GLPostingKey.AccountsPayable) ?? "320";
        var cashCode = GLPostingDefaults.CodeFor(GLPostingKey.Cash) ?? "100";
        var bankCode = GLPostingDefaults.CodeFor(GLPostingKey.Bank) ?? "102";

        var accounts = await _accounts.GetAllAsync(ct);
        var byId = accounts.ToDictionary(a => a.Id);

        var aggregates = await _journals.GetAccountBalancesAsOfAsync(q.AsOf, ct);
        var rows = FinancialStatementMath.ToNaturalRows(aggregates, byId);
        var glByCode = rows
            .GroupBy(r => r.AccountCode)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        decimal GlNatural(params string[] codes) =>
            Math.Round(codes.Sum(c => glByCode.TryGetValue(c, out var v) ? v : 0m), 4, MidpointRounding.ToEven);

        string NameFor(string code, string fallback) =>
            accounts.FirstOrDefault(a => a.Code == code)?.Name ?? fallback;

        var lines = new List<ReconciliationLineDto>();

        // AR 120 vs customer subledger (debit − credit).
        var arGl = GlNatural(arCode);
        var arSub = Math.Round(await _customerLedger.GetTotalBalanceAsOfAsync(q.AsOf, ct), 4, MidpointRounding.ToEven);
        lines.Add(BuildLine(arCode, NameFor(arCode, "Alıcılar"), "CustomerLedger", arGl, arSub));

        // AP 320 vs vendor subledger (credit − debit, "we owe"). The GL control
        // account 320 is credit-normal, so its natural balance already matches the
        // vendor "we owe" sign convention — no inversion needed.
        var apGl = GlNatural(apCode);
        var apSub = Math.Round(await _vendorLedger.GetTotalBalanceAsOfAsync(q.AsOf, ct), 4, MidpointRounding.ToEven);
        lines.Add(BuildLine(apCode, NameFor(apCode, "Satıcılar"), "VendorLedger", apGl, apSub));

        // AR + AP are the only real subledgers — AllReconciled reflects ONLY these,
        // computed before the informational cash line so a GL-only cash variance can
        // never mask (or be masked by) a genuine AR/AP mismatch.
        var allReconciled = lines.All(l => l.IsReconciled);

        // Cash 100 + Bank 102 — no dedicated cash subledger module exists today, so
        // this is reported as GL-only/informational with a zero subledger figure and
        // variance equal to the GL balance until a cash register is wired in.
        var cashGl = GlNatural(cashCode, bankCode);
        lines.Add(new ReconciliationLineDto(
            $"{cashCode}+{bankCode}",
            "Kasa + Bankalar",
            "GL-only (no cash subledger)",
            cashGl,
            0m,
            cashGl,
            IsReconciled: true));

        return new ReconciliationReportDto(q.AsOf, lines, allReconciled);
    }

    private static ReconciliationLineDto BuildLine(
        string controlCode, string controlName, string subledger, decimal glBalance, decimal subledgerBalance)
    {
        var variance = Math.Round(glBalance - subledgerBalance, 4, MidpointRounding.ToEven);
        return new ReconciliationLineDto(
            controlCode,
            controlName,
            subledger,
            glBalance,
            subledgerBalance,
            variance,
            IsReconciled: Math.Abs(variance) < 0.01m);
    }
}
