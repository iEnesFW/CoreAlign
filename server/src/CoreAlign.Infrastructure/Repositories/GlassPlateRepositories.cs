using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassPlates;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public sealed class GlassPlateRepository : IGlassPlateRepository
{
    private readonly CoreAlignDbContext _context;

    public GlassPlateRepository(CoreAlignDbContext context) => _context = context;

    public async Task AddAsync(GlassPlate plate, CancellationToken cancellationToken = default)
        => await _context.Set<GlassPlate>().AddAsync(plate, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<GlassPlate> plates, CancellationToken cancellationToken = default)
        => await _context.Set<GlassPlate>().AddRangeAsync(plates, cancellationToken);

    public Task<GlassPlate?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        => _context.Set<GlassPlate>().FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == id, cancellationToken);

    public Task<bool> PlateNumberExistsAsync(Guid tenantId, string plateNumber, CancellationToken cancellationToken = default)
        => _context.Set<GlassPlate>().AnyAsync(p => p.TenantId == tenantId && p.PlateNumber == plateNumber, cancellationToken);

    public async Task<IReadOnlyList<string>> GetExistingPlateNumbersAsync(Guid tenantId, IReadOnlyCollection<string> plateNumbers, CancellationToken cancellationToken = default)
        => await _context.Set<GlassPlate>()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && plateNumbers.Contains(p.PlateNumber))
            .Select(p => p.PlateNumber)
            .ToListAsync(cancellationToken);

    public Task<int> CountAvailableAsync(Guid tenantId, Guid productId, Guid warehouseId, CancellationToken cancellationToken = default)
        => _context.Set<GlassPlate>().CountAsync(
            p => p.TenantId == tenantId
                && p.ProductId == productId
                && p.WarehouseId == warehouseId
                && p.Status == GlassPlateStatus.Available,
            cancellationToken);

    public async Task<IReadOnlyList<GlassPlate>> ListAsync(
        Guid tenantId,
        Guid? productId,
        Guid? warehouseId,
        Guid? storageLocationId,
        GlassPlateStatus? status,
        PlateKind? kind,
        IReadOnlyCollection<Guid>? allowedWarehouseIds,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<GlassPlate>()
            .AsNoTracking()
            .Include(p => p.Warehouse)
            .Include(p => p.StorageLocation)
            .Where(p => p.TenantId == tenantId);

        if (productId.HasValue) query = query.Where(p => p.ProductId == productId.Value);
        if (warehouseId.HasValue) query = query.Where(p => p.WarehouseId == warehouseId.Value);
        if (storageLocationId.HasValue) query = query.Where(p => p.StorageLocationId == storageLocationId.Value);
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        if (kind.HasValue) query = query.Where(p => p.Kind == kind.Value);
        if (allowedWarehouseIds is not null) query = query.Where(p => allowedWarehouseIds.Contains(p.WarehouseId));

        return await query
            .OrderByDescending(p => p.ReceivedAtUtc)
            .ThenByDescending(p => p.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GlassPlate>> FindUsableForCutAsync(
        Guid tenantId,
        Guid productId,
        decimal requiredWidthMm,
        decimal requiredHeightMm,
        IReadOnlyCollection<Guid>? allowedWarehouseIds,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<GlassPlate>()
            .AsNoTracking()
            .Include(p => p.Warehouse)
            .Include(p => p.StorageLocation)
            .Where(p => p.TenantId == tenantId
                && p.ProductId == productId
                && p.Status == GlassPlateStatus.Available);

        if (allowedWarehouseIds is not null) query = query.Where(p => allowedWarehouseIds.Contains(p.WarehouseId));

        query = query.Where(p =>
            (p.WidthMm >= requiredWidthMm && p.HeightMm >= requiredHeightMm)
            || (p.WidthMm >= requiredHeightMm && p.HeightMm >= requiredWidthMm));

        return await query
            .OrderBy(p => p.RemainingAreaMm2)
            .ThenBy(p => p.ReceivedAtUtc)
            .ThenBy(p => p.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GlassLowStockRow>> GetLowStockAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid>? allowedWarehouseIds,
        CancellationToken cancellationToken = default)
    {
        var available = _context.Set<GlassPlate>()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.Status == GlassPlateStatus.Available);
        if (allowedWarehouseIds is not null)
        {
            available = available.Where(p => allowedWarehouseIds.Contains(p.WarehouseId));
        }

        var counts = await available
            .GroupBy(p => new { p.ProductId, p.WarehouseId })
            .Select(g => new { g.Key.ProductId, g.Key.WarehouseId, Count = g.Count() })
            .ToListAsync(cancellationToken);
        if (counts.Count == 0)
        {
            return Array.Empty<GlassLowStockRow>();
        }

        var productIds = counts.Select(c => c.ProductId).Distinct().ToList();
        var warehouseIds = counts.Select(c => c.WarehouseId).Distinct().ToList();

        var products = await _context.Set<Product>()
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && p.IsPlateTracked && p.MinPlateCount != null)
            .Select(p => new { p.Id, p.Sku, p.Name, MinPlateCount = p.MinPlateCount!.Value })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var warehouses = await _context.Set<Warehouse>()
            .AsNoTracking()
            .Where(w => warehouseIds.Contains(w.Id))
            .Select(w => new { w.Id, w.Name })
            .ToDictionaryAsync(w => w.Id, cancellationToken);

        var result = new List<GlassLowStockRow>();
        foreach (var c in counts)
        {
            if (!products.TryGetValue(c.ProductId, out var product) || c.Count > product.MinPlateCount)
            {
                continue;
            }
            var warehouseName = warehouses.TryGetValue(c.WarehouseId, out var wh) ? wh.Name : string.Empty;
            result.Add(new GlassLowStockRow(
                c.ProductId, product.Sku, product.Name, c.WarehouseId, warehouseName, c.Count, product.MinPlateCount));
        }
        return result;
    }
}

