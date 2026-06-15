using CoreAlign.Application.GlassEnclosure.Services;

namespace CoreAlign.Application.Tests.GlassEnclosure;

public class CuttingOptimizer1DTests
{
    private readonly FirstFitDecreasingOptimizer1D _sut = new();

    [Fact]
    public void Empty_request_returns_zero_bars()
    {
        var result = _sut.Plan(Array.Empty<CuttingRequest1D>(), stockBarLengthMm: 6000, kerfMm: 5);

        result.TotalBars.Should().Be(0);
        result.TotalCuts.Should().Be(0);
        result.TotalUsedMm.Should().Be(0);
    }

    [Fact]
    public void Single_cut_under_stock_fits_in_one_bar()
    {
        var request = new[] { new CuttingRequest1D("top", 2400, 1) };

        var result = _sut.Plan(request, stockBarLengthMm: 6000, kerfMm: 5);

        result.TotalBars.Should().Be(1);
        result.TotalCuts.Should().Be(1);
        result.Patterns[0].Cuts.Should().HaveCount(1);
        result.Patterns[0].WasteMm.Should().Be(3600);
    }

    [Fact]
    public void Ffd_packs_multiple_cuts_efficiently()
    {
        var requests = new[]
        {
            new CuttingRequest1D("a", 2200, 1),
            new CuttingRequest1D("b", 2100, 1),
            new CuttingRequest1D("c", 1000, 1),
            new CuttingRequest1D("d", 600, 1),
        };

        var result = _sut.Plan(requests, stockBarLengthMm: 6000, kerfMm: 5);

        result.TotalBars.Should().Be(1);
        result.TotalCuts.Should().Be(4);
        result.Patterns[0].Cuts.Should().HaveCount(4);
        result.Patterns[0].WasteMm.Should().BeLessThan(150);
    }

    [Fact]
    public void Ffd_opens_new_bar_when_required()
    {
        var requests = new[]
        {
            new CuttingRequest1D("a", 4000, 1),
            new CuttingRequest1D("b", 4000, 1),
        };

        var result = _sut.Plan(requests, stockBarLengthMm: 6000, kerfMm: 5);

        result.TotalBars.Should().Be(2);
        result.TotalCuts.Should().Be(2);
        result.Patterns[0].Cuts[0].LengthMm.Should().Be(4000);
        result.Patterns[1].Cuts[0].LengthMm.Should().Be(4000);
    }

    [Fact]
    public void Throws_when_cut_exceeds_stock_bar()
    {
        var requests = new[] { new CuttingRequest1D("oversize", 7000, 1) };

        Action act = () => _sut.Plan(requests, stockBarLengthMm: 6000, kerfMm: 5);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Utilization_is_computed_from_used_and_capacity()
    {
        var requests = new[] { new CuttingRequest1D("a", 6000, 1) };

        var result = _sut.Plan(requests, stockBarLengthMm: 6000, kerfMm: 5);

        result.TotalBars.Should().Be(1);
        result.UtilizationPercent.Should().BeApproximately(100m, 0.5m);
    }
}
