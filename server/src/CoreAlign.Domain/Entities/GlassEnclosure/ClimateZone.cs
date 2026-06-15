using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class ClimateZone
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Code { get; private set; } = string.Empty;
    public string NameTr { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public decimal AvgWinterTemperatureC { get; private set; }
    public decimal AvgHumidityPercent { get; private set; }
    public CorrosionClass CorrosionClass { get; private set; } = CorrosionClass.C2;
    public bool RecommendsDoubleGlazing { get; private set; }
    public bool RecommendsCorrosionResistantCoating { get; private set; }
    public bool RecommendsSeismicSmallerPanel { get; private set; }
    public string IlPostalPrefixListJson { get; private set; } = "[]";
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    protected ClimateZone() { }

    public ClimateZone(
        string code,
        string nameTr,
        string nameEn,
        decimal avgWinterTemperatureC,
        decimal avgHumidityPercent,
        CorrosionClass corrosionClass,
        bool recommendsDoubleGlazing,
        bool recommendsCorrosionResistantCoating,
        bool recommendsSeismicSmallerPanel,
        string ilPostalPrefixListJson)
    {
        Code = code;
        NameTr = nameTr;
        NameEn = nameEn;
        AvgWinterTemperatureC = avgWinterTemperatureC;
        AvgHumidityPercent = avgHumidityPercent;
        CorrosionClass = corrosionClass;
        RecommendsDoubleGlazing = recommendsDoubleGlazing;
        RecommendsCorrosionResistantCoating = recommendsCorrosionResistantCoating;
        RecommendsSeismicSmallerPanel = recommendsSeismicSmallerPanel;
        IlPostalPrefixListJson = ilPostalPrefixListJson;
    }

    public void Update(
        string nameTr,
        string nameEn,
        decimal avgWinterTemperatureC,
        decimal avgHumidityPercent,
        CorrosionClass corrosionClass,
        bool recommendsDoubleGlazing,
        bool recommendsCorrosionResistantCoating,
        bool recommendsSeismicSmallerPanel,
        string ilPostalPrefixListJson,
        bool isActive)
    {
        NameTr = nameTr;
        NameEn = nameEn;
        AvgWinterTemperatureC = avgWinterTemperatureC;
        AvgHumidityPercent = avgHumidityPercent;
        CorrosionClass = corrosionClass;
        RecommendsDoubleGlazing = recommendsDoubleGlazing;
        RecommendsCorrosionResistantCoating = recommendsCorrosionResistantCoating;
        RecommendsSeismicSmallerPanel = recommendsSeismicSmallerPanel;
        IlPostalPrefixListJson = ilPostalPrefixListJson;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
