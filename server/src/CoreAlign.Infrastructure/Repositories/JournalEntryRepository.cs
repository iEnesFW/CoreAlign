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
        // a posted entry that nets them out automatically.
        var query = _context.JournalLines.AsNoTracking()
            .Where(l => _context.JournalEntries
                .Any(j => j.Id == l.JournalEntryId
                    && j.Status == JournalEntryStatus.Posted
                    && (!fromUtc.HasValue || j.PostingDate >= fromUtc.Value)
                    && (!toUtc.HasValue || j.PostingDate <= toUtc.Value)));

        // EF Core 10: subquery (Any) + GroupBy aggregate kombinasyonu translate edilemiyor.
        // Flat fetch + in-memory group (trial balance icin acceptable, journal lines fazla buyuk degil).
        var flat = await query
            .Select(l => new { l.AccountId, l.AccountCode, l.AccountName, l.Debit, l.Credit })
            .ToListAsync(cancellationToken);

        return flat
            .GroupBy(l => new { l.AccountId, l.AccountCode, l.AccountName })
            .Select(g => new AccountBalanceRow(
                g.Key.AccountId,
                g.Key.AccountCode,
                g.Key.AccountName,
                g.Sum(l => l.Debit),
                g.Sum(l => l.Credit)))
            .OrderBy(r => r.AccountCode)
            .ToList();
    }

    public async Task AddAsync(JournalEntry entry, CancellationToken cancellationToken = default) =>
        await _context.JournalEntries.AddAsync(entry, cancellationToken);

    public void Update(JournalEntry entry) => _context.JournalEntries.Update(entry);

    public void Remove(JournalEntry entry) => _context.JournalEntries.Remove(entry);
}
