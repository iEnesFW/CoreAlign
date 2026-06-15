namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class WindZone
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Code { get; private set; } = string.Empty;
    public string RegionLabelTr { get; private set; } = string.Empty;
    public string RegionLabelEn { get; private set; } = string.Empty;
    public decimal BaseWindPressurePa { get; private set; }
    public decimal HeightFactorMultiplier { get; private set; } = 1m;
    public bool IsCoastal { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    protected WindZone() { }

    public WindZone(
        string code,
        string regionLabelTr,
        string regionLabelEn,
        decimal baseWindPressurePa,
        decimal heightFactorMultiplier,
        bool isCoastal)
    {
        Code = code;
        RegionLabelTr = regionLabelTr;
        RegionLabelEn = regionLabelEn;
        BaseWindPressurePa = baseWindPressurePa;
        HeightFactorMultiplier = heightFactorMultiplier;
        IsCoastal = isCoastal;
    }

    public void Update(
        string regionLabelTr,
        string regionLabelEn,
        decimal baseWindPressurePa,
        decimal heightFactorMultiplier,
        bool isCoastal,
        bool isActive)
    {
        RegionLabelTr = regionLabelTr;
        RegionLabelEn = regionLabelEn;
        BaseWindPressurePa = baseWindPressurePa;
        HeightFactorMultiplier = heightFactorMultiplier;
        IsCoastal = isCoastal;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
