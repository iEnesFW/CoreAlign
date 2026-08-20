using CoreAlign.Application.Accounting.Commands;
using CoreAlign.Application.Accounting.DTOs;
using CoreAlign.Application.Accounting.Mapping;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Accounting.Handlers;

/// <summary>
/// Shared constants + deterministic source keys for the statutory year-end
/// close / opening. The result accounts (690/590/591/570/580) are TDHP standards;
/// the deterministic ids carry the tenant so the same fiscal year never collides
/// across tenants and the SHA256-derived GUID is never <see cref="Guid.Empty"/>.
/// </summary>
internal static class YearEnd
{
    public const decimal LineEpsilon = 0.005m;

    public const string ProfitSummaryCode = "690"; // Dönem Kârı veya Zararı
    public const string NetProfitCode = "590";     // Dönem Net Kârı
    public const string NetLossCode = "591";        // Dönem Net Zararı (-)
    public const string RetainedProfitCode = "570"; // Geçmiş Yıllar Kârları
    public const string RetainedLossCode = "580";   // Geçmiş Yıllar Zararları (-)

    // Only the aggregation account (690) and the equity destinations (590/591) are
    // excluded from the P&L sweep. 691 (tax provision, Expense) and 692 (net result,
    // Revenue) ARE swept — ComputeNetIncome counts them as P&L, so leaving them on
    // the books would unbalance the post-closing balance sheet.
    public static readonly string[] ResultCodes = { "690", "590", "591" };

    public static Guid CloseId(Guid tenantId, int year) =>
        DeterministicGuid.From($"yearend-close:{tenantId}:{year}");

    public static Guid OpenId(Guid tenantId, int openingYear) =>
        DeterministicGuid.From($"yearend-open:{tenantId}:{openingYear}");

