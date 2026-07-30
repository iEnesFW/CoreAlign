using CoreAlign.Domain.Entities.GlassEnclosure;

namespace CoreAlign.Application.GlassEnclosure.Services;

public record ThermalAcousticPanelInput(Guid PanelId, decimal AreaM2, decimal UValue, decimal SoundDb);

public record ThermalAcousticResult(
    decimal TotalAreaM2,
    decimal WeightedUValue,
    decimal WeightedSoundDb,
    decimal EstimatedWinterHeatLossKwh,
    decimal EstimatedDbReductionVsOpen);

public interface IThermalAcousticCalculator
{
    ThermalAcousticResult Calculate(GlassProject project, IEnumerable<ThermalAcousticPanelInput> panels, ClimateZone? climateZone);
}

public class ThermalAcousticCalculator : IThermalAcousticCalculator
{
    private const decimal OpenDbReference = 22m;
    private const decimal AnnualHeatingHours = 1800m;

    public ThermalAcousticResult Calculate(
        GlassProject project,
        IEnumerable<ThermalAcousticPanelInput> panels,
        ClimateZone? climateZone)
    {
        var list = panels.ToList();
        if (list.Count == 0)
        {
            return new ThermalAcousticResult(0m, 0m, 0m, 0m, 0m);
        }

        var totalArea = list.Sum(p => p.AreaM2);
        if (totalArea <= 0m)
        {
            return new ThermalAcousticResult(0m, 0m, 0m, 0m, 0m);
        }

        var weightedUNumerator = list.Sum(p => p.AreaM2 * p.UValue);
        var weightedU = weightedUNumerator / totalArea;

        // Composite sound reduction is averaged over the TRANSMISSION coefficient τ = 10^(−Rw/10),
        // then converted back with Rw = −10·log10(τ̄). Averaging 10^(+Rw/10) instead let the BEST
        // pane dominate, so a mixed assembly was quoted several dB better than it performs — for
        // equal areas of 30 dB and 40 dB glass the honest answer is 32.6 dB, not 37.4 dB.
        // Acoustics is governed by the weakest element, never the strongest.
        var tauSum = list.Sum(p => p.AreaM2 * (decimal)Math.Pow(10, -(double)p.SoundDb / 10));
        var weightedDb = tauSum > 0
            ? -10m * (decimal)Math.Log10((double)(tauSum / totalArea))
            : 0m;

        var deltaTemp = climateZone is null ? 12m : Math.Max(0m, 22m - climateZone.AvgWinterTemperatureC);
        // Q = U·A·ΔT·t is the heat LOST through the glazing, not a saving — a saving would need a
        // baseline to subtract from. It was reported as "energy savings", so a WORSE (higher-U)
        // glass read as a BIGGER saving and the field argued against its own specification.
        var heatLossKwh = totalArea * weightedU * deltaTemp * AnnualHeatingHours / 1000m;
        var reductionDb = Math.Max(0m, weightedDb - OpenDbReference);

        _ = project;

        return new ThermalAcousticResult(
            decimal.Round(totalArea, 3),
            decimal.Round(weightedU, 3),
            decimal.Round(weightedDb, 2),
            decimal.Round(heatLossKwh, 2),
            decimal.Round(reductionDb, 2));
    }
}
