namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class WindZone
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Code { get; private set; } = string.Empty;
    public string RegionLabelTr { get; private set; } = string.Empty;
    public string RegionLabelEn { get; private set; } = string.Empty;
    public decimal BaseWindPressurePa { get; private set; }
    /// <summary>
    /// v_b,0 — the 10 min mean wind speed at 10 m over terrain category II, from the national
    /// wind map. This is what TS EN 1991-1-4 actually starts from; BaseWindPressurePa predates
    /// the Eurocode chain and is kept so existing zones keep reporting until they are surveyed.
    /// </summary>
    public decimal BasicWindSpeedMs { get; private set; }
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
        bool isCoastal,
        decimal basicWindSpeedMs = 0m)
    {
        Code = code;
        BasicWindSpeedMs = basicWindSpeedMs;
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
        bool isActive,
        decimal basicWindSpeedMs = 0m)
    {
        RegionLabelTr = regionLabelTr;
        BasicWindSpeedMs = basicWindSpeedMs;
        RegionLabelEn = regionLabelEn;
        BaseWindPressurePa = baseWindPressurePa;
        HeightFactorMultiplier = heightFactorMultiplier;
        IsCoastal = isCoastal;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
