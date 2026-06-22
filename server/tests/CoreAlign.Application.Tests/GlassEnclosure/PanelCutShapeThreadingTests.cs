using CoreAlign.Application.GlassEnclosure.Cutting;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Infrastructure.GlassEnclosure.Cutting;

namespace CoreAlign.Application.Tests.GlassEnclosure;

public class PanelCutShapeThreadingTests
{
    [Fact]
    public async Task Nesting_optimizer_carries_shape_onto_the_placed_panel()
    {
        var shape = new PanelCutShape("raked", 2200m, null, null, null, null, null);
        var request = new GlassPanelRequest(Guid.NewGuid(), "P", 1000m, 2200m, 1, true, shape, 2000m);
        var sheet = new GlassSheet(Guid.NewGuid(), 6000m, 3210m);

        var result = await new MaxRectsGlass2DOptimizer()
            .OptimizeAsync(new[] { request }, new[] { sheet }, new NestingOptions(), CancellationToken.None);

        var placed = result.Sheets.Single().Panels.Single();
        placed.Shape.Should().Be(shape);
        placed.NominalHeightMm.Should().Be(2000m);
    }

    [Fact]
    public void Guillotine_optimizer_carries_shape_onto_the_placement()
    {
        var shape = new PanelCutShape("arched", null, 120, null, null, null, null);
        var request = new CuttingRequest2D("P", 1000, 2120, 1, shape, 2000);

        var result = new MaximalRectanglesOptimizer2D()
            .Plan(new[] { request }, 6000, 3210, 4, guillotineOnly: false);

        var placement = result.Sheets.Single().Placements.Single();
        placement.Shape.Should().Be(shape);
        placement.NominalHeightMm.Should().Be(2000);
    }
}