public sealed class StorageLocationRepository : IStorageLocationRepository
{
    private readonly CoreAlignDbContext _context;

    public StorageLocationRepository(CoreAlignDbContext context) => _context = context;

    public async Task AddAsync(StorageLocation location, CancellationToken cancellationToken = default)
        => await _context.Set<StorageLocation>().AddAsync(location, cancellationToken);

    public Task<StorageLocation?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        => _context.Set<StorageLocation>().FirstOrDefaultAsync(l => l.TenantId == tenantId && l.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(Guid tenantId, Guid warehouseId, string code, Guid? excludeId, CancellationToken cancellationToken = default)
        => _context.Set<StorageLocation>().AnyAsync(
            l => l.TenantId == tenantId
                && l.WarehouseId == warehouseId
                && l.Code == code
                && (excludeId == null || l.Id != excludeId),
            cancellationToken);

    public async Task<IReadOnlyList<StorageLocation>> ListAsync(Guid tenantId, Guid? warehouseId, CancellationToken cancellationToken = default)
        => await _context.Set<StorageLocation>()
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId && (warehouseId == null || l.WarehouseId == warehouseId))
            .OrderBy(l => l.Code)
            .ToListAsync(cancellationToken);
}

public sealed class GlassPlateConsumptionRepository : IGlassPlateConsumptionRepository
{
    private readonly CoreAlignDbContext _context;

    public GlassPlateConsumptionRepository(CoreAlignDbContext context) => _context = context;

    public async Task AddAsync(GlassPlateConsumption consumption, CancellationToken cancellationToken = default)
        => await _context.Set<GlassPlateConsumption>().AddAsync(consumption, cancellationToken);

    public async Task<IReadOnlyList<GlassPlateConsumption>> ListByPlateAsync(Guid tenantId, Guid glassPlateId, CancellationToken cancellationToken = default)
        => await _context.Set<GlassPlateConsumption>()
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.GlassPlateId == glassPlateId)
            .OrderByDescending(c => c.OccurredAtUtc)
            .ThenByDescending(c => c.Id)
            .ToListAsync(cancellationToken);
}

public sealed class UserWarehouseAccessRepository : IUserWarehouseAccessRepository
{
    private readonly CoreAlignDbContext _context;

    public UserWarehouseAccessRepository(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<Guid>> GetWarehouseIdsByUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
        => await _context.Set<UserWarehouseAccess>()
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.UserId == userId)
            .Select(a => a.WarehouseId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UserWarehouseAccess>> ListByUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
        => await _context.Set<UserWarehouseAccess>()
            .Where(a => a.TenantId == tenantId && a.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(UserWarehouseAccess access, CancellationToken cancellationToken = default)
        => await _context.Set<UserWarehouseAccess>().AddAsync(access, cancellationToken);

    public void RemoveRange(IEnumerable<UserWarehouseAccess> items)
        => _context.Set<UserWarehouseAccess>().RemoveRange(items);
}
