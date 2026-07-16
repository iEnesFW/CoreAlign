using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.GlassPlates;

public class StorageLocation : TenantEntity
{
    public Guid WarehouseId { get; private set; }
    public Guid? ParentLocationId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public StorageLocationKind Kind { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? Notes { get; private set; }

    public Warehouse Warehouse { get; set; } = null!;
    public StorageLocation? ParentLocation { get; set; }

    protected StorageLocation() { }

    public StorageLocation(
        Guid warehouseId,
        string code,
        string name,
        StorageLocationKind kind,
        Guid? parentLocationId = null,
        string? notes = null)
    {
        WarehouseId = warehouseId;
        Code = code.Trim();
        Name = name.Trim();
        Kind = kind;
        ParentLocationId = parentLocationId;
        Notes = notes;
    }

    public void Update(string code, string name, StorageLocationKind kind, Guid? parentLocationId, bool isActive, string? notes)
    {
        if (parentLocationId == Id)
        {
            throw new ArgumentException("A storage location cannot reference itself as parent.", nameof(parentLocationId));
        }
        Code = code.Trim();
        Name = name.Trim();
        Kind = kind;
        ParentLocationId = parentLocationId;
        IsActive = isActive;
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
