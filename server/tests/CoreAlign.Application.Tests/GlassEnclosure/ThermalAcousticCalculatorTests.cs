using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.GlassEnclosure;

public class ThermalAcousticCalculatorTests
{
    private readonly ThermalAcousticCalculator _sut = new();

    private static GlassProject Project() =>
        new("GE-2026-0001", Guid.NewGuid(), "Test", Guid.NewGuid(), "TRY");

    [Fact]
    public void Empty_panels_yield_zero_metrics()
    {
        var result = _sut.Calculate(Project(), Array.Empty<ThermalAcousticPanelInput>(), null);

        result.TotalAreaM2.Should().Be(0m);
        result.WeightedUValue.Should().Be(0m);
        result.WeightedSoundDb.Should().Be(0m);
    }

    [Fact]
    public void Weighted_u_value_uses_area_weighting()
    {
        var panels = new[]
        {
            new ThermalAcousticPanelInput(Guid.NewGuid(), AreaM2: 4m, UValue: 1.6m, SoundDb: 32m),
            new ThermalAcousticPanelInput(Guid.NewGuid(), AreaM2: 1m, UValue: 5.7m, SoundDb: 29m),
        };

        var result = _sut.Calculate(Project(), panels, null);

        var expectedU = (4m * 1.6m + 1m * 5.7m) / 5m;
        result.WeightedUValue.Should().BeApproximately(decimal.Round(expectedU, 3), 0.01m);
    }

    [Fact]
    public void Cold_climate_increases_estimated_savings()
    {
        var coldZone = new ClimateZone(
            "TR-DOGU", "Doğu", "East", avgWinterTemperatureC: -5m, avgHumidityPercent: 55m,
            CorrosionClass.C2, recommendsDoubleGlazing: true, recommendsCorrosionResistantCoating: false,
            recommendsSeismicSmallerPanel: true, ilPostalPrefixListJson: "[]");
        var warmZone = new ClimateZone(
            "TR-AKDENIZ", "Akdeniz", "Mediterranean", avgWinterTemperatureC: 11m, avgHumidityPercent: 70m,
            CorrosionClass.C5, recommendsDoubleGlazing: false, recommendsCorrosionResistantCoating: true,
            recommendsSeismicSmallerPanel: true, ilPostalPrefixListJson: "[]");
        var panels = new[] { new ThermalAcousticPanelInput(Guid.NewGuid(), 5m, 1.6m, 32m) };

        var cold = _sut.Calculate(Project(), panels, coldZone);
        var warm = _sut.Calculate(Project(), panels, warmZone);

        cold.EstimatedWinterEnergySavingsKwh.Should().BeGreaterThan(warm.EstimatedWinterEnergySavingsKwh);
    }
}
