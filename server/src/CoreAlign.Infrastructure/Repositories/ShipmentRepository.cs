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
            .AsNoTracking()
            .Include(s => s.Warehouse)
            .Include(s => s.Lines)
            .Where(s => s.OrderId == orderId)
            .OrderByDescending(s => s.CreatedDate)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<ShipmentSearchRow> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        Guid? orderId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Shipments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = $"%{search.Trim().ToLower()}%";
            if (_context.Database.IsNpgsql())
            {
                query = query.Where(s =>
                    EF.Functions.ILike(s.ShipmentNumber, lower) ||
                    (s.TrackingNumber != null && EF.Functions.ILike(s.TrackingNumber, lower)));
            }
            else
            {
                query = query.Where(s =>
                    EF.Functions.Like(s.ShipmentNumber.ToLower(), lower) ||
                    (s.TrackingNumber != null && EF.Functions.Like(s.TrackingNumber.ToLower(), lower)));
            }
        }

        if (customerId.HasValue) query = query.Where(s => s.CustomerId == customerId.Value);
        if (orderId.HasValue) query = query.Where(s => s.OrderId == orderId.Value);

        var total = await query.CountAsync(cancellationToken);
        // Project to slim row — skips ShipmentLines, ShippingAddressSnapshot
        // (owned) and the warehouse entity beyond its name.
        var items = await query
            .OrderByDescending(s => s.CreatedDate)
            .ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ShipmentSearchRow(
                s.Id,
                s.ShipmentNumber,
                s.OrderId,
                s.CustomerId,
                s.WarehouseId,
                s.Warehouse != null ? s.Warehouse.Name : null,
                s.Status,
                s.CreatedDate,
                s.PickedAtUtc,
                s.PackedAtUtc,
                s.DispatchedAtUtc,
                s.DeliveredAtUtc,
                s.CancelledAtUtc,
                s.CarrierName,
                s.TrackingNumber,
                s.TrackingUrl,
                s.ShippingCost,
                s.ReceivedBy,
                s.Notes,
                s.CancelReason,
                s.CreatedAtUtc,
                s.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default) =>
        await _context.Shipments.AddAsync(shipment, cancellationToken);

    public void Update(Shipment shipment) => _context.Shipments.Update(shipment);
    public void Remove(Shipment shipment) => _context.Shipments.Remove(shipment);
}
