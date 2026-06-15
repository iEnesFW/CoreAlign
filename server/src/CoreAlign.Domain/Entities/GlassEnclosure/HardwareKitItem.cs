using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class HardwareKitItem : TenantEntity
{
    public Guid KitId { get; private set; }
    public Guid HardwareItemId { get; private set; }
    public string QuantityFormula { get; private set; } = "1";
    public string? ConditionExpression { get; private set; }
    public string? Note { get; private set; }
    public int SortOrder { get; private set; }

    protected HardwareKitItem() { }

    public HardwareKitItem(
        Guid kitId,
        Guid hardwareItemId,
        string quantityFormula,
        string? conditionExpression = null,
        string? note = null,
        int sortOrder = 0)
    {
        KitId = kitId;
        HardwareItemId = hardwareItemId;
        QuantityFormula = quantityFormula;
        ConditionExpression = conditionExpression;
        Note = note;
        SortOrder = sortOrder;
    }

    public void Update(string quantityFormula, string? conditionExpression, string? note, int sortOrder)
    {
        QuantityFormula = quantityFormula;
        ConditionExpression = conditionExpression;
        Note = note;
        SortOrder = sortOrder;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