    public static DateTime CloseInstant(int year) =>
        new(year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

    public static DateTime LastInstantBefore(int year) =>
        new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(-1);

    public static DateTime OpenInstant(int closedYear) =>
        new(closedYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Signed natural balance of an account from its own normal side — the exact
    /// convention <c>FinancialStatementMath.ToNaturalRows</c> and the trial balance
    /// use, applied here to the raw cumulative aggregates.
    /// </summary>
    public static decimal Natural(AccountBalanceRow row, GLAccount account)
    {
        var debit = Math.Round(row.Debit, 4, MidpointRounding.ToEven);
        var credit = Math.Round(row.Credit, 4, MidpointRounding.ToEven);
        return account.NormalSide == NormalSide.Debit ? debit - credit : credit - debit;
    }

    public static bool IsResultAccount(string code) =>
        ResultCodes.Contains(code);

    public static bool IsProfitAndLoss(GLAccount account) =>
        account.Type is AccountType.Revenue or AccountType.Expense or AccountType.CostOfGoodsSold
        && !IsResultAccount(account.Code);

    public static bool IsBalanceSheet(GLAccount account) =>
        account.Type is AccountType.Asset or AccountType.Liability or AccountType.Equity or AccountType.Memorandum;
}

/// <summary>
/// Accumulates journal lines and the running debit/credit totals so a sub-cent
/// rounding residual from many leaves can be absorbed onto the largest line of the
/// heavier side before <c>Post()</c> — the same residual-absorb contract
/// <c>GLPostingService</c> honours, kept migration-free and reusable here.
/// </summary>
internal sealed class YearEndLineBuilder
{
    private readonly List<(GLAccount Account, decimal Debit, decimal Credit, string? Description)> _lines = new();

    public int Count => _lines.Count;

    public void Book(GLAccount account, decimal naturalAmount, NormalSide naturalSide, string? description = null)
    {
        if (Math.Abs(naturalAmount) <= YearEnd.LineEpsilon) return;

        // A positive natural amount sits on the account's own side; a negative one
        // (e.g. a contra-revenue carrying a debit) flips to the opposite side so a
        // single line never has both Debit and Credit > 0 (JournalLine forbids it).
        var onNaturalSide = naturalAmount > 0m;
        var amount = Math.Abs(naturalAmount);
        var side = onNaturalSide ? naturalSide : Opposite(naturalSide);
        Add(account, amount, side, description);
    }

    public void Add(GLAccount account, decimal amount, NormalSide side, string? description = null)
    {
        if (amount <= YearEnd.LineEpsilon) return;
        var rounded = Math.Round(amount, 4, MidpointRounding.ToEven);
        if (side == NormalSide.Debit)
        {
            _lines.Add((account, rounded, 0m, description));
        }
        else
        {
            _lines.Add((account, 0m, rounded, description));
        }
    }

    private static NormalSide Opposite(NormalSide side) =>
        side == NormalSide.Debit ? NormalSide.Credit : NormalSide.Debit;

    public void Flush(JournalEntry entry)
    {
        AbsorbResidual();
        foreach (var l in _lines)
        {
            entry.AddLine(l.Account.Id, l.Account.Code, l.Account.Name, l.Debit, l.Credit, l.Account.Currency, l.Description);
        }
    }

    private void AbsorbResidual()
    {
        if (_lines.Count == 0) return;
        var residual = Math.Round(_lines.Sum(l => l.Debit) - _lines.Sum(l => l.Credit), 4, MidpointRounding.ToEven);
        if (residual == 0m) return;

        var tolerance = Math.Max(0.01m, _lines.Count * 0.0001m);
        if (Math.Abs(residual) > tolerance) return; // genuine imbalance — let Post() throw

        if (residual > 0m)
        {
            var i = LargestIndex(byCredit: true);
            _lines[i] = (_lines[i].Account, _lines[i].Debit, _lines[i].Credit + residual, _lines[i].Description);
        }
        else
        {
            var i = LargestIndex(byCredit: false);
            _lines[i] = (_lines[i].Account, _lines[i].Debit - residual, _lines[i].Credit, _lines[i].Description);
        }
    }

    private int LargestIndex(bool byCredit)
    {
        var idx = 0;
        var max = -1m;
        for (var i = 0; i < _lines.Count; i++)
        {
            var v = byCredit ? _lines[i].Credit : _lines[i].Debit;
            if (v > max) { max = v; idx = i; }
        }
        return idx;
    }
}

public class CloseFiscalYearHandler : IRequestHandler<CloseFiscalYearCommand, YearEndEntryDto>
{
    private readonly IJournalEntryRepository _journals;
    private readonly IGLAccountRepository _accounts;
    private readonly IAccountingPeriodRepository _periods;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public CloseFiscalYearHandler(
        IJournalEntryRepository journals,
        IGLAccountRepository accounts,
        IAccountingPeriodRepository periods,
        IDocumentSequenceRepository sequences,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _journals = journals;
        _accounts = accounts;
        _periods = periods;
        _sequences = sequences;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<YearEndEntryDto> Handle(CloseFiscalYearCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var closeId = YearEnd.CloseId(tenantId, c.Year);

        // Idempotency: a non-reversed close already on the books is a no-op. A
        // reversed close is treated as absent so a corrected close can re-post.
        var existing = await _journals.GetActiveBySourceAsync(JournalSourceType.Manual, closeId, ct);
        if (existing is not null)
        {
            return new YearEndEntryDto(c.Year, AccountingMapper.ToDto(existing), NetOf(existing), AlreadyExisted: true);
        }

        // Soft business gate: every monthly period of the year should be Closed (or
        // Locked) first. Optional — when no monthly periods exist for the year the
        // gate is a no-op, mirroring CreateJournalEntryHandler's optional period.
        var periods = await _periods.ListAsync(c.Year, ct);
        if (periods.Any(p => p.Status == AccountingPeriodStatus.Open))
        {
            throw new YearNotReadyForCloseException(c.Year);
        }

        var dec31 = YearEnd.CloseInstant(c.Year);
        var accounts = await _accounts.GetAllAsync(ct);
        var byId = accounts.ToDictionary(a => a.Id);
        var byCode = accounts.GroupBy(a => a.Code).ToDictionary(g => g.Key, g => g.First());

        // WHY the earlier years have to be swept first: the sweep zeroes each P&L leaf at its
        // FROM-INCEPTION balance, so closing a year while a previous one is still open rolls that
        // year's result into this one — 590 would report two years of profit and the year it
        // belonged to would never show its own. The income statement, which is date-ranged, would
        // then disagree with the closing entry. Detected directly rather than by assuming the
        // prior year exists: a tenant in its first year has nothing before the start.
        var openingPnl = await _journals.GetAccountBalancesAsOfAsync(YearEnd.LastInstantBefore(c.Year), ct);
        if (openingPnl.Any(row =>
                byId.TryGetValue(row.AccountId, out var earlier)
                && YearEnd.IsProfitAndLoss(earlier)
                && Math.Abs(YearEnd.Natural(row, earlier)) > YearEnd.LineEpsilon))
        {
            throw new EarlierFiscalYearNotClosedException(c.Year);
        }

        var balances = await _journals.GetAccountBalancesAsOfAsync(dec31, ct);

        var builder = new YearEndLineBuilder();
        var totalRevenue = 0m; // credit-normal P&L swept (revenue/gains)
        var totalExpense = 0m; // debit-normal P&L swept (expense/COGS)

        foreach (var row in balances)
        {
            if (!byId.TryGetValue(row.AccountId, out var account) || !YearEnd.IsProfitAndLoss(account)) continue;

            var natural = YearEnd.Natural(row, account);
            if (Math.Abs(natural) <= YearEnd.LineEpsilon) continue;

            // Zero each leaf by booking its exact contra; accumulate the signed
            // natural to the matching 690 side so 690 mirrors every leaf.
            if (account.NormalSide == NormalSide.Credit)
            {
                builder.Book(account, natural, NormalSide.Debit);
                totalRevenue += natural;
            }
            else
            {
                builder.Book(account, natural, NormalSide.Credit);
                totalExpense += natural;
            }
        }

        var profitSummary = byCode[YearEnd.ProfitSummaryCode];
        // 690's two aggregate legs: credited by total revenue swept in, debited by
        // total expense/COGS swept in. Mirrors every leaf's contra so the sweep
        // balances on its own.
        builder.Add(profitSummary, totalRevenue, NormalSide.Credit, "Gelir hesaplarının devri");
        builder.Add(profitSummary, totalExpense, NormalSide.Debit, "Gider hesaplarının devri");

        // Transfer 690 -> 590/591 so 690 itself ends at zero.
        var net690 = totalRevenue - totalExpense;
        if (net690 > YearEnd.LineEpsilon)
        {
            builder.Add(profitSummary, net690, NormalSide.Debit, "Dönem net kârının devri");
            builder.Add(byCode[YearEnd.NetProfitCode], net690, NormalSide.Credit, "Dönem net kârı");
        }
        else if (net690 < -YearEnd.LineEpsilon)
        {
            var loss = Math.Abs(net690);
            builder.Add(byCode[YearEnd.NetLossCode], loss, NormalSide.Debit, "Dönem net zararı");
            builder.Add(profitSummary, loss, NormalSide.Credit, "Dönem net zararının devri");
        }

        if (builder.Count < 2)
        {
            // Nothing to close (no P&L movement in the year) — record an explicit
            // no-op result rather than posting an invalid single-line entry.
            return new YearEndEntryDto(c.Year, EmptyEntryDto(dec31, c.Year), 0m, AlreadyExisted: false);
        }

        var number = await _sequences.ConsumeAsync(DocumentSequenceType.JournalNumber, dec31, ct);
        var entry = new JournalEntry(number, dec31, dec31, JournalEntryType.Kapanis, $"Yıl sonu kapanış fişi {c.Year}", $"KAP-{c.Year}")
        {
            TenantId = tenantId,
        };
        entry.AssignSource(JournalSourceType.Manual, closeId, $"KAP-{c.Year}");
        builder.Flush(entry);

        // The closing fiş is the one entry legally permitted to post into the closed
        // fiscal year, so it intentionally bypasses the period gate the ordinary
        // CreateJournalEntryHandler applies.
        entry.Post(c.PostedByUserId ?? Guid.Empty);
        await _journals.AddAsync(entry, ct);
        await _uow.SaveChangesAsync(ct);

        return new YearEndEntryDto(c.Year, AccountingMapper.ToDto(entry), net690, AlreadyExisted: false);
    }

    private static decimal NetOf(JournalEntry entry)
    {
        var profit = entry.Lines.Where(l => l.AccountCode == YearEnd.NetProfitCode).Sum(l => l.Credit - l.Debit);
        var loss = entry.Lines.Where(l => l.AccountCode == YearEnd.NetLossCode).Sum(l => l.Credit - l.Debit);
        return profit + loss;
    }

    private static JournalEntryDto EmptyEntryDto(DateTime date, int year) => new()
    {
        Number = string.Empty,
        EntryDate = date,
        PostingDate = date,
        Type = JournalEntryType.Kapanis,
        Status = JournalEntryStatus.Draft,
        Description = $"Yıl sonu kapanış fişi {year} (hareket yok)",
    };
}

public class OpenFiscalYearHandler : IRequestHandler<OpenFiscalYearCommand, YearEndEntryDto>
{
    private readonly IJournalEntryRepository _journals;
    private readonly IGLAccountRepository _accounts;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public OpenFiscalYearHandler(
        IJournalEntryRepository journals,
        IGLAccountRepository accounts,
        IDocumentSequenceRepository sequences,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _journals = journals;
        _accounts = accounts;
        _sequences = sequences;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<YearEndEntryDto> Handle(OpenFiscalYearCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var openingYear = c.Year + 1;
        var openId = YearEnd.OpenId(tenantId, openingYear);

        var existing = await _journals.GetActiveBySourceAsync(JournalSourceType.Manual, openId, ct);
        if (existing is not null)
        {
            return new YearEndEntryDto(openingYear, AccountingMapper.ToDto(existing), 0m, AlreadyExisted: true);
        }

        // Opening cannot precede close — an ACTIVE (posted, un-reversed) close of
        // Year must already exist. A reversed-and-not-re-closed year is treated as
        // not closed, so the opening is correctly blocked.
        var closeId = YearEnd.CloseId(tenantId, c.Year);
        if (await _journals.GetActiveBySourceAsync(JournalSourceType.Manual, closeId, ct) is null)
        {
            throw new FiscalYearCloseNotFoundException(c.Year);
        }

        var dec31 = YearEnd.CloseInstant(c.Year);
        var jan1 = YearEnd.OpenInstant(c.Year);
        var accounts = await _accounts.GetAllAsync(ct);
        var byId = accounts.ToDictionary(a => a.Id);
        var byCode = accounts.GroupBy(a => a.Code).ToDictionary(g => g.Key, g => g.First());

        // Post-close as-of: 6xx/690 are zero, 590/591 carry the result, every
        // balance-sheet account sits at its true closing balance.
        var balances = await _journals.GetAccountBalancesAsOfAsync(dec31, ct);

        var builder = new YearEndLineBuilder();
        var rolledRetained = 0m;

        foreach (var row in balances)
        {
            if (!byId.TryGetValue(row.AccountId, out var account) || !YearEnd.IsBalanceSheet(account)) continue;

            var natural = YearEnd.Natural(row, account);
            if (Math.Abs(natural) <= YearEnd.LineEpsilon) continue;

            // Roll the result accounts into retained earnings: the new year's 590/591
            // must open empty, so a 590 profit becomes a 570 credit and a 591 loss a
            // 580 debit instead of carrying 590/591 forward.
            if (account.Code == YearEnd.NetProfitCode)
            {
                builder.Add(byCode[YearEnd.RetainedProfitCode], natural, NormalSide.Credit, "Dönem kârının geçmiş yıllar kârlarına devri");
                rolledRetained += natural;
                continue;
            }
            if (account.Code == YearEnd.NetLossCode)
            {
                builder.Add(byCode[YearEnd.RetainedLossCode], Math.Abs(natural), NormalSide.Debit, "Dönem zararının geçmiş yıllar zararlarına devri");
                rolledRetained -= Math.Abs(natural);
                continue;
            }

            builder.Book(account, natural, account.NormalSide);
        }

        if (builder.Count < 2)
        {
            return new YearEndEntryDto(openingYear, EmptyOpeningDto(jan1, openingYear), 0m, AlreadyExisted: false);
        }

        var number = await _sequences.ConsumeAsync(DocumentSequenceType.JournalNumber, jan1, ct);
        var entry = new JournalEntry(number, jan1, jan1, JournalEntryType.Acilis, $"Açılış fişi {openingYear}", $"ACL-{openingYear}")
        {
            TenantId = tenantId,
        };
        entry.AssignSource(JournalSourceType.Manual, openId, $"ACL-{openingYear}");
        builder.Flush(entry);

        entry.Post(c.PostedByUserId ?? Guid.Empty);
        await _journals.AddAsync(entry, ct);
        await _uow.SaveChangesAsync(ct);

        return new YearEndEntryDto(openingYear, AccountingMapper.ToDto(entry), rolledRetained, AlreadyExisted: false);
    }

    private static JournalEntryDto EmptyOpeningDto(DateTime date, int year) => new()
    {
        Number = string.Empty,
        EntryDate = date,
        PostingDate = date,
        Type = JournalEntryType.Acilis,
        Status = JournalEntryStatus.Draft,
        Description = $"Açılış fişi {year} (bakiye yok)",
    };
}

public class ReverseFiscalYearCloseHandler : IRequestHandler<ReverseFiscalYearCloseCommand, YearEndEntryDto>
{
    private readonly IJournalEntryRepository _journals;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public ReverseFiscalYearCloseHandler(
        IJournalEntryRepository journals,
        IDocumentSequenceRepository sequences,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _journals = journals;
        _sequences = sequences;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<YearEndEntryDto> Handle(ReverseFiscalYearCloseCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var closeId = YearEnd.CloseId(tenantId, c.Year);

        // Active = Posted and not already reversed. A missing active close means
        // either no close ever ran, or it was already reversed — both block here.
        var original = await _journals.GetActiveBySourceAsync(JournalSourceType.Manual, closeId, ct)
            ?? throw new FiscalYearCloseNotFoundException(c.Year);

        // A close consumed by the next year's opening must not be reversed — the
        // açılış already rolled 590->570, so reversing would double-book retained
        // earnings and unbalance equity.
        var openId = YearEnd.OpenId(tenantId, c.Year + 1);
        if (await _journals.ExistsForSourceAsync(JournalSourceType.Manual, openId, ct))
        {
            throw new FiscalYearAlreadyOpenedException(c.Year);
        }

        var postingDate = original.PostingDate;
        var revNumber = await _sequences.ConsumeAsync(DocumentSequenceType.JournalNumber, postingDate, ct);
        var reversal = new JournalEntry(
            revNumber,
            postingDate,
            postingDate,
            original.Type,
            $"Reversal of {original.Number}",
            original.Reference)
        {
            TenantId = tenantId,
        };
        reversal.MarkAsReversalOf(original.Id);

        foreach (var l in original.Lines.OrderBy(l => l.LineNumber))
        {
            reversal.AddLine(
                l.AccountId, l.AccountCode, l.AccountName,
                debit: l.Credit, credit: l.Debit,
                currency: l.Currency, description: l.Description,
                costCenter: l.CostCenter, project: l.Project,
                foreignAmount: l.ForeignAmount, exchangeRate: l.ExchangeRate);
        }
        reversal.Post(c.ReversedByUserId ?? Guid.Empty);

        // The original Kapanış is intentionally LEFT Posted: the cumulative as-of
        // aggregate counts only Posted entries, so keeping both the close and its
        // exact contra Posted nets the sweep back to zero (6xx re-inflated, 590/591
        // back to zero). The reversal's ReversalOfId is what marks the close as
        // consumed, so a re-close can re-post under the same deterministic id.
        await _journals.AddAsync(reversal, ct);
        await _uow.SaveChangesAsync(ct);

        return new YearEndEntryDto(c.Year, AccountingMapper.ToDto(reversal), 0m, AlreadyExisted: false);
    }
}
