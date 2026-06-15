using CoreAlign.Application.Treasury.Fx;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Services;

public sealed class FxOpenBalanceReader : IFxOpenBalanceReader
{
    private readonly CoreAlignDbContext _context;

    public FxOpenBalanceReader(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<OpenForeignBalance>> GetOpenForeignBalancesAsync(DateTime asOfUtc, CancellationToken ct)
    {
        var arRows = await _context.CustomerLedgerEntries
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(e => e.PostingDate <= asOfUtc && e.Currency != "TRY" && e.TenantId != Guid.Empty)
            .GroupBy(e => new { e.TenantId, e.Currency })
            .Select(g => new
            {
                g.Key.TenantId,
                g.Key.Currency,
                ForeignAmount = g.Sum(e => e.EntryType == Domain.Enums.LedgerEntryType.Debit ? e.Amount : -e.Amount),
                AmountInBase = g.Sum(e => e.EntryType == Domain.Enums.LedgerEntryType.Debit ? e.AmountInBase : -e.AmountInBase),
            })
            .ToListAsync(ct);

        var apRows = await _context.VendorLedgerEntries
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(e => e.PostingDate <= asOfUtc && e.Currency != "TRY" && e.TenantId != Guid.Empty)
            .GroupBy(e => new { e.TenantId, e.Currency })
            .Select(g => new
            {
                g.Key.TenantId,
                g.Key.Currency,
                ForeignAmount = g.Sum(e => e.EntryType == Domain.Enums.LedgerEntryType.Credit ? e.Amount : -e.Amount),
                AmountInBase = g.Sum(e => e.EntryType == Domain.Enums.LedgerEntryType.Credit ? e.AmountInBase : -e.AmountInBase),
            })
            .ToListAsync(ct);

        var results = new List<OpenForeignBalance>(arRows.Count + apRows.Count);
        foreach (var r in arRows)
        {
            if (r.ForeignAmount == 0m) continue;
            var booked = Math.Abs(r.AmountInBase / r.ForeignAmount);
            results.Add(new OpenForeignBalance(r.Currency, r.ForeignAmount, booked, IsReceivable: true, TenantId: r.TenantId));
        }
        foreach (var r in apRows)
        {
            if (r.ForeignAmount == 0m) continue;
            var booked = Math.Abs(r.AmountInBase / r.ForeignAmount);
            results.Add(new OpenForeignBalance(r.Currency, r.ForeignAmount, booked, IsReceivable: false, TenantId: r.TenantId));
        }
        return results;
    }
}
