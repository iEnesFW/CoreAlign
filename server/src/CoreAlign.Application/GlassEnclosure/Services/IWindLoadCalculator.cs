using CoreAlign.Domain.Entities.GlassEnclosure;

namespace CoreAlign.Application.GlassEnclosure.Services;

public record WindLoadPanelInput(Guid RunId, Guid PanelId, decimal AreaM2, int CurrentGlassThicknessMm);

public record WindLoadPanelResult(
    Guid RunId,
    Guid PanelId,
    decimal AppliedPressurePa,
    int CurrentThicknessMm,
    int RequiredMinThicknessMm,
    bool IsSufficient);

public record WindLoadResult(
    decimal BasePressurePa,
    decimal HeightFactor,
    decimal AppliedPressurePa,
    IReadOnlyList<WindLoadPanelResult> Panels);

public interface IWindLoadCalculator
{
    WindLoadResult Calculate(WindZone zone, decimal buildingHeightM, IEnumerable<WindLoadPanelInput> panels);
}

public class WindLoadCalculator : IWindLoadCalculator
{
    private static readonly (int ThicknessMm, decimal AllowablePa)[] AllowableTable =
    {
        (4, 800),
        (5, 1000),
        (6, 1250),
        (8, 1800),
        (10, 2400),
        (12, 3100),
        (15, 4200),
        (19, 6000),
    };

    public WindLoadResult Calculate(WindZone zone, decimal buildingHeightM, IEnumerable<WindLoadPanelInput> panels)
    {
        if (zone is null) throw new ArgumentNullException(nameof(zone));
        var heightM = Math.Max(0m, buildingHeightM);
        var heightFactor = 1m + (heightM / 100m) * zone.HeightFactorMultiplier;
        var applied = zone.BaseWindPressurePa * heightFactor;

        var results = new List<WindLoadPanelResult>();
        foreach (var panel in panels)
        {
            var requiredThickness = RequiredThicknessFor(applied);
            var isSufficient = panel.CurrentGlassThicknessMm >= requiredThickness;
            results.Add(new WindLoadPanelResult(
                panel.RunId,
                panel.PanelId,
                decimal.Round(applied, 2),
                panel.CurrentGlassThicknessMm,
                requiredThickness,
                isSufficient));
        }
        return new WindLoadResult(zone.BaseWindPressurePa, decimal.Round(heightFactor, 4), decimal.Round(applied, 2), results);
    }

    private static int RequiredThicknessFor(decimal appliedPa)
    {
        foreach (var (thickness, allowable) in AllowableTable)
        {
            if (allowable >= appliedPa) return thickness;
        }
        return AllowableTable[^1].ThicknessMm;
    }
}
