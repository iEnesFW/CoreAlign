using CoreAlign.Application.GlassEnclosure.Cutting;
using CoreAlign.Infrastructure.GlassEnclosure.Cutting;

namespace CoreAlign.Application.Tests.GlassEnclosure;

public class MaxRectsGlass2DOptimizerTests
{
    private readonly MaxRectsGlass2DOptimizer _sut = new();

    private static GlassSheet Jumbo(decimal w = 3210m, decimal h = 2250m, decimal kerf = 4m, decimal margin = 0m) =>
        new(Guid.NewGuid(), w, h, kerf, margin);

    private static NestingOptions DefaultOpts(string heuristic = "BestShortSideFit", bool guillotine = false) =>
        new(Algorithm: "MaxRects", Heuristic: heuristic, MinimizeSheets: true, AcceptableUtilization: 0.85m, GuillotineOnly: guillotine);

    [Fact]
    public async Task SingleSheet_FourSmallPanels_AllPlaced_HighUtilization()
    {
        var panels = new[]
        {
            new GlassPanelRequest(Guid.NewGuid(), "A", 1000m, 1000m, 4),
        };
        var sheets = new[] { Jumbo(kerf: 0m) };

        var result = await _sut.OptimizeAsync(panels, sheets, DefaultOpts(), CancellationToken.None);

        result.SheetsUsed.Should().Be(1);
        result.UnplacedPanels.Should().BeEmpty();
        result.Sheets[0].Panels.Should().HaveCount(4);
        result.TotalUtilizationPercent.Should().BeGreaterThan(50m);
    }

    [Fact]
    public async Task PanelLargerThanSheet_Unplaced()
    {
        var panels = new[]
        {
            new GlassPanelRequest(Guid.NewGuid(), "X", 5000m, 4000m, 1),
        };
        var sheets = new[] { Jumbo() };

        var result = await _sut.OptimizeAsync(panels, sheets, DefaultOpts(), CancellationToken.None);

        result.SheetsUsed.Should().Be(0);
        result.UnplacedPanels.Should().HaveCount(1);
        result.UnplacedPanels[0].Reason.Should().Be("ExceedsSheet");
    }

    [Fact]
    public async Task PanelExactlyFitsSheet_NearFullUtilization()
    {
        var panels = new[]
        {
            new GlassPanelRequest(Guid.NewGuid(), "Full", 3210m, 2250m, 1),
        };
        var sheets = new[] { Jumbo(kerf: 0m, margin: 0m) };

        var result = await _sut.OptimizeAsync(panels, sheets, DefaultOpts(), CancellationToken.None);

        result.SheetsUsed.Should().Be(1);
        result.UnplacedPanels.Should().BeEmpty();
        result.TotalUtilizationPercent.Should().BeGreaterThan(95m);
    }

    [Fact]
    public async Task Rotation_AllowsTallPanelOnSheet()
    {
        var panels = new[]
        {
            new GlassPanelRequest(Guid.NewGuid(), "Tall", 2200m, 1500m, 1, AllowRotation: true),
        };
        var sheets = new[] { Jumbo(w: 3210m, h: 2250m) };

        var result = await _sut.OptimizeAsync(panels, sheets, DefaultOpts(), CancellationToken.None);

        result.SheetsUsed.Should().Be(1);
        result.UnplacedPanels.Should().BeEmpty();
        result.Sheets[0].Panels.Should().HaveCount(1);
    }

    [Fact]
    public async Task RotationDisabled_PanelThatRequiresRotation_Unplaced()
    {
        var panels = new[]
        {
            new GlassPanelRequest(Guid.NewGuid(), "NoRot", 600m, 3000m, 1, AllowRotation: false),
        };
        var sheets = new[] { Jumbo(w: 3210m, h: 2250m) };

        var result = await _sut.OptimizeAsync(panels, sheets, DefaultOpts(), CancellationToken.None);

        result.UnplacedPanels.Should().NotBeEmpty();
        result.UnplacedPanels[0].Reason.Should().Be("ExceedsSheet");
    }

    [Fact]
    public async Task MultiplePanels_MinimizeSheets_UsesFewerThanCount()
    {
        var panels = new[]
        {
            new GlassPanelRequest(Guid.NewGuid(), "A", 1000m, 1100m, 50),
        };
        var sheets = new[] { Jumbo() };

        var result = await _sut.OptimizeAsync(panels, sheets, DefaultOpts(), CancellationToken.None);

        result.SheetsUsed.Should().BeLessThan(50);
        result.Sheets.SelectMany(s => s.Panels).Should().HaveCount(50);
        result.UnplacedPanels.Should().BeEmpty();
    }

    [Fact]
    public async Task GuillotineOnly_StillPlacesPanels()
    {
        var panels = new[]
        {
            new GlassPanelRequest(Guid.NewGuid(), "G1", 1500m, 1100m, 3),
        };
        var sheets = new[] { Jumbo() };

        var result = await _sut.OptimizeAsync(panels, sheets, DefaultOpts(guillotine: true), CancellationToken.None);

        result.SheetsUsed.Should().BeGreaterThan(0);
        result.Sheets.SelectMany(s => s.Panels).Should().HaveCount(3);
    }

    [Fact]
    public async Task BestAreaFitHeuristic_ProducesValidPlacement()
    {
        var panels = new[]
        {
            new GlassPanelRequest(Guid.NewGuid(), "A", 1605m, 1125m, 2),
        };
        var sheets = new[] { Jumbo() };

        var result = await _sut.OptimizeAsync(panels, sheets, DefaultOpts("BestAreaFit"), CancellationToken.None);

        result.SheetsUsed.Should().Be(1);
        result.Sheets[0].Panels.Should().HaveCount(2);
        result.UnplacedPanels.Should().BeEmpty();
    }
}
