using CoreAlign.Domain.Entities.Invoices;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class RecurringInvoiceTemplateRepository : IRecurringInvoiceTemplateRepository
{
    private readonly CoreAlignDbContext _context;

    public RecurringInvoiceTemplateRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<RecurringInvoiceTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.RecurringInvoiceTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<RecurringInvoiceTemplate?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.RecurringInvoiceTemplates
            .Include(t => t.Lines)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<RecurringInvoiceTemplate> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        RecurringInvoiceStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.RecurringInvoiceTemplates.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(t => EF.Functions.ILike(t.Name, term));
        }
        if (customerId.HasValue)
        {
            query = query.Where(t => t.CustomerId == customerId.Value);
        }
        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(t => t.Lines)
            .AsSplitQuery()
            .OrderByDescending(t => t.CreatedAtUtc)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(RecurringInvoiceTemplate template, CancellationToken cancellationToken = default)
    {
        await _context.RecurringInvoiceTemplates.AddAsync(template, cancellationToken);
    }

    public void Update(RecurringInvoiceTemplate template) => _context.RecurringInvoiceTemplates.Update(template);
}
