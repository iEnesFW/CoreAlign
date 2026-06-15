using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
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

    public Task<Order?> GetWithLinesAndRevisionsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
            .ThenInclude(l => l.Product)
            .Include(o => o.Revisions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Order?> GetByGlassProjectIdAsync(Guid glassProjectId, CancellationToken cancellationToken = default) =>
        _context.Orders
            .Where(o => o.GlassProjectId == glassProjectId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> OrderNumberExistsAsync(string orderNumber, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.Where(o => o.OrderNumber == orderNumber);
        if (excludeId.HasValue)
        {
            query = query.Where(o => o.Id != excludeId.Value);
        }
        return query.AnyAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<OrderSearchRow> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = $"%{search.Trim().ToLower()}%";
            if (_context.Database.IsNpgsql())
            {
                query = query.Where(o =>
                    EF.Functions.ILike(o.OrderNumber, lower) ||
                    EF.Functions.ILike(o.Customer.Name, lower));
            }
            else
            {
                query = query.Where(o =>
                    EF.Functions.Like(o.OrderNumber.ToLower(), lower) ||
                    EF.Functions.Like(o.Customer.Name.ToLower(), lower));
            }
        }

        if (customerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == customerId.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        // Project to slim row — skips OrderLines, snapshots, tax breakdown, etc.
        var items = await query
            .OrderByDescending(o => o.OrderDate)
            .ThenBy(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSearchRow(
                o.Id,
                o.OrderNumber,
                o.CustomerId,
                o.Customer != null ? o.Customer.Name : o.CustomerSnapshot != null ? o.CustomerSnapshot.LegalName : string.Empty,
                o.OrderDate,
                o.Status,
                o.Currency,
                o.Total,
                o.DealerApprovalStatus,
                o.OriginDealerAccountId))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<OrderSearchRow> Items, int Total)> SearchByDealerAsync(
        Guid dealerAccountId,
        string? status,
        string? approvalStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Where(o => o.OriginDealerAccountId == dealerAccountId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(o => o.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(approvalStatus))
        {
            query = query.Where(o => o.DealerApprovalStatus == approvalStatus);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.OrderDate)
            .ThenBy(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSearchRow(
                o.Id,
                o.OrderNumber,
                o.CustomerId,
                o.Customer != null ? o.Customer.Name : o.CustomerSnapshot != null ? o.CustomerSnapshot.LegalName : string.Empty,
                o.OrderDate,
                o.Status,
                o.Currency,
                o.Total,
                o.DealerApprovalStatus,
                o.OriginDealerAccountId))
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<OrderSearchRow> Items, int Total)> SearchPendingApprovalsForCustomerAsync(
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pending = DealerOrderApprovalStatuses.PendingCustomerApproval;
        var query = _context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId && o.DealerApprovalStatus == pending);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.OrderDate)
            .ThenBy(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSearchRow(
                o.Id,
                o.OrderNumber,
                o.CustomerId,
                o.Customer != null ? o.Customer.Name : o.CustomerSnapshot != null ? o.CustomerSnapshot.LegalName : string.Empty,
                o.OrderDate,
                o.Status,
                o.Currency,
                o.Total,
                o.DealerApprovalStatus,
                o.OriginDealerAccountId))
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<int> CountDealerOrdersByStatusesSinceAsync(
        Guid dealerAccountId,
        IReadOnlyCollection<OrderStatus> statuses,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
    {
        if (statuses is null || statuses.Count == 0)
        {
            return Task.FromResult(0);
        }
        return _context.Orders
            .AsNoTracking()
            .Where(o => o.OriginDealerAccountId == dealerAccountId
                && o.OrderDate >= sinceUtc
                && statuses.Contains(o.Status))
            .CountAsync(cancellationToken);
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

    public async Task<IReadOnlyList<StatusGroup>> GetOrderStatusBreakdownAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Total = g.Sum(o => o.Total) })
            .ToListAsync(cancellationToken);
        return rows.Select(r => new StatusGroup(r.Status.ToString(), r.Count, r.Total)).ToList();
    }

    public async Task<(int OrderCount, decimal OrderTotal, DateTime? FirstOrderAt, DateTime? LastOrderAt)>
        GetOrderTotalsExtendedAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var result = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Total = g.Sum(o => o.Total),
                First = g.Min(o => (DateTime?)o.OrderDate),
                Last = g.Max(o => (DateTime?)o.OrderDate),
            })
            .FirstOrDefaultAsync(cancellationToken);
        return result is null
            ? (0, 0m, null, null)
            : (result.Count, result.Total, result.First, result.Last);
    }
}
