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
}

public sealed class PostFxRevaluationJob
{
    private readonly IExchangeRateRepository _rates;
    private readonly IJournalEntryRepository _journals;
    private readonly IGLAccountRepository _accounts;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly IFxOpenBalanceReader _openBalances;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PostFxRevaluationJob> _logger;

    public PostFxRevaluationJob(
        IExchangeRateRepository rates,
        IJournalEntryRepository journals,
        IGLAccountRepository accounts,
        IDocumentSequenceRepository sequences,
        IFxOpenBalanceReader openBalances,
        ITenantContext tenantContext,
        IUnitOfWork uow,
        ILogger<PostFxRevaluationJob> logger)
    {
        _rates = rates;
        _journals = journals;
        _accounts = accounts;
        _sequences = sequences;
        _openBalances = openBalances;
        _tenantContext = tenantContext;
        _uow = uow;
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
        if (balances.Count == 0)
        {
            _logger.LogInformation("PostFxRevaluationJob: no open foreign balances at {AsOf:o}.", asOfUtc);
            return 0;
        }

        var totalRows = 0;
        foreach (var byTenant in balances.GroupBy(b => b.TenantId).Where(g => g.Key != Guid.Empty))
        {
            var revaluations = FxRevaluation.Compute(byTenant, rateMap);
            if (revaluations.Count == 0) continue;

            using (_tenantContext.PushScope(byTenant.Key))
            {
                totalRows += await PostForTenantAsync(byTenant.Key, asOfUtc, revaluations, cancellationToken);
            }
        }

        _logger.LogInformation("PostFxRevaluationJob posted {Count} FX revaluation rows across all tenants at {AsOf:o}.", totalRows, asOfUtc);
        return totalRows;
    }

    private async Task<int> PostForTenantAsync(Guid tenantId, DateTime asOfUtc, IReadOnlyList<FxRevaluationRow> revaluations, CancellationToken cancellationToken)
    {
        var allAccounts = await _accounts.ListAsync(null, null, null, null, cancellationToken);
        var byCode = allAccounts.ToDictionary(a => a.Code, StringComparer.Ordinal);
        if (!byCode.TryGetValue(FxRevaluation.GainAccountCode, out var gain) ||
            !byCode.TryGetValue(FxRevaluation.LossAccountCode, out var loss) ||
            !byCode.TryGetValue(FxRevaluation.ArAccountCode, out var ar) ||
            !byCode.TryGetValue(FxRevaluation.ApAccountCode, out var ap))
        {
            _logger.LogWarning("PostFxRevaluationJob: required GL accounts missing (120/320/646/656) for tenant {TenantId}; skipping.", tenantId);
            return 0;
        }

        var number = await _sequences.ConsumeAsync(DocumentSequenceType.JournalNumber, asOfUtc, cancellationToken);
        var entry = new JournalEntry(number, asOfUtc, asOfUtc, JournalEntryType.Mahsup, "FX revaluation", "FX-REVAL");

        foreach (var row in revaluations)
        {
            var amount = row.DeltaTry;
            var subjectAccount = row.IsReceivable ? ar : ap;
            var pnlAccount = row.IsGain ? gain : loss;

            if (row.IsGain)
            {
                entry.AddLine(subjectAccount.Id, subjectAccount.Code, subjectAccount.Name, debit: amount, credit: 0m, currency: row.Currency, foreignAmount: row.ForeignAmount, exchangeRate: row.CurrentRate);
                entry.AddLine(pnlAccount.Id, pnlAccount.Code, pnlAccount.Name, debit: 0m, credit: amount);
            }
            else
            {
                entry.AddLine(pnlAccount.Id, pnlAccount.Code, pnlAccount.Name, debit: amount, credit: 0m);
                entry.AddLine(subjectAccount.Id, subjectAccount.Code, subjectAccount.Name, debit: 0m, credit: amount, currency: row.Currency, foreignAmount: row.ForeignAmount, exchangeRate: row.CurrentRate);
            }
        }

        entry.AssignSource(JournalSourceType.Manual, entry.Id, "FX-REVAL");
        await _journals.AddAsync(entry, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("PostFxRevaluationJob posted {Count} FX revaluation rows for tenant {TenantId} at {AsOf:o}.", revaluations.Count, tenantId, asOfUtc);
        return revaluations.Count;
    }
}

public interface IFxOpenBalanceReader
{
    Task<IReadOnlyList<OpenForeignBalance>> GetOpenForeignBalancesAsync(DateTime asOfUtc, CancellationToken ct);
}
