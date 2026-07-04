using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly CoreAlignDbContext _context;

    public InvoiceRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public Task<Invoice?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .ThenInclude(l => l.Product)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<Invoice?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _context.Invoices
            .Include(i => i.Lines)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.OrderId == orderId, cancellationToken);
    }

    public Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _context.Invoices.AnyAsync(i => i.OrderId == orderId, cancellationToken);
    }

    public Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var query = _context.Invoices.Where(i => i.InvoiceNumber == invoiceNumber);
        if (excludeId.HasValue) query = query.Where(i => i.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<InvoiceSearchRow> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // No .Include(Customer): the customer name lives on the snapshot field
        // already, and projecting Customer.Name via a left-join would force the
        // wider join we just removed. The projection below picks only the
        // columns InvoiceSearchRow needs — JSONB snapshots/breakdown and long
        // notes never leave the server.
        var query = _context.Invoices.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // ILike (Postgres) can use a gin_trgm_ops functional index; the
            // SQLite fallback uses LOWER+LIKE which works without indexes for
            // local dev volume. The pattern is built once per request.
            var lower = $"%{search.Trim().ToLower()}%";
            if (_context.Database.IsNpgsql())
            {
                query = query.Where(i =>
                    EF.Functions.ILike(i.InvoiceNumber, lower) ||
                    EF.Functions.ILike(i.CustomerNameSnapshot, lower));
            }
            else
            {
                query = query.Where(i =>
                    EF.Functions.Like(i.InvoiceNumber.ToLower(), lower) ||
                    EF.Functions.Like(i.CustomerNameSnapshot.ToLower(), lower));
            }
        }

        if (customerId.HasValue)
        {
            query = query.Where(i => i.CustomerId == customerId.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.IssueDate)
            .ThenBy(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InvoiceSearchRow(
                i.Id,
                i.InvoiceNumber,
                i.Type,
                i.OrderId,
                i.Customer != null ? i.Customer.Name : i.CustomerNameSnapshot,
                i.IssueDate,
                i.DueDate,
                i.Status,
                i.Currency,
                i.Total,
                i.AmountPaid,
                _context.Orders
                    .Where(o => o.Id == i.OrderId)
                    .Select(o => o.OrderNumber)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<Invoice>> GetOpenForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await _context.Invoices
            .AsNoTracking()
            .Where(i => i.CustomerId == customerId
                && (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.Sent
                    || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue))
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MonthlyInvoiceTotal>> GetMonthlyRevenueByCustomerAsync(
        Guid customerId,
        DateTime fromUtc,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.CustomerId == customerId
                && i.IssueDate >= fromUtc
                && i.Status != InvoiceStatus.Draft
                && i.Status != InvoiceStatus.Cancelled
                && i.Status != InvoiceStatus.Void)
            .GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Revenue = g.Sum(i => i.Total),
                InvoiceCount = g.Count(),
                Paid = g.Sum(i => i.AmountPaid),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new MonthlyInvoiceTotal(r.Year, r.Month, r.Revenue, r.InvoiceCount, r.Paid))
            .OrderBy(r => r.Year)
            .ThenBy(r => r.Month)
            .ToList();
    }

    public async Task<IReadOnlyList<TopProductLine>> GetTopProductsByCustomerAsync(
        Guid customerId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        // EF Core can't translate a distinct-count over a grouped subcollection
        // (g.Select(x => x.InvoiceId).Distinct().Count()), so the per-line columns
        // are projected in SQL (filtered to one customer) and aggregated in memory.
        var lines = await _context.InvoiceLines
            .AsNoTracking()
            .Where(l => l.Invoice!.CustomerId == customerId
                && l.Invoice.Status != InvoiceStatus.Draft
                && l.Invoice.Status != InvoiceStatus.Cancelled
                && l.Invoice.Status != InvoiceStatus.Void)
            .Select(l => new
            {
                l.ProductId,
                l.ProductSku,
                l.ProductName,
                l.Quantity,
                l.LineTotal,
                l.InvoiceId,
            })
            .ToListAsync(cancellationToken);

        return lines
            .GroupBy(l => new { l.ProductId, l.ProductSku, l.ProductName })
            .Select(g => new TopProductLine(
                g.Key.ProductId,
                g.Key.ProductSku,
                g.Key.ProductName,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.LineTotal),
                g.Select(x => x.InvoiceId).Distinct().Count()))
            .OrderByDescending(t => t.Revenue)
            .Take(limit)
            .ToList();
    }

    public async Task<PaymentBehavior> GetPaymentBehaviorByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.CustomerId == customerId
                && i.PaidAtUtc != null
                && i.Status == InvoiceStatus.Paid)
            .Select(i => new { i.DueDate, PaidAt = i.PaidAtUtc!.Value })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return new PaymentBehavior(0, 0, 0d);
        }

        var onTime = rows.Count(r => r.PaidAt.Date <= r.DueDate.Date);
        var late = rows.Count - onTime;
        var avgDays = rows.Average(r => (r.PaidAt.Date - r.DueDate.Date).TotalDays);
        return new PaymentBehavior(onTime, late, avgDays);
    }

    public async Task<IReadOnlyList<StatusGroup>> GetInvoiceStatusBreakdownAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.CustomerId == customerId)
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Total = g.Sum(i => i.Total) })
            .ToListAsync(cancellationToken);
        return rows.Select(r => new StatusGroup(r.Status.ToString(), r.Count, r.Total)).ToList();
    }

    public async Task<IReadOnlyList<Invoice>> GetCreditNotesForInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default) =>
        await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Where(i => i.Type == InvoiceType.CreditNote && i.OriginInvoiceId == invoiceId)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<InvoiceSearchRow> Items, int Total)> SearchForCustomersAsync(
        IReadOnlyCollection<Guid> customerIds,
        InvoiceStatus? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (customerIds is null || customerIds.Count == 0)
        {
            return (Array.Empty<InvoiceSearchRow>(), 0);
        }

        var customerIdArray = customerIds.Distinct().ToArray();
        var query = _context.Invoices
            .AsNoTracking()
            .Where(i => customerIdArray.Contains(i.CustomerId));

        if (status.HasValue) query = query.Where(i => i.Status == status.Value);
        if (fromUtc.HasValue) query = query.Where(i => i.IssueDate >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(i => i.IssueDate <= toUtc.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(i => i.IssueDate)
            .ThenBy(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InvoiceSearchRow(
                i.Id,
                i.InvoiceNumber,
                i.Type,
                i.OrderId,
                i.Customer != null ? i.Customer.Name : i.CustomerNameSnapshot,
                i.IssueDate,
                i.DueDate,
                i.Status,
                i.Currency,
                i.Total,
                i.AmountPaid,
                _context.Orders
                    .Where(o => o.Id == i.OrderId)
                    .Select(o => o.OrderNumber)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyDictionary<Guid, Invoice>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        // Materialize once in case the caller passed a transient enumerable.
        var idList = ids?.Distinct().ToArray() ?? Array.Empty<Guid>();
        if (idList.Length == 0)
        {
            return new Dictionary<Guid, Invoice>();
        }
        // Tracked load — handlers use the returned entities to record payments,
        // so we must let EF's change tracker observe the mutations.
        var rows = await _context.Invoices
            .Where(i => idList.Contains(i.Id))
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(i => i.Id);
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _context.Invoices.AddAsync(invoice, cancellationToken);
    }

    public void Update(Invoice invoice)
    {
        _context.Invoices.Update(invoice);
    }
}
