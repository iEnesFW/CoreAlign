using CoreAlign.Application.GlassEnclosure.Engineering;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.Services;

/// <param name="AreaM2">Net glass area, for reporting. The CHECK uses the spans below.</param>
/// <param name="WidthMm">Pane width — one of the two plate spans.</param>
/// <param name="HeightMm">Pane height — the other plate span.</param>
/// <param name="Structure">Product family, which decides the characteristic bending strength.</param>
/// <param name="LayerThicknessesMm">Laminate plies, if the catalogue records them.</param>
public record WindLoadPanelInput(
    Guid RunId,
    Guid PanelId,
    decimal AreaM2,
    int CurrentGlassThicknessMm,
    decimal WidthMm = 0m,
    decimal HeightMm = 0m,
    GlassStructure Structure = GlassStructure.Tempered,
    IReadOnlyList<decimal>? LayerThicknessesMm = null);

public record WindLoadPanelResult(
    Guid RunId,
    Guid PanelId,
    decimal AppliedPressurePa,
    int CurrentThicknessMm,
    int RequiredMinThicknessMm,
    bool IsSufficient,
    decimal ShortSpanMm = 0m,
    decimal AspectRatio = 1m,
    decimal MaxBendingStressMPa = 0m,
    decimal DesignStrengthMPa = 0m,
    decimal StressUtilisation = 0m,
    decimal MaxDeflectionMm = 0m,
    decimal DeflectionLimitMm = 0m,
    decimal DeflectionUtilisation = 0m,
    string GoverningLimit = "Strength");

public record WindLoadResult(
    decimal BasePressurePa,
    decimal HeightFactor,
    decimal AppliedPressurePa,
    IReadOnlyList<WindLoadPanelResult> Panels)
{
    /// <summary>The full EN 1991-1-4 chain, so a reviewer can retrace every number.</summary>
    public WindSitePressure? Site { get; init; }

    public decimal PeakVelocityPressurePa { get; init; }
    public decimal ExternalPressureCoefficient { get; init; }
    public decimal InternalPressureCoefficient { get; init; }
    public WindTerrainCategory Terrain { get; init; } = WindTerrainCategory.Country;
    public string StandardReference { get; init; } = string.Empty;
}

public interface IWindLoadCalculator
{
    WindLoadResult Calculate(
        WindZone zone,
        decimal buildingHeightM,
        IEnumerable<WindLoadPanelInput> panels,
        WindTerrainCategory terrain = WindTerrainCategory.Country);
}

/// <summary>
/// Site pressure from TS EN 1991-1-4, pane resistance from EN 16612 + plate theory.
///
/// WHY the rewrite: the old model was <c>storedBasePressure × (1 + height/100)</c> against a flat
/// "allowable pascals per thickness" table. It ignored terrain, turbulence, pressure coefficients
/// AND the pane's own geometry — the per-panel table repeated one global verdict for every pane in
/// the project, so a 4 m² fixed light and a 0.4 m² vent were reported identically. Bending stress
/// scales with the square of the SHORT span; none of that was in the old arithmetic.
/// </summary>
public class WindLoadCalculator : IWindLoadCalculator
{
    /// <summary>
    /// Thicknesses a fabricator actually stocks. A calculated requirement is rounded UP to one of
    /// these — rounding down would specify glass that cannot be bought and would not pass.
    /// </summary>
    private static readonly int[] StockThicknessesMm = { 4, 5, 6, 8, 10, 12, 15, 19, 25 };

