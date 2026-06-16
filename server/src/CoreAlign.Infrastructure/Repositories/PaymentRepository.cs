using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly CoreAlignDbContext _context;
    public PaymentRepository(CoreAlignDbContext context) => _context = context;

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Payments
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Payment?> GetWithApplicationsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Payments
            .Include(p => p.Customer)
            .Include(p => p.Applications)
            .ThenInclude(a => a.Invoice)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<PaymentSearchRow> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Payments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = $"%{search.Trim().ToLower()}%";
            if (_context.Database.IsNpgsql())
            {
                query = query.Where(p =>
                    EF.Functions.ILike(p.PaymentNumber, lower) ||
                    EF.Functions.ILike(p.CustomerNameSnapshot, lower) ||
                    (p.ReferenceNumber != null && EF.Functions.ILike(p.ReferenceNumber, lower)));
            }
            else
            {
                query = query.Where(p =>
                    EF.Functions.Like(p.PaymentNumber.ToLower(), lower) ||
                    EF.Functions.Like(p.CustomerNameSnapshot.ToLower(), lower) ||
                    (p.ReferenceNumber != null && EF.Functions.Like(p.ReferenceNumber.ToLower(), lower)));
            }
        }

        if (customerId.HasValue) query = query.Where(p => p.CustomerId == customerId.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PaymentSearchRow(
                p.Id,
                p.PaymentNumber,
                p.Direction,
                p.Status,
                p.CustomerId,
                p.Customer != null ? p.Customer.Name : p.CustomerNameSnapshot,
                p.PaymentDate,
                p.Method,
                p.Amount,
                p.AppliedAmount,
                p.Currency))
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<Payment>> GetByCustomerAsync(
        Guid customerId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 500);
        return await _context.Payments
            .AsNoTracking()
            .Include(p => p.Applications)
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.PaymentDate)
            .Take(safeLimit)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentSummaryAggregate> GetCustomerPaymentSummaryAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var result = await _context.Payments
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Last = g.Max(p => (DateTime?)p.PaymentDate),
                Total = g.Sum(p => p.Amount),
            })
            .FirstOrDefaultAsync(cancellationToken);
        return result is null
            ? new PaymentSummaryAggregate(0, null, 0m)
            : new PaymentSummaryAggregate(result.Count, result.Last, result.Total);
    }

    public async Task<IReadOnlyList<PaymentApplication>> GetApplicationsByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default) =>
        await _context.PaymentApplications
            .Include(a => a.Payment)
            .Include(a => a.Invoice)
            .Where(a => a.InvoiceId == invoiceId)
            .OrderByDescending(a => a.AppliedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default) =>
        await _context.Payments.AddAsync(payment, cancellationToken);

    public void Update(Payment payment) => _context.Payments.Update(payment);
}

public class CustomerLedgerRepository : ICustomerLedgerRepository
{
    private readonly CoreAlignDbContext _context;
    public CustomerLedgerRepository(CoreAlignDbContext context) => _context = context;

    public async Task AddAsync(CustomerLedgerEntry entry, CancellationToken cancellationToken = default) =>
        await _context.CustomerLedgerEntries.AddAsync(entry, cancellationToken);

    public async Task AcquireAppendLockAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsNpgsql()) return;
        var key = $"ledger:customer:{customerId}";
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
    }

    public async Task<(IReadOnlyList<CustomerLedgerEntry> Items, int Total)> SearchByCustomerAsync(
        Guid customerId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CustomerLedgerEntries.AsNoTracking().Where(e => e.CustomerId == customerId);
        if (fromUtc.HasValue) query = query.Where(e => e.OccurredAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(e => e.OccurredAtUtc <= toUtc.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<decimal> GetCurrentBalanceAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        // Single round-trip: conditional sums let Postgres compute debit/credit in
        // one scan instead of two predicate queries against the same partition.
        var row = await _context.CustomerLedgerEntries
            .AsNoTracking()
            .Where(e => e.CustomerId == customerId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Debit = g.Sum(e => e.EntryType == Domain.Enums.LedgerEntryType.Debit ? e.Amount : 0m),
                Credit = g.Sum(e => e.EntryType == Domain.Enums.LedgerEntryType.Credit ? e.Amount : 0m),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null) return 0m;
        return Math.Round(row.Debit - row.Credit, 4);
    }

    public async Task<decimal> GetLastRunningBalanceAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var last = await _context.CustomerLedgerEntries
            .Where(e => e.CustomerId == customerId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return last?.RunningBalanceAfter ?? 0m;
    }

    public async Task<decimal> GetBalanceAsOfAsync(Guid customerId, DateTime? cutoffUtc, CancellationToken cancellationToken = default)
    {
        var query = _context.CustomerLedgerEntries
            .AsNoTracking()
            .Where(e => e.CustomerId == customerId);
        if (cutoffUtc.HasValue)
        {
            var cutoff = DateTime.SpecifyKind(cutoffUtc.Value, DateTimeKind.Utc);
            query = query.Where(e => e.OccurredAtUtc <= cutoff);
        }

        var row = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Debit = g.Sum(e => e.EntryType == Domain.Enums.LedgerEntryType.Debit ? e.Amount : 0m),
                Credit = g.Sum(e => e.EntryType == Domain.Enums.LedgerEntryType.Credit ? e.Amount : 0m),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null) return 0m;
        return Math.Round(row.Debit - row.Credit, 4);
    }

    public async Task<decimal> GetTotalBalanceAsOfAsync(DateTime asOf, CancellationToken cancellationToken = default)
    {
        // Aggregate over ALL customers (no per-customer Where). Filters on
        // PostingDate to align with the GL's PostingDate cutoff for a true as-of
        // reconciliation. Customer convention: balance = Σ debit − Σ credit.
        var asOfUtc = DateTime.SpecifyKind(asOf, DateTimeKind.Utc);
        var row = await _context.CustomerLedgerEntries
            .AsNoTracking()
            .Where(e => e.PostingDate <= asOfUtc)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Debit = g.Sum(e => e.EntryType == Domain.Enums.LedgerEntryType.Debit ? e.Amount : 0m),
                Credit = g.Sum(e => e.EntryType == Domain.Enums.LedgerEntryType.Credit ? e.Amount : 0m),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null) return 0m;
        return Math.Round(row.Debit - row.Credit, 4);
    }

    public async Task<int> CountByCustomerAsync(Guid customerId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var query = _context.CustomerLedgerEntries.AsNoTracking().Where(e => e.CustomerId == customerId);
        if (fromUtc.HasValue) query = query.Where(e => e.OccurredAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(e => e.OccurredAtUtc <= toUtc.Value);
        return await query.CountAsync(cancellationToken);
    }
}
