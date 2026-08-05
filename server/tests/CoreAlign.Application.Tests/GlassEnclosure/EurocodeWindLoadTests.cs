using CoreAlign.Application.GlassEnclosure.Engineering;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.GlassEnclosure;

/// <summary>
/// The previous model was <c>storedPressure × (1 + height/100)</c> against a flat allowable-pascals
/// table. It had no terrain, no turbulence, no pressure coefficients and — the part that made the
/// per-panel report meaningless — no pane geometry: a 4 m² fixed light and a 0.4 m² vent came back
/// with the same verdict. Bending stress scales with the SQUARE of the short span.
/// </summary>
public class EurocodeWindLoadTests
{
    private readonly WindLoadCalculator _sut = new();

    private static WindZone Zone(decimal basicSpeedMs = 28m) =>
        new("TR-1", "Bölge 1", "Zone 1", 800m, 0.5m, false, basicSpeedMs);

    private static WindLoadPanelInput Panel(decimal widthMm, decimal heightMm, int thicknessMm = 8) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            widthMm * heightMm / 1_000_000m,
            thicknessMm,
            widthMm,
            heightMm);

    [Fact]
    public void Terrain_changes_the_pressure_for_the_same_wind_map_value()
    {
        var panels = new[] { Panel(1000m, 2000m) };

        var coastal = _sut.Calculate(Zone(), 10m, panels, WindTerrainCategory.Sea);
        var city = _sut.Calculate(Zone(), 10m, panels, WindTerrainCategory.Urban);

        // An exposed coastal site sees a materially higher peak velocity pressure than a city
        // centre at the same height — the old model reported one number for both.
        coastal.PeakVelocityPressurePa.Should().BeGreaterThan(city.PeakVelocityPressurePa);
        coastal.AppliedPressurePa.Should().BeGreaterThan(city.AppliedPressurePa);
    }

    [Fact]
    public void Pressure_grows_with_height()
    {
        var panels = new[] { Panel(1000m, 2000m) };

        var low = _sut.Calculate(Zone(), 5m, panels);
        var high = _sut.Calculate(Zone(), 60m, panels);

        high.PeakVelocityPressurePa.Should().BeGreaterThan(low.PeakVelocityPressurePa);
    }

    [Fact]
    public void A_BIG_pane_needs_more_glass_than_a_small_one_under_the_same_wind()
    {
        var big = Panel(2000m, 2000m);
        var small = Panel(600m, 700m);

        var result = _sut.Calculate(Zone(), 10m, new[] { big, small });

        var bigResult = result.Panels[0];
        var smallResult = result.Panels[1];

        // Same site pressure...
        bigResult.AppliedPressurePa.Should().Be(smallResult.AppliedPressurePa);
        // ...but the panes are not interchangeable. This is the assertion the old table could
        // never satisfy: it returned one required thickness for the whole project.
        bigResult.MaxBendingStressMPa.Should().BeGreaterThan(smallResult.MaxBendingStressMPa);
        bigResult.RequiredMinThicknessMm.Should().BeGreaterThan(smallResult.RequiredMinThicknessMm);
    }

    [Fact]
    public void Stress_scales_with_the_square_of_the_short_span()
    {
        // Two square panes, one twice the side of the other, same thickness. Plate theory says
        // sigma = beta*q*a^2/t^2, and beta is identical at the same aspect ratio, so doubling the
        // span must quadruple the stress.
        var result = _sut.Calculate(
            Zone(),
            10m,
            new[] { Panel(1000m, 1000m), Panel(2000m, 2000m) });

        var ratio = result.Panels[1].MaxBendingStressMPa / result.Panels[0].MaxBendingStressMPa;
        ratio.Should().BeApproximately(4m, 0.05m);
    }

    [Fact]
    public void Toughened_glass_is_allowed_more_stress_than_laminated_annealed()
    {
        var toughened = Panel(1500m, 2000m) with { Structure = GlassStructure.Tempered };
        var annealed = Panel(1500m, 2000m) with { Structure = GlassStructure.Laminated };

        var result = _sut.Calculate(Zone(), 10m, new[] { toughened, annealed });

        result.Panels[0].DesignStrengthMPa.Should().BeGreaterThan(result.Panels[1].DesignStrengthMPa);
    }

    [Fact]
    public void The_required_thickness_is_a_thickness_a_fabricator_can_buy()
    {
        var stock = new[] { 4, 5, 6, 8, 10, 12, 15, 19, 25 };

        var result = _sut.Calculate(Zone(), 30m, new[] { Panel(2400m, 3000m), Panel(500m, 500m) });

        foreach (var panel in result.Panels)
        {
            stock.Should().Contain(panel.RequiredMinThicknessMm);
        }
    }

    [Fact]
    public void Every_step_of_the_chain_is_reported_so_it_can_be_audited()
    {
        var result = _sut.Calculate(Zone(), 12m, new[] { Panel(1200m, 2200m) });

        result.Site.Should().NotBeNull();
        result.Site!.BasicWindSpeedMs.Should().Be(28m);
        result.Site.RoughnessFactor.Should().BeGreaterThan(0m);
        result.Site.TurbulenceIntensity.Should().BeGreaterThan(0m);
        result.Site.MeanWindSpeedMs.Should().BeGreaterThan(0m);
        result.PeakVelocityPressurePa.Should().BeGreaterThan(0m);
        result.StandardReference.Should().Contain("EN 1991-1-4");

        var panel = result.Panels.Should().ContainSingle().Which;
        panel.ShortSpanMm.Should().Be(1200m);
        panel.AspectRatio.Should().BeApproximately(2200m / 1200m, 0.01m);
        panel.GoverningLimit.Should().BeOneOf("Strength", "Deflection");
    }

    [Fact]
    public void A_zone_that_only_stores_a_pressure_still_reports_instead_of_refusing()
    {
        // Pre-Eurocode rows carry no wind speed. Inverting q = 0.5*rho*v^2 keeps them usable.
        var legacy = new WindZone("TR-OLD", "Eski", "Legacy", 800m, 0.5m, false);

        var result = _sut.Calculate(legacy, 10m, new[] { Panel(1000m, 2000m) });

        result.Site!.BasicWindSpeedMs.Should().BeApproximately(35.78m, 0.05m);
        result.AppliedPressurePa.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void An_over_thin_pane_fails_and_a_generous_one_passes()
    {
        var thin = Panel(2000m, 2500m, thicknessMm: 4);
        var thick = Panel(2000m, 2500m, thicknessMm: 19);

        var result = _sut.Calculate(Zone(), 25m, new[] { thin, thick });

        result.Panels[0].IsSufficient.Should().BeFalse();
        result.Panels[0].StressUtilisation.Should().BeGreaterThan(1m);
        result.Panels[1].IsSufficient.Should().BeTrue();
    }

    [Fact]
    public void A_laminate_is_weaker_than_a_monolith_of_the_same_total_thickness()
    {
        // Two 6 mm plies with no shear transfer behave like cube-root(2*6^3) = 7.56 mm, not 12 mm.
        var laminate = new GlassPaneGeometry(1000m, 2000m, 12m, new[] { 6m, 6m });
        var monolith = new GlassPaneGeometry(1000m, 2000m, 12m, Array.Empty<decimal>());

        var lam = GlassPlateResistance.EffectiveThicknessMm(laminate);
        var mono = GlassPlateResistance.EffectiveThicknessMm(monolith);

        lam.ForStress.Should().BeLessThan(mono.ForStress);
        lam.ForStress.Should().BeApproximately(7.56m, 0.02m);
    }

    [Fact]
    public void Interlayers_are_not_counted_as_glass()
    {
        var json = """
        [
          { "kind": "glass", "thicknessMm": 6 },
          { "kind": "PVB", "thicknessMm": 1.52 },
          { "kind": "glass", "thicknessMm": 6 }
        ]
        """;

        GlassLayerThickness.Parse(json).Should().Equal(6m, 6m);
    }

    [Fact]
    public void Unreadable_layer_data_falls_back_to_the_monolithic_check_instead_of_throwing()
    {
        GlassLayerThickness.Parse("not json").Should().BeEmpty();
        GlassLayerThickness.Parse(null).Should().BeEmpty();
        GlassLayerThickness.Parse("{}").Should().BeEmpty();
    }
}
