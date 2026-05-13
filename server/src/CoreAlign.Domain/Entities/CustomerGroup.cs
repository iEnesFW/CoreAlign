using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class CustomerGroup : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? DefaultPriceListId { get; private set; }
    public decimal DefaultDiscountPercent { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected CustomerGroup() { }

    public CustomerGroup(string code, string name, string? description = null)
    {
        Code = code;
        Name = name;
        Description = description;
    }

    public void Update(
        string code,
        string name,
        string? description,
        Guid? defaultPriceListId,
        decimal defaultDiscountPercent,
        bool isActive)
    {
        Code = code;
        Name = name;
        Description = description;
        DefaultPriceListId = defaultPriceListId;
        DefaultDiscountPercent = defaultDiscountPercent;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
