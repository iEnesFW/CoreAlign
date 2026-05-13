using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly CoreAlignDbContext _context;

    public OrderRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public Task<Order?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
            .ThenInclude(l => l.Product)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Order?> GetWithLinesAndShipmentsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
            .ThenInclude(l => l.Product)
            .Include(o => o.Shipments)
            .ThenInclude(s => s.Lines)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<bool> OrderNumberExistsAsync(string orderNumber, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.Where(o => o.OrderNumber == orderNumber);
        if (excludeId.HasValue)
        {
            query = query.Where(o => o.Id != excludeId.Value);
        }
        return query.AnyAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Order> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.AsNoTracking().Include(o => o.Customer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim().ToLower()}%";
            query = query.Where(o =>
                EF.Functions.Like(o.OrderNumber.ToLower(), pattern) ||
                EF.Functions.Like(o.Customer.Name.ToLower(), pattern));
        }

        if (customerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == customerId.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }

    public void Update(Order order)
    {
        _context.Orders.Update(order);
    }

    public void Remove(Order order)
    {
        _context.Orders.Remove(order);
    }
}
