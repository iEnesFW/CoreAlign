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

    public async Task<(IReadOnlyList<Invoice> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Invoices.AsNoTracking().Include(i => i.Customer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim().ToLower()}%";
            query = query.Where(i =>
                EF.Functions.Like(i.InvoiceNumber.ToLower(), pattern) ||
                EF.Functions.Like(i.CustomerNameSnapshot.ToLower(), pattern));
        }

        if (customerId.HasValue)
        {
            query = query.Where(i => i.CustomerId == customerId.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.IssueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<Invoice>> GetOpenForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await _context.Invoices
            .Where(i => i.CustomerId == customerId
                && (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.Sent
                    || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue))
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _context.Invoices.AddAsync(invoice, cancellationToken);
    }

    public void Update(Invoice invoice)
    {
        _context.Invoices.Update(invoice);
    }
}
