using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class StockReasonCode : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public StockReasonCategory Category { get; private set; }
    public bool AffectsCost { get; private set; } = true;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected StockReasonCode() { }

    public StockReasonCode(string code, string name, StockReasonCategory category, bool affectsCost = true, string? description = null)
    {
        Code = code;
        Name = name;
        Category = category;
        AffectsCost = affectsCost;
        Description = description;
    }

    public void Update(string code, string name, StockReasonCategory category, bool affectsCost, string? description, bool isActive)
    {
        Code = code;
        Name = name;
        Category = category;
        AffectsCost = affectsCost;
        Description = description;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
