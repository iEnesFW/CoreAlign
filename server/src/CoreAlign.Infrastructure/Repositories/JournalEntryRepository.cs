using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class JournalEntryRepository : IJournalEntryRepository
{
    private readonly CoreAlignDbContext _context;
    public JournalEntryRepository(CoreAlignDbContext context) => _context = context;

    public Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.JournalEntries.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

    public Task<JournalEntry?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.JournalEntries
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

    public Task<bool> NumberExistsAsync(string number, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var query = _context.JournalEntries.AsNoTracking().Where(j => j.Number == number);
        if (excludeId.HasValue) query = query.Where(j => j.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> ExistsForSourceAsync(JournalSourceType sourceType, Guid sourceDocumentId, CancellationToken cancellationToken = default)
    {
        if (sourceDocumentId == Guid.Empty) return Task.FromResult(false);
        return _context.JournalEntries
            .AsNoTracking()
            .AnyAsync(j => j.SourceType == sourceType && j.SourceDocumentId == sourceDocumentId, cancellationToken);
    }

    public Task<JournalEntry?> GetActiveBySourceAsync(JournalSourceType sourceType, Guid sourceDocumentId, CancellationToken cancellationToken = default)
    {
        if (sourceDocumentId == Guid.Empty) return Task.FromResult<JournalEntry?>(null);
        // "Active" = Posted and not undone. A close-reversal keeps the original
        // Posted (so the aggregate nets its sweep against the contra), so a stale
        // close is detected by an existing reversal pointing at it — not by the
        // original's own status.
        return _context.JournalEntries
            .Include(j => j.Lines)
            .Where(j => j.SourceType == sourceType
                && j.SourceDocumentId == sourceDocumentId
                && j.Status == JournalEntryStatus.Posted
                && !_context.JournalEntries.Any(r => r.ReversalOfId == j.Id))
            .OrderByDescending(j => j.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<JournalEntry?> GetMostRecentBySourceTypeBeforeAsync(
        JournalSourceType sourceType,
        DateTime beforePostingDate,
        CancellationToken cancellationToken = default)
    {
        var bound = DateTime.SpecifyKind(beforePostingDate, DateTimeKind.Utc);
        return _context.JournalEntries
            .Include(j => j.Lines)
            .Where(j => j.SourceType == sourceType
                && j.Status == JournalEntryStatus.Posted
                && j.PostingDate < bound)
            .OrderByDescending(j => j.PostingDate)
            .ThenByDescending(j => j.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<JournalEntrySearchRow> Items, int Total)> SearchAsync(
        string? search,
        JournalEntryType? type,
        JournalEntryStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.JournalEntries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = $"%{search.Trim().ToLower()}%";
            if (_context.Database.IsNpgsql())
            {
                query = query.Where(j =>
                    EF.Functions.ILike(j.Number, lower) ||
                    (j.Description != null && EF.Functions.ILike(j.Description, lower)) ||
                    (j.Reference != null && EF.Functions.ILike(j.Reference, lower)));
            }
            else
            {
                query = query.Where(j =>
                    EF.Functions.Like(j.Number.ToLower(), lower) ||
                    (j.Description != null && EF.Functions.Like(j.Description.ToLower(), lower)) ||
                    (j.Reference != null && EF.Functions.Like(j.Reference.ToLower(), lower)));
            }
        }
        if (type.HasValue) query = query.Where(j => j.Type == type.Value);
        if (status.HasValue) query = query.Where(j => j.Status == status.Value);
        if (fromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc);
            query = query.Where(j => j.PostingDate >= fromUtc);
        }
        if (toDate.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc);
            query = query.Where(j => j.PostingDate <= toUtc);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(j => j.PostingDate)
            .ThenBy(j => j.Number)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new JournalEntrySearchRow(
                j.Id,
                j.Number,
                j.EntryDate,
                j.PostingDate,
                j.Type,
                j.Status,
                j.Description,
                j.Reference,
                j.TotalDebit,
                j.TotalCredit,
                j.Lines.Count))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<AccountBalanceRow>> GetAccountBalancesAsync(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = fromDate.HasValue ? DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc) : (DateTime?)null;
        var toUtc = toDate.HasValue ? DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc) : (DateTime?)null;

        // Posted entries only — Draft entries don't appear in the trial balance.
        // Reversed entries stay in the totals because their reversal is itself
        // a posted entry that nets them out automatically. Aggregated server-side
        // (join + GROUP BY/SUM) so the DB returns one row per account, never the
        // full journal_lines set — scale-safe at millions of lines.
        var grouped = await (
            from l in _context.JournalLines.AsNoTracking()
            join j in _context.JournalEntries on l.JournalEntryId equals j.Id
            where j.Status == JournalEntryStatus.Posted
                && (!fromUtc.HasValue || j.PostingDate >= fromUtc.Value)
                && (!toUtc.HasValue || j.PostingDate <= toUtc.Value)
            group new { l.Debit, l.Credit } by new { l.AccountId, l.AccountCode, l.AccountName } into g
            orderby g.Key.AccountCode
            select new
            {
                g.Key.AccountId,
                g.Key.AccountCode,
                g.Key.AccountName,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit),
            }).ToListAsync(cancellationToken);

        return grouped
            .Select(r => new AccountBalanceRow(r.AccountId, r.AccountCode, r.AccountName, r.Debit, r.Credit))
            .ToList();
    }

    public async Task<IReadOnlyList<AccountBalanceRow>> GetAccountBalancesAsOfAsync(
        DateTime asOf,
        CancellationToken ct = default)
    {
        // Cumulative variant of GetAccountBalancesAsync: drops the lower bound and
        // sums ALL posted history up to and including asOf. Full history IS the
        // opening-balance carry-forward, so asset/liability positions reflect their
        // true balance — not just the period movement. Reversals self-net because
        // each reversal is itself a Posted entry with PostingDate <= asOf.
        // asOf is a calendar cutoff; include the whole day (end-of-day) so same-day
        // time-stamped postings (e.g. a reversal booked at UtcNow) are not silently
        // dropped when asOf arrives at midnight.
        var asOfUtc = DateTime.SpecifyKind(asOf.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
        var grouped = await (
            from l in _context.JournalLines.AsNoTracking()
            join j in _context.JournalEntries on l.JournalEntryId equals j.Id
            where j.Status == JournalEntryStatus.Posted && j.PostingDate <= asOfUtc
            group new { l.Debit, l.Credit } by new { l.AccountId, l.AccountCode, l.AccountName } into g
            orderby g.Key.AccountCode
            select new
            {
                g.Key.AccountId,
                g.Key.AccountCode,
                g.Key.AccountName,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit),
            }).ToListAsync(ct);

        return grouped
            .Select(r => new AccountBalanceRow(r.AccountId, r.AccountCode, r.AccountName, r.Debit, r.Credit))
            .ToList();
    }

    public async Task AddAsync(JournalEntry entry, CancellationToken cancellationToken = default) =>
        await _context.JournalEntries.AddAsync(entry, cancellationToken);

    public void Update(JournalEntry entry) => _context.JournalEntries.Update(entry);

    public void Remove(JournalEntry entry) => _context.JournalEntries.Remove(entry);
}
