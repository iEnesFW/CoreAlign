using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class Warehouse : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public WarehouseType Type { get; private set; } = WarehouseType.Main;
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? PostalCode { get; private set; }
    public string? Country { get; private set; }
    public string? Phone { get; private set; }
    public Guid? ManagerUserId { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected Warehouse() { }

    public Warehouse(string code, string name, WarehouseType type = WarehouseType.Main, bool isDefault = false)
    {
        Code = code;
        Name = name;
        Type = type;
        IsDefault = isDefault;
    }

    public void Update(
        string code,
        string name,
        WarehouseType type,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? state,
        string? postalCode,
        string? country,
        string? phone,
        Guid? managerUserId,
        bool isDefault,
        bool isActive)
    {
        Code = code;
        Name = name;
        Type = type;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
        Phone = phone;
        ManagerUserId = managerUserId;
        IsDefault = isDefault;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