    public WindLoadResult Calculate(
        WindZone zone,
        decimal buildingHeightM,
        IEnumerable<WindLoadPanelInput> panels,
        WindTerrainCategory terrain = WindTerrainCategory.Country)
    {
        ArgumentNullException.ThrowIfNull(zone);
        var list = (panels ?? Array.Empty<WindLoadPanelInput>()).ToList();

        var coefficients = WindPressureCoefficients.EnclosureDefault;
        var site = EurocodeWindPressure.Compute(
            new WindSiteInput(
                BasicWindSpeedForZone(zone),
                terrain,
                Math.Max(0m, buildingHeightM)));
        var applied = EurocodeWindPressure.GoverningNetPressurePa(
            site.PeakVelocityPressurePa,
            coefficients);

        var results = new List<WindLoadPanelResult>(list.Count);
        foreach (var panel in list)
        {
            // A caller that has not been taught the spans yet still gets a usable check: fall back
            // to a square pane of the reported area rather than silently reporting nothing.
            var widthMm = panel.WidthMm > 0m ? panel.WidthMm : FallbackSpanMm(panel.AreaM2);
            var heightMm = panel.HeightMm > 0m ? panel.HeightMm : FallbackSpanMm(panel.AreaM2);

            var strength = GlassPlateResistance.StrengthFor(panel.Structure);
            var shortSpan = Math.Max(1m, Math.Min(widthMm, heightMm));
            var longSpan = Math.Max(shortSpan, Math.Max(widthMm, heightMm));
            var pane = new GlassPaneGeometry(
                shortSpan,
                longSpan,
                panel.CurrentGlassThicknessMm,
                panel.LayerThicknessesMm ?? Array.Empty<decimal>());
            var check = GlassPlateResistance.Check(pane, applied, strength);

            var required = RoundUpToStock(
                Math.Max(check.RequiredMonolithicThicknessMm, DeflectionDrivenThicknessMm(check)));

            results.Add(new WindLoadPanelResult(
                panel.RunId,
                panel.PanelId,
                applied,
                panel.CurrentGlassThicknessMm,
                required,
                check.IsSufficient && panel.CurrentGlassThicknessMm >= required,
                decimal.Round(shortSpan, 1),
                decimal.Round(longSpan / shortSpan, 3),
                check.MaxBendingStressMPa,
                check.DesignStrengthMPa,
                check.StressUtilisation,
                check.MaxDeflectionMm,
                check.DeflectionLimitMm,
                check.DeflectionUtilisation,
                check.DeflectionUtilisation > check.StressUtilisation ? "Deflection" : "Strength"));
        }

        return new WindLoadResult(
            decimal.Round(site.BasicVelocityPressurePa, 2),
            // Kept for the existing contract: how much the site profile amplifies the basic pressure.
            site.BasicVelocityPressurePa == 0m
                ? 1m
                : decimal.Round(site.PeakVelocityPressurePa / site.BasicVelocityPressurePa, 4),
            applied,
            results)
        {
            Site = site,
            PeakVelocityPressurePa = site.PeakVelocityPressurePa,
            ExternalPressureCoefficient = coefficients.ExternalSuctionCpe,
            InternalPressureCoefficient = coefficients.InternalPositiveCpi,
            Terrain = terrain,
            StandardReference = "TS EN 1991-1-4 + EN 16612",
        };
    }

    /// <summary>
    /// A zone that predates the Eurocode chain only stores a pressure. Invert q_b = ½ρv² to get a
    /// usable basic speed rather than refusing to report anything — the number is then no better
    /// than the pressure it came from, which is exactly what that zone record already claimed.
    /// </summary>
    private static decimal BasicWindSpeedForZone(WindZone zone)
    {
        if (zone.BasicWindSpeedMs > 0m) return zone.BasicWindSpeedMs;
        if (zone.BaseWindPressurePa <= 0m) return 28m;
        return (decimal)Math.Sqrt((double)(zone.BaseWindPressurePa * 2m / 1.25m));
    }

    private static decimal FallbackSpanMm(decimal areaM2) =>
        areaM2 > 0m ? (decimal)Math.Sqrt((double)areaM2) * 1000m : 1000m;

    /// <summary>Recover the thickness deflection alone would demand: w scales with 1/t³.</summary>
    private static decimal DeflectionDrivenThicknessMm(GlassPlateCheck check)
    {
        if (check.DeflectionUtilisation <= 1m) return 0m;
        var scale = (decimal)Math.Cbrt((double)check.DeflectionUtilisation);
        return check.EffectiveThicknessForDeflectionMm * scale;
    }

    private static int RoundUpToStock(decimal requiredMm)
    {
        foreach (var t in StockThicknessesMm)
        {
            if (t >= requiredMm) return t;
        }
        return StockThicknessesMm[^1];
    }
}
