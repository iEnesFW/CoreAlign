using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Entities.GlassEnclosure;

namespace CoreAlign.Application.Tests.GlassEnclosure;

/// <summary>
/// These assertions used to pin the naive model (<c>storedPressure × (1 + height/100)</c> against a
/// flat allowable-pascals table). That model was replaced by the TS EN 1991-1-4 chain plus a
/// geometry-aware EN 16612 pane check, so its exact numbers are gone on purpose — the INTENT the
/// tests were protecting (pressure rises with height, a good pane passes, a thin one fails) is
/// what is asserted here. The chain itself is covered in <see cref="EurocodeWindLoadTests"/>.
/// </summary>
public class WindLoadCalculatorTests
{
    private readonly WindLoadCalculator _sut = new();

    private static WindZone Zone(decimal basePa, decimal multiplier) =>
        new("TR-X", "tr", "en", basePa, multiplier, isCoastal: false);

    private static WindLoadPanelInput Panel(int thicknessMm, decimal widthMm = 1000m, decimal heightMm = 2000m) =>
        new(Guid.NewGuid(), Guid.NewGuid(), widthMm * heightMm / 1_000_000m, thicknessMm, widthMm, heightMm);

    [Fact]
    public void Applies_height_factor_increasing_with_building_height()
    {
        var zone = Zone(1000m, 1m);
        var panels = new[] { Panel(8) };

        var r0 = _sut.Calculate(zone, buildingHeightM: 0m, panels);
        var r50 = _sut.Calculate(zone, buildingHeightM: 50m, panels);
        var r100 = _sut.Calculate(zone, buildingHeightM: 100m, panels);

        r0.AppliedPressurePa.Should().BeGreaterThan(0m);
        r50.AppliedPressurePa.Should().BeGreaterThan(r0.AppliedPressurePa);
        r100.AppliedPressurePa.Should().BeGreaterThan(r50.AppliedPressurePa);
    }

    [Fact]
    public void Marks_panel_sufficient_when_thickness_meets_pressure()
    {
        var zone = Zone(1200m, 1m);

        var result = _sut.Calculate(zone, 0m, new[] { Panel(thicknessMm: 8) });

        var panel = result.Panels.Should().ContainSingle().Which;
        panel.RequiredMinThicknessMm.Should().BeLessThanOrEqualTo(8);
        panel.IsSufficient.Should().BeTrue();
    }

    [Fact]
    public void Marks_panel_insufficient_for_high_wind()
    {
        // A big pane on a tall, exposed facade under a strong zone: the 6 mm it carries cannot hold.
        var zone = Zone(2500m, 1m);

        var result = _sut.Calculate(
            zone,
            60m,
            new[] { Panel(thicknessMm: 6, widthMm: 2400m, heightMm: 3000m) });

        var panel = result.Panels.Should().ContainSingle().Which;
        panel.RequiredMinThicknessMm.Should().BeGreaterThan(6);
        panel.IsSufficient.Should().BeFalse();
    }
}
