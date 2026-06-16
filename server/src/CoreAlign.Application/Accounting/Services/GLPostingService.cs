using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Accounting.Services;

/// <summary>
/// A posting leg. <see cref="Key"/> resolves to an account through the tenant's
/// role→account mapping; set <see cref="AccountCodeOverride"/> to instead target
/// an exact GL account code, bypassing the mapping. The override exists so a
/// reversal can mirror the precise account a prior entry actually booked to —
/// even when the tenant has since remapped that role — instead of re-resolving
/// (and possibly losing) the leg.
/// </summary>
public sealed record GLPostingLine(GLPostingKey Key, decimal Debit, decimal Credit, string? Description = null, string? AccountCodeOverride = null);

public sealed record GLPostingRequest(
    JournalSourceType SourceType,
    Guid SourceDocumentId,
    string SourceDocumentNumber,
    DateTime PostingDate,
    JournalEntryType EntryType,
    string Description,
    IReadOnlyList<GLPostingLine> Lines,
    string Currency = "TRY",
    decimal ExchangeRate = 1m);

/// <summary>
/// Standard TDHP account code each posting role resolves to when a tenant has
/// not overridden it via <see cref="GLPostingMapping"/>.
/// </summary>
public static class GLPostingDefaults
{
    public static string? CodeFor(GLPostingKey key) => key switch
    {
        GLPostingKey.AccountsReceivable => "120",
        GLPostingKey.SalesRevenue => "600",
        GLPostingKey.OutputVat => "391",
        GLPostingKey.Cash => "100",
        GLPostingKey.Bank => "102",
        GLPostingKey.AccountsPayable => "320",
        GLPostingKey.InputVat => "191",
        GLPostingKey.Inventory => "153",
        GLPostingKey.CostOfGoodsSold => "621",
        GLPostingKey.GoodsReceiptClearing => "322",
        GLPostingKey.PurchaseExpense => "632",
        GLPostingKey.InventoryWriteOff => "689",
        GLPostingKey.WithholdingReceivable => "193",
        GLPostingKey.PurchasePriceVariance => "631",
        GLPostingKey.FxGain => "646",
        GLPostingKey.FxLoss => "656",
        _ => null,
    };
}

/// <summary>
/// Builds the two-line cash movement shared by every payment posting: a cash/bank
/// account against a control account (AR for customers, AP for vendors). When
/// <paramref name="cashIsDebit"/> the cash account is debited (money in / customer
/// receipt), otherwise it is credited (money out / vendor payment, refund).
/// </summary>
public static class PaymentGLLines
{
    public static IReadOnlyList<GLPostingLine> CashMovement(
        GLPostingKey cashKey, GLPostingKey controlKey, decimal amount, bool cashIsDebit) =>
        cashIsDebit
            ? new[]
            {
                new GLPostingLine(cashKey, amount, 0m),
                new GLPostingLine(controlKey, 0m, amount),
            }
            : new[]
            {
                new GLPostingLine(controlKey, amount, 0m),
                new GLPostingLine(cashKey, 0m, amount),
            };
}

public enum GLPostingResult
{
    Posted = 0,
    SkippedDuplicate = 1,
    SkippedClosedPeriod = 2,
    SkippedUnmapped = 3,
    SkippedEmpty = 4,
}

public interface IGLPostingService
{
    /// <summary>
    /// Posts a balanced journal entry for a sub-ledger event and reports the
    /// outcome. Adds the entry to the ambient unit of work but does NOT save —
    /// the outbox processor owns the SaveChanges so a posting failure (e.g. a
    /// journal-number clash) can be retried without touching business data.
    /// Returns a Skipped* result instead of throwing for duplicate / closed
    /// period / unmapped account so the caller can record it.
    /// </summary>
    Task<GLPostingResult> PostAsync(GLPostingRequest request, CancellationToken cancellationToken = default);
}

public class GLPostingService : IGLPostingService
{
    private readonly IJournalEntryRepository _journals;
    private readonly IGLAccountRepository _accounts;
    private readonly IGLPostingMappingRepository _mappings;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly IAccountingPeriodRepository _periods;

    public GLPostingService(
        IJournalEntryRepository journals,
        IGLAccountRepository accounts,
        IGLPostingMappingRepository mappings,
        IDocumentSequenceRepository sequences,
        IAccountingPeriodRepository periods)
    {
        _journals = journals;
        _accounts = accounts;
        _mappings = mappings;
        _sequences = sequences;
        _periods = periods;
    }

    public async Task<GLPostingResult> PostAsync(GLPostingRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0) return GLPostingResult.SkippedEmpty;

        // Idempotency — a single business document books to the GL exactly once.
        if (await _journals.ExistsForSourceAsync(request.SourceType, request.SourceDocumentId, cancellationToken))
        {
            return GLPostingResult.SkippedDuplicate;
        }

        // Period gate. A closed period must not break the originating action, so
        // the entry is deferred (replayable) rather than thrown.
        var period = await _periods.GetByDateAsync(request.PostingDate, cancellationToken);
        if (period is not null && period.IsClosed) return GLPostingResult.SkippedClosedPeriod;

        // Load the tenant's overrides + chart once (two queries) instead of a
        // per-line round-trip — account resolution below is then pure in-memory.
        var overrides = (await _mappings.ListAsync(cancellationToken))
            .ToDictionary(m => m.PostingKey, m => m.AccountCode);
        var accountsByCode = (await _accounts.GetAllAsync(cancellationToken))
            .GroupBy(a => a.Code)
            .ToDictionary(g => g.Key, g => g.First());

