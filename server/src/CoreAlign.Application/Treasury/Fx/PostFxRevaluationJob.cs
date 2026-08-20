using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Common;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Treasury.Fx;

public sealed record OpenForeignBalance(string Currency, decimal ForeignAmount, decimal BookedRate, bool IsReceivable, Guid TenantId);

public sealed record FxRevaluationRow(string Currency, decimal ForeignAmount, decimal BookedRate, decimal CurrentRate, decimal DeltaTry, bool IsGain, bool IsReceivable);

public static class FxRevaluation
{
    public const string GainAccountCode = "646";
    public const string LossAccountCode = "656";
    public const string ArAccountCode = "120";
    public const string ApAccountCode = "320";

    public static IReadOnlyList<FxRevaluationRow> Compute(IEnumerable<OpenForeignBalance> balances, IReadOnlyDictionary<string, decimal> currentRates)
    {
        var rows = new List<FxRevaluationRow>();
        foreach (var b in balances)
        {
            if (!currentRates.TryGetValue(b.Currency, out var current)) continue;
            var bookedTry = Math.Round(b.ForeignAmount * b.BookedRate, 4, MidpointRounding.ToEven);
            var currentTry = Math.Round(b.ForeignAmount * current, 4, MidpointRounding.ToEven);
            var delta = Math.Round(currentTry - bookedTry, 4, MidpointRounding.ToEven);
            if (delta == 0m) continue;
            var isGain = b.IsReceivable ? delta > 0m : delta < 0m;
            rows.Add(new FxRevaluationRow(b.Currency, b.ForeignAmount, b.BookedRate, current, Math.Abs(delta), isGain, b.IsReceivable));
        }
        return rows;
    }

    /// <summary>
    /// Stable idempotency key for a tenant's revaluation as of a given date: one
    /// entry per (tenant, calendar day). A re-run for the same asOf resolves to the
    /// same key, so <see cref="IGLPostingService"/> dedupes it instead of double
    /// posting the unrealized mark.
    /// </summary>
    public static Guid SourceKey(Guid tenantId, DateTime asOfUtc) =>
        DeterministicGuid.From($"FXREVAL|{tenantId:N}|{asOfUtc:yyyyMMdd}");

    /// <summary>
    /// GL legs for one revaluation row, amount already in TRY (DeltaTry). Receivable
    /// gain debits AR and credits FxGain; receivable loss debits FxLoss and credits
    /// AR; payable gain debits AP and credits FxGain; payable loss debits FxLoss and
    /// credits AP. Mirrors <see cref="Compute"/>'s IsGain semantics.
    /// </summary>
    public static IReadOnlyList<GLPostingLine> Legs(FxRevaluationRow row)
    {
        var amount = row.DeltaTry;
        var subjectKey = row.IsReceivable ? GLPostingKey.AccountsReceivable : GLPostingKey.AccountsPayable;
        var pnlKey = row.IsGain ? GLPostingKey.FxGain : GLPostingKey.FxLoss;
        var desc = $"FX reval {row.Currency} {(row.IsGain ? "gain" : "loss")}";
        return row.IsGain
            ? new[]
            {
                new GLPostingLine(subjectKey, amount, 0m, desc),
                new GLPostingLine(pnlKey, 0m, amount, desc),
            }
            : new[]
            {
                new GLPostingLine(pnlKey, amount, 0m, desc),
                new GLPostingLine(subjectKey, 0m, amount, desc),
            };
    }
}

public sealed class PostFxRevaluationJob
{
    private const string SourceReference = "FX-REVAL";

    private readonly IExchangeRateRepository _rates;
    private readonly IJournalEntryRepository _journals;
    private readonly IFxOpenBalanceReader _openBalances;
    private readonly ITenantContext _tenantContext;
    private readonly IGLPostingOutbox _outbox;
    private readonly ILogger<PostFxRevaluationJob> _logger;

    public PostFxRevaluationJob(
        IExchangeRateRepository rates,
        IJournalEntryRepository journals,
        IFxOpenBalanceReader openBalances,
        ITenantContext tenantContext,
        IGLPostingOutbox outbox,
        ILogger<PostFxRevaluationJob> logger)
    {
        _rates = rates;
        _journals = journals;
        _openBalances = openBalances;
        _tenantContext = tenantContext;
        _outbox = outbox;
        _logger = logger;
    }

    public async Task<int> RunAsync(DateTime asOfUtc, CancellationToken cancellationToken = default)
    {
        var ratesAtDate = await _rates.GetLatestPerCurrencyOnOrBeforeAsync(asOfUtc, cancellationToken);
        var rateMap = ratesAtDate.ToDictionary(r => r.Currency, r => r.RateAgainstTry, StringComparer.OrdinalIgnoreCase);
        if (rateMap.Count == 0)
        {
            _logger.LogInformation("PostFxRevaluationJob: no FX rates available for {AsOf:o}; skipping.", asOfUtc);
            return 0;
        }

        var balances = await _openBalances.GetOpenForeignBalancesAsync(asOfUtc, cancellationToken);
        var byTenant = balances
            .Where(b => b.TenantId != Guid.Empty)
            .GroupBy(b => b.TenantId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<OpenForeignBalance>)g.ToList());

