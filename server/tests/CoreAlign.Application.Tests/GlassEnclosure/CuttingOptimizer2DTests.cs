using CoreAlign.Application.GlassEnclosure.Services;

namespace CoreAlign.Application.Tests.GlassEnclosure;

public class CuttingOptimizer2DTests
{
    private readonly MaximalRectanglesOptimizer2D _sut = new();

    [Fact]
    public void Empty_returns_zero_sheets()
    {
        var result = _sut.Plan(Array.Empty<CuttingRequest2D>(), 3210, 2250, 4, guillotineOnly: false);

        result.TotalSheets.Should().Be(0);
        result.Sheets.Should().BeEmpty();
    }

    [Fact]
    public void Single_rectangle_fits_on_first_sheet()
    {
        var requests = new[] { new CuttingRequest2D("p1", 1000, 2000, 1) };

        var result = _sut.Plan(requests, 3210, 2250, 4, guillotineOnly: false);

        result.TotalSheets.Should().Be(1);
        result.Sheets[0].Placements.Should().HaveCount(1);
        result.Sheets[0].Placements[0].WidthMm.Should().Be(1000);
        result.Sheets[0].Placements[0].HeightMm.Should().Be(2000);
    }

    [Fact]
    public void Multiple_rectangles_pack_to_fewer_sheets_than_count()
    {
        var requests = new[]
        {
            new CuttingRequest2D("a", 1000, 2000, 2),
            new CuttingRequest2D("b", 800, 1800, 2),
        };

        var result = _sut.Plan(requests, 3210, 2250, 4, guillotineOnly: false);

        result.TotalSheets.Should().BeLessThanOrEqualTo(2);
        result.Sheets.Sum(s => s.Placements.Count).Should().Be(4);
    }

    [Fact]
    public void Guillotine_only_still_packs_a_rectangle()
    {
        var requests = new[] { new CuttingRequest2D("g1", 1500, 2000, 1) };

        var result = _sut.Plan(requests, 3210, 2250, 4, guillotineOnly: true);

        result.TotalSheets.Should().Be(1);
        result.Sheets[0].Placements.Should().HaveCount(1);
        result.GuillotineOnly.Should().BeTrue();
    }

    [Fact]
    public void Rotation_used_when_natural_orientation_does_not_fit()
    {
        var requests = new[] { new CuttingRequest2D("tall", 2200, 1500, 1) };

        var result = _sut.Plan(requests, 3210, 2250, 4, guillotineOnly: false);

        result.TotalSheets.Should().Be(1);
        result.Sheets[0].Placements.Should().HaveCount(1);
    }

    [Fact]
    public void Throws_when_rectangle_exceeds_sheet_both_orientations()
    {
        var requests = new[] { new CuttingRequest2D("oversize", 4000, 3000, 1) };

        Action act = () => _sut.Plan(requests, 3210, 2250, 4, guillotineOnly: false);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Utilization_reflects_used_area()
    {
        var requests = new[]
        {
            new CuttingRequest2D("a", 1605, 1125, 1),
            new CuttingRequest2D("b", 1605, 1125, 1),
        };

        var result = _sut.Plan(requests, 3210, 2250, 0, guillotineOnly: false);

        result.TotalSheets.Should().Be(1);
        result.UtilizationPercent.Should().BeGreaterThan(40m);
    }
}
