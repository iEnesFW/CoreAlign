using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Exceptions;

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
    public void An_oversize_cut_is_spliced_rather_than_rejected()
    {
        // Superseded behaviour: this used to throw a raw InvalidOperationException, which reached
        // the user as a generic 500 and lost the whole cutting report — including the 2D glass
        // plan that would have succeeded.
        var requests = new[] { new CuttingRequest1D("oversize", 7000, 1) };

        var result = _sut.Plan(requests, stockBarLengthMm: 6000, kerfMm: 5);

        result.TotalCuts.Should().Be(2);
        result.TotalUsedMm.Should().Be(7000);
        result.Patterns.SelectMany(p => p.Cuts).Should().OnlyContain(c => c.LengthMm <= 6000);
    }

    [Fact]
    public void Refuses_with_a_domain_error_when_a_splice_cannot_meet_the_minimum_piece()
    {
        // A bar so short that an even split would produce sub-minimum stubs is genuinely
        // infeasible — but it must surface as a domain conflict, not a 500.
        var requests = new[] { new CuttingRequest1D("hairline", 900, 1, StockBarLengthMm: 200) };

        Action act = () => _sut.Plan(requests, stockBarLengthMm: 6000, kerfMm: 5);

        act.Should().Throw<GlassCutCannotBeSplicedException>().Which.Message.Should().Contain("hairline");
    }

    [Fact]
    public void Utilization_is_computed_from_used_and_capacity()
    {
        var requests = new[] { new CuttingRequest1D("a", 6000, 1) };

        var result = _sut.Plan(requests, stockBarLengthMm: 6000, kerfMm: 5);

        result.TotalBars.Should().Be(1);
        result.UtilizationPercent.Should().BeApproximately(100m, 0.5m);
    }
    [Fact]
    public void Curved_rail_longer_than_a_bar_is_spliced_instead_of_throwing()
    {
        // The exact case that crashed the report: a 3000 mm chord curved to a 6098 mm developed rail.
        var request = new[] { new CuttingRequest1D("AG-SM-TOP", 6098, 1) };

        var result = _sut.Plan(request, stockBarLengthMm: 6000, kerfMm: 5);

        result.TotalCuts.Should().Be(2);
        result.TotalUsedMm.Should().Be(6098);
        var cuts = result.Patterns.SelectMany(p => p.Cuts).OrderBy(c => c.PieceIndex).ToList();
        cuts.Should().OnlyContain(c => c.PieceCount == 2);
        cuts.Select(c => c.PieceIndex).Should().Equal(1, 2);
        // Split EVENLY, not 6000 + 98 — an even split maximises the shortest piece.
        cuts.Select(c => c.LengthMm).Should().Equal(3049, 3049);
    }

    [Fact]
    public void A_spliced_rail_reports_every_piece_within_the_bar_length()
    {
        var result = _sut.Plan(new[] { new CuttingRequest1D("LONG", 19000, 1) }, 6000, 5);

        result.Patterns.SelectMany(p => p.Cuts).Should().OnlyContain(c => c.LengthMm <= 6000);
        result.Patterns.SelectMany(p => p.Cuts).Sum(c => c.LengthMm).Should().Be(19000);
    }

    [Fact]
    public void A_cut_that_fits_is_never_marked_as_spliced()
    {
        var result = _sut.Plan(new[] { new CuttingRequest1D("top", 2400, 1) }, 6000, 5);

        var cut = result.Patterns.Single().Cuts.Single();
        cut.PieceCount.Should().Be(1);
        cut.PieceIndex.Should().Be(1);
        cut.LengthMm.Should().Be(2400);
    }

    [Fact]
    public void A_profile_stocked_in_longer_bars_uses_its_own_length()
    {
        // 6500 mm would be a splice against the 6000 mm tenant default, but this profile ships in
        // 7 m bars, so it is a single piece.
        var request = new[] { new CuttingRequest1D("BIG", 6500, 1, StockBarLengthMm: 7000) };

        var result = _sut.Plan(request, stockBarLengthMm: 6000, kerfMm: 5);

        var pattern = result.Patterns.Single();
        pattern.StockBarLengthMm.Should().Be(7000);
        pattern.Cuts.Single().PieceCount.Should().Be(1);
    }

    [Fact]
    public void Profiles_with_different_bar_lengths_never_share_a_bar()
    {
        var requests = new[]
        {
            new CuttingRequest1D("A", 1000, 1, StockBarLengthMm: 6000),
            new CuttingRequest1D("B", 1000, 1, StockBarLengthMm: 7000),
        };

        var result = _sut.Plan(requests, stockBarLengthMm: 6000, kerfMm: 5);

        result.TotalBars.Should().Be(2);
        result.Patterns.Select(p => p.StockBarLengthMm).Should().BeEquivalentTo(new[] { 6000, 7000 });
    }

    [Fact]
    public void Waste_and_utilization_account_for_each_bar_own_length()
    {
        var requests = new[] { new CuttingRequest1D("B", 3500, 1, StockBarLengthMm: 7000) };

        var result = _sut.Plan(requests, stockBarLengthMm: 6000, kerfMm: 5);

        result.TotalWasteMm.Should().Be(3500);
        result.UtilizationPercent.Should().BeApproximately(50m, 0.01m);
    }

    /// <summary>
    /// The header counted kerf as waste while each bar reported only its tail, so the per-bar
    /// figures never summed to the total and the report looked like it had lost material it had
    /// already accounted for. Waste is now "everything that is not a finished cut", with the
    /// reusable tail carried separately.
    /// </summary>
    [Fact]
    public void Per_bar_waste_sums_to_the_header_total_once_kerf_is_included()
    {
        var requests = new[] { new CuttingRequest1D("A", 1000, 5) };

        var result = _sut.Plan(requests, stockBarLengthMm: 6000, kerfMm: 10);

        result.Patterns.Sum(p => p.WasteMm).Should().Be((int)result.TotalWasteMm);
    }

    [Fact]
    public void The_reusable_tail_is_reported_apart_from_the_kerf()
    {
        // Four 1000 mm cuts on a 6000 mm bar with a 10 mm kerf: 3 kerfs = 30 mm burnt,
        // 6000 − 4000 − 30 = 1970 mm of tail left over.
        var requests = new[] { new CuttingRequest1D("A", 1000, 4) };

        var result = _sut.Plan(requests, stockBarLengthMm: 6000, kerfMm: 10);

        var bar = result.Patterns.Should().ContainSingle().Which;
        bar.WasteMm.Should().Be(2000);
        bar.OffcutMm.Should().Be(1970);
        (bar.WasteMm - bar.OffcutMm).Should().Be(30);
    }
}
