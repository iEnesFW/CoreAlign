using CoreAlign.Domain.Entities.Sales;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class OrderTemplateRepository : IOrderTemplateRepository
{
    private readonly CoreAlignDbContext _context;

    public OrderTemplateRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<OrderTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.OrderTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<OrderTemplate?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.OrderTemplates
            .Include(t => t.Lines)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<OrderTemplate> Items, int Total)> SearchAsync(
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.OrderTemplates.AsNoTracking();
        if (customerId.HasValue)
        {
            query = query.Where(t => t.CustomerId == customerId.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<OrderTemplate>> GetDueAsync(DateTime nowUtc, int max, CancellationToken cancellationToken = default)
    {
        return await _context.OrderTemplates
            .IgnoreQueryFilters()
            .Where(t => t.IsActive
                && t.Frequency != Domain.Enums.OrderFrequency.None
                && t.NextRunAtUtc != null
                && t.NextRunAtUtc <= nowUtc)
            .Include(t => t.Lines)
            .AsSplitQuery()
            .OrderBy(t => t.NextRunAtUtc)
            .Take(max)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(OrderTemplate template, CancellationToken cancellationToken = default)
    {
        await _context.OrderTemplates.AddAsync(template, cancellationToken);
    }

    public void Update(OrderTemplate template) => _context.OrderTemplates.Update(template);

    public void Remove(OrderTemplate template) => _context.OrderTemplates.Remove(template);
}
