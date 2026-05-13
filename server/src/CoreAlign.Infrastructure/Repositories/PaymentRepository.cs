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

    public async Task<(IReadOnlyList<Payment> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Payments.AsNoTracking().Include(p => p.Customer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim().ToLower()}%";
            query = query.Where(p =>
                EF.Functions.Like(p.PaymentNumber.ToLower(), pattern) ||
                EF.Functions.Like(p.CustomerNameSnapshot.ToLower(), pattern) ||
                (p.ReferenceNumber != null && EF.Functions.Like(p.ReferenceNumber.ToLower(), pattern)));
        }

        if (customerId.HasValue) query = query.Where(p => p.CustomerId == customerId.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<Payment>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await _context.Payments
            .Include(p => p.Applications)
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.PaymentDate)
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
        var debit = await _context.CustomerLedgerEntries
            .Where(e => e.CustomerId == customerId && e.EntryType == Domain.Enums.LedgerEntryType.Debit)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;
        var credit = await _context.CustomerLedgerEntries
            .Where(e => e.CustomerId == customerId && e.EntryType == Domain.Enums.LedgerEntryType.Credit)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;
        return Math.Round(debit - credit, 4);
    }

    public async Task<decimal> GetLastRunningBalanceAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var last = await _context.CustomerLedgerEntries
            .Where(e => e.CustomerId == customerId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return last?.RunningBalanceAfter ?? 0m;
    }
}
