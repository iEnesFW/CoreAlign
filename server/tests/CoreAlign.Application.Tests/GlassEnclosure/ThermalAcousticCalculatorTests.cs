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
    public void Composite_sound_reduction_is_governed_by_the_WEAKEST_pane()
    {
        // Equal areas of 30 dB and 40 dB glass. Averaging the transmission coefficient
        // τ = 10^(−Rw/10) gives τ̄ = (1e-3 + 1e-4)/2 = 5.5e-4 → Rw = 32.6 dB.
        // The old code averaged 10^(+Rw/10) and reported 37.4 dB — the best pane dominated,
        // which is the opposite of how a mixed assembly actually performs.
        var panels = new[]
        {
            new ThermalAcousticPanelInput(Guid.NewGuid(), AreaM2: 1m, UValue: 1.6m, SoundDb: 30m),
            new ThermalAcousticPanelInput(Guid.NewGuid(), AreaM2: 1m, UValue: 1.6m, SoundDb: 40m),
        };

        var result = _sut.Calculate(Project(), panels, null);

        result.WeightedSoundDb.Should().BeApproximately(32.6m, 0.1m);
        result.WeightedSoundDb.Should().BeLessThan(35m);
    }

    [Fact]
    public void Uniform_glass_reports_its_own_rating_unchanged()
    {
        var panels = new[]
        {
            new ThermalAcousticPanelInput(Guid.NewGuid(), AreaM2: 3m, UValue: 1.6m, SoundDb: 36m),
            new ThermalAcousticPanelInput(Guid.NewGuid(), AreaM2: 7m, UValue: 1.6m, SoundDb: 36m),
        };

        var result = _sut.Calculate(Project(), panels, null);

        result.WeightedSoundDb.Should().BeApproximately(36m, 0.01m);
    }

    [Fact]
    public void Worse_glass_reports_MORE_heat_loss_not_more_saving()
    {
        // The field used to be called "energy savings" while carrying Q = U·A·ΔT·t, so a higher-U
        // (worse) glass looked like a bigger benefit — the number argued against its own spec.
        var good = new[] { new ThermalAcousticPanelInput(Guid.NewGuid(), 5m, 1.1m, 32m) };
        var bad = new[] { new ThermalAcousticPanelInput(Guid.NewGuid(), 5m, 5.7m, 32m) };

        var goodResult = _sut.Calculate(Project(), good, null);
        var badResult = _sut.Calculate(Project(), bad, null);

        badResult.EstimatedWinterHeatLossKwh.Should().BeGreaterThan(goodResult.EstimatedWinterHeatLossKwh);
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

        cold.EstimatedWinterHeatLossKwh.Should().BeGreaterThan(warm.EstimatedWinterHeatLossKwh);
    }
}
