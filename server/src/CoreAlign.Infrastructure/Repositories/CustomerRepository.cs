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
            var pattern = $"%{search.Trim().ToLower()}%";
            query = query.Where(c =>
                EF.Functions.Like(c.Name.ToLower(), pattern) ||
                (c.Code != null && EF.Functions.Like(c.Code.ToLower(), pattern)) ||
                (c.LegalName != null && EF.Functions.Like(c.LegalName.ToLower(), pattern)) ||
                (c.Email != null && EF.Functions.Like(c.Email.ToLower(), pattern)) ||
                (c.Phone != null && EF.Functions.Like(c.Phone.ToLower(), pattern)) ||
                (c.TaxNumber != null && EF.Functions.Like(c.TaxNumber.ToLower(), pattern)));
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
        var invoices = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.CustomerId == customerId && i.Status != InvoiceStatus.Cancelled)
            .Select(i => new { i.Status, i.Total, i.Currency })
            .ToListAsync(cancellationToken);

        var count = invoices.Count;
        var invoiced = invoices.Sum(i => i.Total);
        var paid = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Total);
        var outstanding = invoices.Where(i => i.Status == InvoiceStatus.Issued).Sum(i => i.Total);
        var currency = invoices.FirstOrDefault()?.Currency ?? "USD";

        return (count, invoiced, paid, outstanding, currency);
    }
}
