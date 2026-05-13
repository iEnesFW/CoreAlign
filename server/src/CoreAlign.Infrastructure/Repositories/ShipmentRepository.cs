using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class ShipmentRepository : IShipmentRepository
{
    private readonly CoreAlignDbContext _context;
    public ShipmentRepository(CoreAlignDbContext context) => _context = context;

    public Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Shipments
            .Include(s => s.Warehouse)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Shipment?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Shipments
            .Include(s => s.Warehouse)
            .Include(s => s.Lines)
            .ThenInclude(l => l.Product)
            .Include(s => s.Lines)
            .ThenInclude(l => l.Lot)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Shipment>> GetByOrderAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        await _context.Shipments
            .Include(s => s.Warehouse)
            .Include(s => s.Lines)
            .Where(s => s.OrderId == orderId)
            .OrderByDescending(s => s.CreatedDate)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Shipment> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        Guid? orderId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Shipments.AsNoTracking().Include(s => s.Warehouse).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim().ToLower()}%";
            query = query.Where(s =>
                EF.Functions.Like(s.ShipmentNumber.ToLower(), pattern) ||
                (s.TrackingNumber != null && EF.Functions.Like(s.TrackingNumber.ToLower(), pattern)));
        }

        if (customerId.HasValue) query = query.Where(s => s.CustomerId == customerId.Value);
        if (orderId.HasValue) query = query.Where(s => s.OrderId == orderId.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default) =>
        await _context.Shipments.AddAsync(shipment, cancellationToken);

    public void Update(Shipment shipment) => _context.Shipments.Update(shipment);
    public void Remove(Shipment shipment) => _context.Shipments.Remove(shipment);
}
