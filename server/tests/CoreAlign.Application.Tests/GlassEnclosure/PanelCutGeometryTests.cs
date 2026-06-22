using CoreAlign.Application.GlassEnclosure.Cutting;

namespace CoreAlign.Application.Tests.GlassEnclosure;

public class PanelCutGeometryTests
{
    [Fact]
    public void From_returns_null_for_a_plain_rectangle()
    {
        PanelCutShape.From("flat", null, null, null, null, null, null).Should().BeNull();
        PanelCutShape.From(null, null, null, null, null, null, null).Should().BeNull();
        PanelCutShape.From("arched", null, 0, null, null, null, null).Should().BeNull();
    }

    [Fact]
    public void IsShaped_detects_raked_arched_and_radii()
    {
        PanelCutGeometry.IsShaped(null).Should().BeFalse();
        PanelCutGeometry.IsShaped(new PanelCutShape("raked", 2200, null, null, null, null, null)).Should().BeTrue();
        PanelCutGeometry.IsShaped(new PanelCutShape("arched", null, 120, null, null, null, null)).Should().BeTrue();
        PanelCutGeometry.IsShaped(new PanelCutShape("arched", null, 0, null, null, null, null)).Should().BeFalse();
        PanelCutGeometry.IsShaped(new PanelCutShape("flat", null, null, 80, null, null, null)).Should().BeTrue();
    }

    [Fact]
    public void BoundingHeight_for_a_flat_panel_is_the_nominal_height()
    {
        PanelCutGeometry.BoundingHeightMm(2000m, null).Should().Be(2000m);
    }

    [Fact]
    public void BoundingHeight_grows_to_the_taller_raked_edge()
    {
        var taller = new PanelCutShape("raked", 2200, null, null, null, null, null);
        PanelCutGeometry.BoundingHeightMm(2000m, taller).Should().Be(2200m);

        var shorter = new PanelCutShape("raked", 1800, null, null, null, null, null);
        PanelCutGeometry.BoundingHeightMm(2000m, shorter).Should().Be(2000m);
    }

    [Fact]
    public void BoundingHeight_adds_the_arch_crown()
    {
        var arched = new PanelCutShape("arched", null, 120, null, null, null, null);
        PanelCutGeometry.BoundingHeightMm(2000m, arched).Should().Be(2120m);
    }

    [Fact]
    public void BoundingHeight_ignores_corner_radii()
    {
        var rounded = new PanelCutShape("flat", null, null, 100, 100, 100, 100);
        PanelCutGeometry.BoundingHeightMm(2000m, rounded).Should().Be(2000m);
    }

    [Fact]
    public void NetArea_of_a_rectangle_is_width_times_height()
    {
        PanelCutGeometry.NetAreaMm2(1000m, 2000m, null).Should().Be(2_000_000m);
    }

    [Fact]
    public void NetArea_of_a_raked_panel_is_the_trapezoid_area()
    {
        var raked = new PanelCutShape("raked", 2200, null, null, null, null, null);
        PanelCutGeometry.NetAreaMm2(1000m, 2000m, raked).Should().Be(2_100_000m);
    }

    [Fact]
    public void NetArea_of_an_arched_panel_adds_the_crown_lune()
    {
        var arched = new PanelCutShape("arched", null, 120, null, null, null, null);
        // body 2,000,000 + crown 1000*120*(2/π) ≈ 76,394.37
        PanelCutGeometry.NetAreaMm2(1000m, 2000m, arched)
            .Should().BeApproximately(2_076_394.37m, 1m);
    }

    [Fact]
    public void NetArea_subtracts_rounded_corner_offcuts()
    {
        var rounded = new PanelCutShape("flat", null, null, 100, 100, 100, 100);
        // 2,000,000 − 4*(1−π/4)*100² ≈ 2,000,000 − 8,584.07
        PanelCutGeometry.NetAreaMm2(1000m, 2000m, rounded)
            .Should().BeApproximately(1_991_415.93m, 1m);
    }

    [Fact]
    public void Signature_separates_distinct_shapes_and_collapses_equal_ones()
    {
        var flat = PanelCutGeometry.Signature(null);
        var raked = PanelCutGeometry.Signature(new PanelCutShape("raked", 2200, null, null, null, null, null));
        var rakedSame = PanelCutGeometry.Signature(new PanelCutShape("raked", 2200, null, null, null, null, null));
        var arched = PanelCutGeometry.Signature(new PanelCutShape("arched", null, 120, null, null, null, null));

        flat.Should().Be("rect");
        raked.Should().Be(rakedSame);
        raked.Should().NotBe(flat);
        raked.Should().NotBe(arched);
    }

    [Fact]
    public void Ellipse_is_shaped_fits_its_box_and_uses_the_ellipse_area()
    {
        var ellipse = PanelCutShape.From(null, null, null, null, null, null, null, "ellipse");
        ellipse.Should().NotBeNull();
        PanelCutGeometry.IsShaped(ellipse).Should().BeTrue();
        // Round/oval fits inside the width × height blank, so the blank height is the nominal height.
        PanelCutGeometry.BoundingHeightMm(2000m, ellipse).Should().Be(2000m);
        // area = π·w·h/4
        PanelCutGeometry.NetAreaMm2(1000m, 2000m, ellipse)
            .Should().BeApproximately(1_570_796.33m, 1m);
        PanelCutGeometry.Signature(ellipse).Should().NotBe("rect");
    }

    [Fact]
    public void Polygon_is_shaped_and_uses_the_shoelace_area()
    {
        const string square = "[{\"x\":-500,\"y\":0},{\"x\":500,\"y\":0},{\"x\":500,\"y\":2000},{\"x\":-500,\"y\":2000}]";
        var poly = PanelCutShape.From(null, null, null, null, null, null, null, "polygon", square);
        poly.Should().NotBeNull();
        PanelCutGeometry.IsShaped(poly).Should().BeTrue();
        PanelCutGeometry.BoundingHeightMm(2000m, poly).Should().Be(2000m);
        PanelCutGeometry.NetAreaMm2(1000m, 2000m, poly).Should().Be(2_000_000m);
    }

    [Fact]
    public void Polygon_with_fewer_than_three_points_is_not_shaped()
    {
        var poly = PanelCutShape.From(null, null, null, null, null, null, null, "polygon", "[{\"x\":0,\"y\":0}]");
        poly.Should().BeNull();
    }

    [Fact]
    public void Polygon_bounding_height_grows_for_a_vertex_above_the_nominal_box()
    {
        const string tall = "[{\"x\":-500,\"y\":0},{\"x\":500,\"y\":0},{\"x\":0,\"y\":2400}]";
        var poly = PanelCutShape.From(null, null, null, null, null, null, null, "polygon", tall);
        PanelCutGeometry.BoundingHeightMm(2000m, poly).Should().Be(2400m);
    }

    [Fact]
    public void Polygon_signature_separates_distinct_outlines()
    {
        var a = PanelCutShape.From(null, null, null, null, null, null, null, "polygon", "[{\"x\":0,\"y\":0},{\"x\":100,\"y\":0},{\"x\":0,\"y\":100}]");
        var b = PanelCutShape.From(null, null, null, null, null, null, null, "polygon", "[{\"x\":0,\"y\":0},{\"x\":200,\"y\":0},{\"x\":0,\"y\":200}]");
        PanelCutGeometry.Signature(a).Should().NotBe(PanelCutGeometry.Signature(b));
    }
}
