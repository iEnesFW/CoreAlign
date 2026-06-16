using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly CoreAlignDbContext _context;

    public CustomerRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Dictionary<Guid, Customer>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToArray();
        if (idList.Length == 0) return new Dictionary<Guid, Customer>();
        return await _context.Customers
            .Where(c => idList.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Customer> Items, int Total)> SearchAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = $"%{search.Trim().ToLower()}%";
            if (_context.Database.IsNpgsql())
            {
                query = query.Where(c =>
                    EF.Functions.ILike(c.Name, lower) ||
                    (c.Code != null && EF.Functions.ILike(c.Code, lower)) ||
                    (c.LegalName != null && EF.Functions.ILike(c.LegalName, lower)) ||
                    (c.Email != null && EF.Functions.ILike(c.Email, lower)) ||
                    (c.Phone != null && EF.Functions.ILike(c.Phone, lower)) ||
                    (c.TaxNumber != null && EF.Functions.ILike(c.TaxNumber, lower)));
            }
            else
            {
                query = query.Where(c =>
                    EF.Functions.Like(c.Name.ToLower(), lower) ||
                    (c.Code != null && EF.Functions.Like(c.Code.ToLower(), lower)) ||
                    (c.LegalName != null && EF.Functions.Like(c.LegalName.ToLower(), lower)) ||
                    (c.Email != null && EF.Functions.Like(c.Email.ToLower(), lower)) ||
                    (c.Phone != null && EF.Functions.Like(c.Phone.ToLower(), lower)) ||
                    (c.TaxNumber != null && EF.Functions.Like(c.TaxNumber.ToLower(), lower)));
            }
        }

        if (isActive.HasValue)
        {
            query = isActive.Value
                ? query.Where(c => c.Status == CustomerStatus.Active)
                : query.Where(c => c.Status != CustomerStatus.Active);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.Customers.AddAsync(customer, cancellationToken);
    }

    public void Update(Customer customer)
    {
        _context.Customers.Update(customer);
    }

    public void Remove(Customer customer)
    {
        _context.Customers.Remove(customer);
    }

    public async Task<(int OrderCount, decimal OrderTotal)> GetOrderTotalsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var stats = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => 1)
            .Select(g => new { Count = g.Count(), Total = g.Sum(o => o.Total) })
            .FirstOrDefaultAsync(cancellationToken);

        return (stats?.Count ?? 0, stats?.Total ?? 0m);
    }

    public async Task<(int InvoiceCount, decimal Invoiced, decimal Paid, decimal Outstanding, string Currency)> GetInvoiceTotalsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        // Server-side aggregate via conditional sums — avoids pulling every
        // invoice row into memory just to derive 5 scalars. Currency falls back
        // to a separate single-row query if no invoices exist yet.
        var agg = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.CustomerId == customerId && i.Status != InvoiceStatus.Cancelled)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Invoiced = g.Sum(i => i.Total),
                Paid = g.Sum(i => i.Status == InvoiceStatus.Paid ? i.Total : 0m),
                Outstanding = g.Sum(i => i.Status == InvoiceStatus.Issued ? i.Total : 0m),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (agg is null)
        {
            return (0, 0m, 0m, 0m, "USD");
        }

        var currency = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.CustomerId == customerId)
            .Select(i => i.Currency)
            .FirstOrDefaultAsync(cancellationToken) ?? "USD";

        return (agg.Count, agg.Invoiced, agg.Paid, agg.Outstanding, currency);
    }
}
