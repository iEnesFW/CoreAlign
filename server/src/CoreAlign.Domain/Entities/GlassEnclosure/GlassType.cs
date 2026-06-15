using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassType : TenantEntity, ICatalogLinkable
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int ThicknessMm { get; private set; }
    public GlassStructure Structure { get; private set; } = GlassStructure.Tempered;
    public string GlassLayersJson { get; private set; } = "[]";
    public decimal UValue { get; private set; }
    public decimal SoundDb { get; private set; }
    public decimal MaxPanelAreaM2 { get; private set; }
    public decimal AllowablePressurePa { get; private set; }
    public decimal WeightKgPerM2 { get; private set; }
    public decimal PricePerM2 { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public Guid? LinkedProductId { get; set; }
    public bool IsActive { get; private set; } = true;

    string ICatalogLinkable.Unit => "M2";
    decimal ICatalogLinkable.UnitCost => PricePerM2;

    protected GlassType() { }

    public GlassType(
        string code,
        string name,
        int thicknessMm,
        GlassStructure structure,
        decimal pricePerM2,
        decimal weightKgPerM2,
        decimal allowablePressurePa,
        decimal maxPanelAreaM2,
        decimal uValue,
        decimal soundDb,
        string glassLayersJson = "[]",
        string currency = "TRY",
        Guid? linkedProductId = null)
    {
        Code = code;
        Name = name;
        ThicknessMm = thicknessMm;
        Structure = structure;
        PricePerM2 = pricePerM2;
        WeightKgPerM2 = weightKgPerM2;
        AllowablePressurePa = allowablePressurePa;
        MaxPanelAreaM2 = maxPanelAreaM2;
        UValue = uValue;
        SoundDb = soundDb;
        GlassLayersJson = glassLayersJson;
        Currency = currency;
        LinkedProductId = linkedProductId;
    }

    public void UpdatePricePerM2(decimal pricePerM2)
    {
        PricePerM2 = pricePerM2;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Update(
        string name,
        int thicknessMm,
        GlassStructure structure,
        decimal pricePerM2,
        decimal weightKgPerM2,
        decimal allowablePressurePa,
        decimal maxPanelAreaM2,
        decimal uValue,
        decimal soundDb,
        string glassLayersJson,
        string currency,
        Guid? linkedProductId,
        bool isActive)
    {
        Name = name;
        ThicknessMm = thicknessMm;
        Structure = structure;
        PricePerM2 = pricePerM2;
        WeightKgPerM2 = weightKgPerM2;
        AllowablePressurePa = allowablePressurePa;
        MaxPanelAreaM2 = maxPanelAreaM2;
        UValue = uValue;
        SoundDb = soundDb;
        GlassLayersJson = glassLayersJson;
        Currency = currency;
        LinkedProductId = linkedProductId;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
