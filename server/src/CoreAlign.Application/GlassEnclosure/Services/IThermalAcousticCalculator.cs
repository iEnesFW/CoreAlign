using CoreAlign.Domain.Entities.GlassEnclosure;

namespace CoreAlign.Application.GlassEnclosure.Services;

public record ThermalAcousticPanelInput(Guid PanelId, decimal AreaM2, decimal UValue, decimal SoundDb);

public record ThermalAcousticResult(
    decimal TotalAreaM2,
    decimal WeightedUValue,
    decimal WeightedSoundDb,
    decimal EstimatedWinterEnergySavingsKwh,
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

        var dbSum = list.Sum(p => p.AreaM2 * (decimal)Math.Pow(10, (double)p.SoundDb / 10));
        var weightedDb = dbSum > 0
            ? 10m * (decimal)Math.Log10((double)(dbSum / totalArea))
            : 0m;

        var deltaTemp = climateZone is null ? 12m : Math.Max(0m, 22m - climateZone.AvgWinterTemperatureC);
        var savingsKwh = totalArea * weightedU * deltaTemp * AnnualHeatingHours / 1000m;
        var reductionDb = Math.Max(0m, weightedDb - OpenDbReference);

        _ = project;

        return new ThermalAcousticResult(
            decimal.Round(totalArea, 3),
            decimal.Round(weightedU, 3),
            decimal.Round(weightedDb, 2),
            decimal.Round(savingsKwh, 2),
            decimal.Round(reductionDb, 2));
    }
}
