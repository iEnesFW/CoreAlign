using CoreAlign.Application.GlassEnclosure.Cutting;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Exceptions;

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
    public void Oversize_rectangle_fails_as_a_user_error_naming_the_cut_and_the_sheet()
    {
        var requests = new[] { new CuttingRequest2D("CLR6 4000×3000", 4000, 3000, 1) };

        Action act = () => _sut.Plan(requests, 3210, 2250, 4, guillotineOnly: false);

        var thrown = act.Should().Throw<GlassCutExceedsJumboSheetException>().Which;
        thrown.Should().BeAssignableTo<ConflictException>();
        thrown.Message.Should().Contain("CLR6 4000×3000").And.Contain("4000x3000").And.Contain("3210x2250");
    }

    [Fact]
    public void Kerf_is_reserved_once_per_cut_plane_so_the_row_still_fits_one_sheet()
    {
        var requests = new[] { new CuttingRequest2D("k", 491, 1000, 4) };

        var result = _sut.Plan(requests, 2000, 1000, 10, guillotineOnly: true);

        result.TotalSheets.Should().Be(1);
        result.Sheets[0].Placements.Should().HaveCount(4);
        result.Sheets[0].Placements.Select(p => p.X).OrderBy(x => x).Should().Equal(0, 501, 1002, 1503);
    }

    [Fact]
    public void Kerf_is_reserved_once_per_cut_plane_in_the_free_form_path_too()
    {
        var requests = new[] { new CuttingRequest2D("k", 491, 1000, 4) };

        var result = _sut.Plan(requests, 2000, 1000, 10, guillotineOnly: false);

        result.TotalSheets.Should().Be(1);
        result.Sheets[0].Placements.Should().HaveCount(4);
    }

    [Fact]
    public void Cuts_of_different_glass_groups_never_share_a_sheet()
    {
        var requests = new[]
        {
            new CuttingRequest2D("clear-6", 1000, 2000, 1) { GroupKey = "CLR · 6 mm" },
            new CuttingRequest2D("clear-8", 1000, 2000, 1) { GroupKey = "CLR · 8 mm" },
        };

        var result = _sut.Plan(requests, 3210, 2250, 4, guillotineOnly: false);

        result.TotalSheets.Should().Be(2);
        result.Sheets.Should().OnlyContain(s => s.Placements.Count == 1);
        result.Sheets.Select(s => s.GroupKey).Should().BeEquivalentTo(new[] { "CLR · 6 mm", "CLR · 8 mm" });
        result.Sheets.Select(s => s.SheetIndex).Should().Equal(1, 2);
    }

    [Fact]
    public void Group_totals_add_up_to_the_report_totals()
    {
        var requests = new[]
        {
            new CuttingRequest2D("clear-6", 1000, 2000, 3) { GroupKey = "CLR · 6 mm" },
            new CuttingRequest2D("clear-8", 900, 1800, 2) { GroupKey = "CLR · 8 mm" },
        };

        var result = _sut.Plan(requests, 3210, 2250, 4, guillotineOnly: true);

        result.Groups.Should().HaveCount(2);
        result.Groups.Sum(g => g.TotalSheets).Should().Be(result.TotalSheets);
        result.Groups.Sum(g => g.TotalUsedMm2).Should().Be(result.TotalUsedMm2);
        result.Groups.Sum(g => g.TotalWasteMm2).Should().Be(result.TotalWasteMm2);
        (result.TotalUsedMm2 + result.TotalWasteMm2)
            .Should().Be((long)result.TotalSheets * result.SheetWidthMm * result.SheetHeightMm);
    }

    [Fact]
    public void Cuts_without_a_group_key_keep_sharing_one_sheet_pool()
    {
        var requests = new[]
        {
            new CuttingRequest2D("a", 1000, 2000, 1),
            new CuttingRequest2D("b", 1000, 2000, 1),
        };

        var result = _sut.Plan(requests, 3210, 2250, 4, guillotineOnly: false);

        result.TotalSheets.Should().Be(1);
        result.Groups.Should().ContainSingle();
        result.Groups[0].GroupKey.Should().BeNull();
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

    /// <summary>
    /// The two cutting screens quoted different utilisation for the very same panels: this planner
    /// counted the whole rectangular BLANK as consumed while the MaxRects nester counted the NET
    /// silhouette. A triangle cut from a 1×2 m blank leaves half of it as real offcut.
    /// </summary>
    [Fact]
    public void A_shaped_panel_consumes_its_silhouette_not_its_blank()
    {
        // Raked from 2000 mm down to 1000 mm → a trapezoid averaging 1500 mm tall, i.e. 75 % of
        // the blank. The blank-based accounting reported the full 2.0 m².
        var shape = PanelCutShape.From("raked", 1000, null, null, null, null, null);
        var shaped = new[] { new CuttingRequest2D("p1", 1000, 2000, 1, shape, NominalHeightMm: 2000) };
        var plain = new[] { new CuttingRequest2D("p1", 1000, 2000, 1) };

        var shapedResult = _sut.Plan(shaped, 3210, 2250, 0, guillotineOnly: false);
        var plainResult = _sut.Plan(plain, 3210, 2250, 0, guillotineOnly: false);

        shapedResult.TotalUsedMm2.Should().BeLessThan(plainResult.TotalUsedMm2);
        shapedResult.TotalUsedMm2.Should().BeInRange(1_499_000, 1_501_000);
        // Capacity is unchanged, so the offcut inside the blank shows up as waste.
        (shapedResult.TotalUsedMm2 + shapedResult.TotalWasteMm2)
            .Should().Be(plainResult.TotalUsedMm2 + plainResult.TotalWasteMm2);
    }

    [Fact]
    public void A_plain_rectangle_still_consumes_its_full_area()
    {
        var requests = new[] { new CuttingRequest2D("p1", 1000, 2000, 1) };

        var result = _sut.Plan(requests, 3210, 2250, 0, guillotineOnly: false);

        result.TotalUsedMm2.Should().Be(2_000_000);
    }
}