        // Resolve every account up front. A single unresolved account aborts the
        // whole posting because a partial entry could never balance.
        var resolved = new List<(GLPostingLine Line, GLAccount Account)>(request.Lines.Count);
        foreach (var line in request.Lines)
        {
            var debit = Math.Round(line.Debit, 4, MidpointRounding.ToEven);
            var credit = Math.Round(line.Credit, 4, MidpointRounding.ToEven);
            if (debit <= 0m && credit <= 0m) continue; // zero line (e.g. tax-free) — drop it

            var account = Resolve(line, overrides, accountsByCode);
            if (account is null) return GLPostingResult.SkippedUnmapped;

            resolved.Add((line with { Debit = debit, Credit = credit }, account));
        }
        if (resolved.Count < 2) return GLPostingResult.SkippedEmpty;

        var number = await NextJournalNumberAsync(request.PostingDate, cancellationToken);
        var entry = new JournalEntry(
            number,
            request.PostingDate,
            request.PostingDate,
            request.EntryType,
            request.Description,
            request.SourceDocumentNumber);
        entry.AssignSource(request.SourceType, request.SourceDocumentId, request.SourceDocumentNumber);

        // The legal ledger is kept in the base/reporting currency, so foreign-
        // currency documents are translated at the document's rate; the original
        // amount + rate are recorded on each line as a memo.
        var rate = request.ExchangeRate <= 0m ? 1m : request.ExchangeRate;
        var foreign = rate != 1m;
        var baseLines = resolved
            .Select(r => new BaseLine(
                r.Account,
                Math.Round(r.Line.Debit * rate, 4, MidpointRounding.ToEven),
                Math.Round(r.Line.Credit * rate, 4, MidpointRounding.ToEven),
                r.Line.Debit > 0m ? r.Line.Debit : r.Line.Credit,
                r.Line.Description))
            .ToList();

        // Per-line rounding (foreign translation OR a caller that pre-rounded each
        // term) can leave a sub-cent residual; push it onto the largest line of the
        // heavier side so the entry always balances exactly — at any rate, including
        // rate == 1 where a domestic caller could still hand us a cent of drift.
        // Only a rounding-scale residual is absorbed: a genuinely lopsided basket
        // exceeds the tolerance and is left to fail balance validation in Post().
        // Each translated line can drift at most half a cent; the worst-case sum
        // therefore scales with the line count and the rate, with a one-cent floor
        // for the domestic (rate == 1) case.
        var tolerance = Math.Max(0.01m, baseLines.Count * 0.0001m * rate);
        var residual = Math.Round(baseLines.Sum(l => l.Debit) - baseLines.Sum(l => l.Credit), 4);
        if (residual != 0m && Math.Abs(residual) <= tolerance)
        {
            if (residual > 0m)
            {
                var i = LargestIndex(baseLines, byCredit: true);
                baseLines[i] = baseLines[i] with { Credit = baseLines[i].Credit + residual };
            }
            else
            {
                var i = LargestIndex(baseLines, byCredit: false);
                baseLines[i] = baseLines[i] with { Debit = baseLines[i].Debit - residual };
            }
        }

        foreach (var l in baseLines)
        {
            entry.AddLine(
                l.Account.Id, l.Account.Code, l.Account.Name, l.Debit, l.Credit,
                foreign ? BaseCurrency : request.Currency,
                l.Description,
                foreignAmount: foreign ? l.ForeignAmount : null,
                exchangeRate: foreign ? rate : null);
        }

        // Throws if the caller handed us an unbalanced set — that is a programming
        // error in the caller, not a runtime/data condition, so let it surface.
        entry.Post(Guid.Empty);
        await _journals.AddAsync(entry, cancellationToken);
        return GLPostingResult.Posted;
    }

    private const string BaseCurrency = "TRY";

    private sealed record BaseLine(GLAccount Account, decimal Debit, decimal Credit, decimal ForeignAmount, string? Description);

    private static int LargestIndex(IReadOnlyList<BaseLine> lines, bool byCredit)
    {
        var idx = 0;
        var max = -1m;
        for (var i = 0; i < lines.Count; i++)
        {
            var v = byCredit ? lines[i].Credit : lines[i].Debit;
            if (v > max)
            {
                max = v;
                idx = i;
            }
        }
        return idx;
    }

    private static GLAccount? Resolve(
        GLPostingLine line,
        IReadOnlyDictionary<GLPostingKey, string> overrides,
        IReadOnlyDictionary<string, GLAccount> accountsByCode)
    {
        // An explicit account override (e.g. a reversal mirroring the exact account
        // a prior entry booked) wins over the role→account mapping; otherwise resolve
        // the key through the tenant override, falling back to the standard code.
        var code = !string.IsNullOrWhiteSpace(line.AccountCodeOverride)
            ? line.AccountCodeOverride
            : overrides.TryGetValue(line.Key, out var ov) ? ov : GLPostingDefaults.CodeFor(line.Key);
        if (string.IsNullOrWhiteSpace(code)) return null;
        if (!accountsByCode.TryGetValue(code, out var account)) return null;
        return account.IsPostable && account.IsActive ? account : null;
    }

    private async Task<string> NextJournalNumberAsync(DateTime date, CancellationToken cancellationToken)
    {
        // GetAsync returns a tracked entity; mutating it in-place (instead of the
        // repo's DB-requerying ConsumeAsync) is what lets numbering work inside
        // the domain-event dispatch loop before SaveChanges has run.
        var seq = await _sequences.GetAsync(DocumentSequenceType.JournalNumber, cancellationToken);
        if (seq is null)
        {
            seq = new DocumentSequence(DocumentSequenceType.JournalNumber, "YEV", date.Year, 1, 5);
            await _sequences.AddAsync(seq, cancellationToken);
        }
        var number = seq.ConsumeNext(date);
        _sequences.Update(seq);
        return number;
    }
}
