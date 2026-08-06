using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.Cutting;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Validators;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.GlassEnclosure;

/// <summary>
/// The designer already refuses these outlines client-side, but the API is callable by anything.
/// A self-intersecting outline is a MONEY defect, not a rendering one: its shoelace lobes cancel,
/// so the silhouette area the BOM prices and the cut list orders is silently under-reported.
/// </summary>
public class PanelShapeGeometryTests
{
    private const string Rect = """[{"x":-400,"y":100},{"x":400,"y":100},{"x":400,"y":1900},{"x":-400,"y":1900}]""";
    private const string Bowtie = """[{"x":-400,"y":100},{"x":400,"y":1900},{"x":400,"y":100},{"x":-400,"y":1900}]""";

    [Fact]
    public void Accepts_a_plain_rectangle_outline()
    {
        var check = PanelShapeGeometry.CheckPolygonJson(Rect, 1000m, 2000m);
        check.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_a_bowtie_whose_lobes_cancel_the_billed_area()
    {
        var check = PanelShapeGeometry.CheckPolygonJson(Bowtie, 1000m, 2000m);
        check.IsValid.Should().BeFalse();
        check.Rejection.Should().Be(PanelShapeGeometry.ShapeRejection.SelfIntersecting);
    }

    [Fact]
    public void Rejects_a_contour_that_passes_through_its_own_vertex()
    {
        const string pinched =
            """[{"x":-400,"y":0},{"x":400,"y":0},{"x":0,"y":1000},{"x":400,"y":2000},{"x":-400,"y":2000},{"x":0,"y":1000}]""";
        var check = PanelShapeGeometry.CheckPolygonJson(pinched, 1000m, 2000m);
        check.Rejection.Should().Be(PanelShapeGeometry.ShapeRejection.SelfIntersecting);
    }

    [Fact]
    public void Rejects_glass_claimed_outside_the_pane_box()
    {
        const string overflowing = """[{"x":-900,"y":100},{"x":900,"y":100},{"x":0,"y":1900}]""";
        var check = PanelShapeGeometry.CheckPolygonJson(overflowing, 1000m, 2000m);
        check.Rejection.Should().Be(PanelShapeGeometry.ShapeRejection.OutOfBounds);
    }

    [Fact]
    public void Skips_the_vertical_upper_bound_when_the_pane_inherits_the_run_height()
    {
        const string tall = """[{"x":-400,"y":100},{"x":400,"y":100},{"x":0,"y":2600}]""";
        PanelShapeGeometry.CheckPolygonJson(tall, 1000m, null).IsValid.Should().BeTrue();
        PanelShapeGeometry.CheckPolygonJson(tall, 1000m, 2000m).Rejection
            .Should().Be(PanelShapeGeometry.ShapeRejection.OutOfBounds);
    }

    [Fact]
    public void Rejects_a_sliver_the_cutter_cannot_make()
    {
        const string sliver = """[{"x":-400,"y":100},{"x":400,"y":100},{"x":400,"y":105}]""";
        var check = PanelShapeGeometry.CheckPolygonJson(sliver, 1000m, 2000m);
        check.Rejection.Should().Be(PanelShapeGeometry.ShapeRejection.Degenerate);
    }

    [Fact]
    public void Rejects_unreadable_payloads_without_throwing()
    {
        PanelShapeGeometry.CheckPolygonJson("not json", 1000m, 2000m).Rejection
            .Should().Be(PanelShapeGeometry.ShapeRejection.Unparsable);
        PanelShapeGeometry.CheckPolygonJson("{}", 1000m, 2000m).Rejection
            .Should().Be(PanelShapeGeometry.ShapeRejection.Unparsable);
        PanelShapeGeometry.CheckPolygonJson("""[{"x":"a","y":1}]""", 1000m, 2000m).Rejection
            .Should().Be(PanelShapeGeometry.ShapeRejection.Unparsable);
    }

    [Fact]
    public void Collapsed_duplicate_vertices_leave_too_few_points()
    {
        const string collapsed = """[{"x":0,"y":0},{"x":0,"y":0.4},{"x":0.3,"y":0.2}]""";
        PanelShapeGeometry.CheckPolygonJson(collapsed, 1000m, 2000m).Rejection
            .Should().Be(PanelShapeGeometry.ShapeRejection.TooFewPoints);
    }

    [Fact]
    public void Update_panel_validator_rejects_a_bowtie_and_accepts_a_rectangle()
    {
        var validator = new UpdatePanelCommandValidator();
        var bad = new UpdatePanelCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            PanelDto(shapeKind: "polygon", shapePointsJson: Bowtie));
        var good = new UpdatePanelCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            PanelDto(shapeKind: "polygon", shapePointsJson: Rect));

        validator.Validate(bad).IsValid.Should().BeFalse();
        validator.Validate(good).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validators_ignore_shapes_that_are_not_polygons()
    {
        var validator = new AddPanelCommandValidator();
        var ellipse = new AddPanelCommand(
            Guid.NewGuid(), Guid.NewGuid(),
            AddDto(shapeKind: "ellipse", shapePointsJson: null));
        var rectPane = new AddPanelCommand(
            Guid.NewGuid(), Guid.NewGuid(),
            AddDto(shapeKind: null, shapePointsJson: Bowtie));

        validator.Validate(ellipse).IsValid.Should().BeTrue();
        // A stray points payload under a non-polygon kind is inert data — the cut geometry never
        // reads it — so it passes; only a POLYGON claim is held to the contract.
        validator.Validate(rectPane).IsValid.Should().BeTrue();
    }

    private static UpdatePanelDto PanelDto(string? shapeKind, string? shapePointsJson) =>
        new(
            WidthMm: 1000,
            OpeningType: GlassOpeningType.Fixed,
            GlassTypeId: Guid.NewGuid(),
            HasHandle: false,
            HasLock: false,
            HasBrushSeal: false,
            Notes: null,
            HeightMm: 2000,
            ShapeKind: shapeKind,
            ShapePointsJson: shapePointsJson);

    private static AddPanelDto AddDto(string? shapeKind, string? shapePointsJson) =>
        new(
            WidthMm: 1000,
            OpeningType: GlassOpeningType.Fixed,
            GlassTypeId: Guid.NewGuid(),
            HasHandle: false,
            HasLock: false,
            HasBrushSeal: false,
            Notes: null,
            HeightMm: 2000,
            ShapeKind: shapeKind,
            ShapePointsJson: shapePointsJson);
}