        // WHY the tenant set is a union: a tenant whose foreign exposure has been settled produces
        // NO open balance, so iterating balances alone skipped it — and its previous unrealized
        // mark then sat on the books forever, overstating AR/AP and the FX P&L. Those tenants
        // still need the reversal leg, with nothing rebooked on top of it.
        var carriedMarks = await _journals.GetTenantIdsWithPostedSourceTypeBeforeAsync(
            JournalSourceType.FxRevaluation, asOfUtc.Date, cancellationToken);

        var tenantIds = byTenant.Keys
            .Concat(carriedMarks.Where(id => id != Guid.Empty))
            .Distinct()
            .ToList();
        if (tenantIds.Count == 0)
        {
            _logger.LogInformation(
                "PostFxRevaluationJob: no open foreign balances and no carried marks at {AsOf:o}.", asOfUtc);
            return 0;
        }

        var totalTenants = 0;
        foreach (var tenantId in tenantIds)
        {
            var tenantBalances = byTenant.TryGetValue(tenantId, out var found)
                ? found
                : Array.Empty<OpenForeignBalance>();
            var revaluations = FxRevaluation.Compute(tenantBalances, rateMap);

            using (_tenantContext.PushScope(tenantId))
            {
                if (await EnqueueForTenantAsync(tenantId, asOfUtc, revaluations, cancellationToken))
                {
                    totalTenants++;
                }
            }
        }

        _logger.LogInformation("PostFxRevaluationJob enqueued FX revaluation for {Count} tenants at {AsOf:o}.", totalTenants, asOfUtc);
        return totalTenants;
    }

    private async Task<bool> EnqueueForTenantAsync(Guid tenantId, DateTime asOfUtc, IReadOnlyList<FxRevaluationRow> revaluations, CancellationToken cancellationToken)
    {
        // Net-delta: back out the CUMULATIVE revaluation booked so far and rebook the current
        // mark in the SAME balanced entry, so the ledger always carries exactly the latest
        // position. Reversing only the PREVIOUS ENTRY is not enough — every entry is itself a
        // delta, so mirroring the last one re-creates the one before it (from the third run on,
        // the cumulative drifted to mark(n) + mark(n-2)). Both legs commit atomically; routing
        // through the outbox/GLPostingService gives idempotency (one entry per tenant+asOf) and
        // the closed-period gate for free.
        var lines = new List<GLPostingLine>();

        var booked = await _journals.GetPostedSourceTypeAccountNetsBeforeAsync(
            JournalSourceType.FxRevaluation, asOfUtc.Date, cancellationToken);
        lines.AddRange(BuildReversalLines(booked));

        foreach (var row in revaluations)
        {
            lines.AddRange(FxRevaluation.Legs(row));
        }

        // Nothing to book and nothing to reverse — a flat period with no prior mark.
        if (lines.Count == 0) return false;

        await _outbox.EnqueueAsync(new GLPostingRequest(
            JournalSourceType.FxRevaluation,
            FxRevaluation.SourceKey(tenantId, asOfUtc),
            SourceReference,
            asOfUtc.Date,
            JournalEntryType.Mahsup,
            $"FX revaluation {asOfUtc:yyyy-MM-dd}",
            lines,
            Currency: "TRY",
            ExchangeRate: 1m), cancellationToken);

        return true;
    }

    // Mirror the cumulative position of every FX revaluation account: a net debit becomes a
    // credit on the SAME account and vice versa, valued at the amount actually booked. Each leg
    // carries the real account code as an explicit override, so it reverses what is on the books
    // rather than what the current role→account mapping would re-resolve to — the entry stays
    // balanced even when the tenant remapped FxGain/FxLoss/AR/AP after an earlier mark.
    private static IEnumerable<GLPostingLine> BuildReversalLines(IReadOnlyList<AccountNet> booked)
    {
        const string desc = "FX reval cumulative reversal";
        foreach (var account in booked)
        {
            var net = account.Debit - account.Credit;
            if (net == 0m) continue;
            var key = KeyForCode(account.AccountCode) ?? GLPostingKey.FxGain;
            yield return net > 0m
                ? new GLPostingLine(key, 0m, net, desc, account.AccountCode)
                : new GLPostingLine(key, -net, 0m, desc, account.AccountCode);
        }
    }

    // Best-effort posting-role label for a reversal leg; resolution itself targets the
    // line's explicit account code, so an unrecognized (overridden) code is harmless.
    private static GLPostingKey? KeyForCode(string code) => code switch
    {
        FxRevaluation.GainAccountCode => GLPostingKey.FxGain,
        FxRevaluation.LossAccountCode => GLPostingKey.FxLoss,
        FxRevaluation.ArAccountCode => GLPostingKey.AccountsReceivable,
        FxRevaluation.ApAccountCode => GLPostingKey.AccountsPayable,
        _ => null,
    };
}

public interface IFxOpenBalanceReader
{
    Task<IReadOnlyList<OpenForeignBalance>> GetOpenForeignBalancesAsync(DateTime asOfUtc, CancellationToken ct);
}
