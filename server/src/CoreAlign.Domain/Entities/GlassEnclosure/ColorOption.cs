using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class ColorOption : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? RalCode { get; private set; }
    public string HexColor { get; private set; } = "#FFFFFF";
    public ColorFinishType FinishType { get; private set; } = ColorFinishType.PowderCoated;
    public decimal PriceModifierPercent { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected ColorOption() { }

    public ColorOption(
        string code,
        string name,
        string hexColor,
        ColorFinishType finishType,
        string? ralCode = null,
        decimal priceModifierPercent = 0m,
        int sortOrder = 0)
    {
        Code = code;
        Name = name;
        HexColor = hexColor;
        FinishType = finishType;
        RalCode = ralCode;
        PriceModifierPercent = priceModifierPercent;
        SortOrder = sortOrder;
    }

    public void Update(
        string name,
        string hexColor,
        ColorFinishType finishType,
        string? ralCode,
        decimal priceModifierPercent,
        int sortOrder,
        bool isActive)
    {
        Name = name;
        HexColor = hexColor;
        FinishType = finishType;
        RalCode = ralCode;
        PriceModifierPercent = priceModifierPercent;
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
