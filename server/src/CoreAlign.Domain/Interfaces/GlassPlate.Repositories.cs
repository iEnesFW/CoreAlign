using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassPlates;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public record GlassLowStockRow(
    Guid ProductId,
    string Sku,
    string ProductName,
    Guid WarehouseId,
    string WarehouseName,
    int AvailableCount,
    int MinPlateCount);

public interface IGlassPlateRepository
{
    Task AddAsync(GlassPlate plate, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<GlassPlate> plates, CancellationToken cancellationToken = default);
    Task<GlassPlate?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> PlateNumberExistsAsync(Guid tenantId, string plateNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetExistingPlateNumbersAsync(Guid tenantId, IReadOnlyCollection<string> plateNumbers, CancellationToken cancellationToken = default);
    Task<int> CountAvailableAsync(Guid tenantId, Guid productId, Guid warehouseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GlassPlate>> ListAsync(
        Guid tenantId,
        Guid? productId,
        Guid? warehouseId,
        Guid? storageLocationId,
        GlassPlateStatus? status,
        PlateKind? kind,
        IReadOnlyCollection<Guid>? allowedWarehouseIds,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GlassPlate>> FindUsableForCutAsync(
        Guid tenantId,
        Guid productId,
        decimal requiredWidthMm,
        decimal requiredHeightMm,
        IReadOnlyCollection<Guid>? allowedWarehouseIds,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GlassLowStockRow>> GetLowStockAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid>? allowedWarehouseIds,
        CancellationToken cancellationToken = default);
}

public interface IStorageLocationRepository
{
    Task AddAsync(StorageLocation location, CancellationToken cancellationToken = default);
    Task<StorageLocation?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(Guid tenantId, Guid warehouseId, string code, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StorageLocation>> ListAsync(Guid tenantId, Guid? warehouseId, CancellationToken cancellationToken = default);
}

public interface IGlassPlateConsumptionRepository
{
    Task AddAsync(GlassPlateConsumption consumption, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlassPlateConsumption>> ListByPlateAsync(Guid tenantId, Guid glassPlateId, CancellationToken cancellationToken = default);
}

public interface IUserWarehouseAccessRepository
{
    Task<IReadOnlyList<Guid>> GetWarehouseIdsByUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserWarehouseAccess>> ListByUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserWarehouseAccess access, CancellationToken cancellationToken = default);
    void RemoveRange(IEnumerable<UserWarehouseAccess> items);
}
