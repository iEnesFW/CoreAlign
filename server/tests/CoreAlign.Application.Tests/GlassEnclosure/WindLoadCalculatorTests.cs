using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Entities.GlassEnclosure;

namespace CoreAlign.Application.Tests.GlassEnclosure;

public class WindLoadCalculatorTests
{
    private readonly WindLoadCalculator _sut = new();

    private static WindZone Zone(decimal basePa, decimal multiplier) =>
        new("TR-X", "tr", "en", basePa, multiplier, isCoastal: false);

    [Fact]
    public void Applies_height_factor_increasing_with_building_height()
    {
        var zone = Zone(1000m, 1m);
        var panels = new[] { new WindLoadPanelInput(Guid.NewGuid(), Guid.NewGuid(), 2m, 8) };

        var r0 = _sut.Calculate(zone, buildingHeightM: 0m, panels);
        var r50 = _sut.Calculate(zone, buildingHeightM: 50m, panels);
        var r100 = _sut.Calculate(zone, buildingHeightM: 100m, panels);

        r0.AppliedPressurePa.Should().Be(1000m);
        r50.AppliedPressurePa.Should().BeGreaterThan(1000m);
        r100.AppliedPressurePa.Should().BeGreaterThan(r50.AppliedPressurePa);
    }

    [Fact]
    public void Marks_panel_sufficient_when_thickness_meets_pressure()
    {
        var zone = Zone(1200m, 1m);
        var panel = new WindLoadPanelInput(Guid.NewGuid(), Guid.NewGuid(), 2m, CurrentGlassThicknessMm: 8);

        var result = _sut.Calculate(zone, 0m, new[] { panel });

        result.Panels[0].RequiredMinThicknessMm.Should().Be(6);
        result.Panels[0].IsSufficient.Should().BeTrue();
    }

    [Fact]
    public void Marks_panel_insufficient_for_high_wind()
    {
        var zone = Zone(2500m, 1m);
        var panel = new WindLoadPanelInput(Guid.NewGuid(), Guid.NewGuid(), 2m, CurrentGlassThicknessMm: 6);

        var result = _sut.Calculate(zone, 0m, new[] { panel });

        result.Panels[0].RequiredMinThicknessMm.Should().BeGreaterThan(6);
        result.Panels[0].IsSufficient.Should().BeFalse();
    }
}
