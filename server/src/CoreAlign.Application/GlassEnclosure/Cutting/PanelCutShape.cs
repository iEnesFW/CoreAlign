using System.Globalization;
using System.Text.Json;

namespace CoreAlign.Application.GlassEnclosure.Cutting;

public sealed record PanelCutShape(
    string? TopShape,
    decimal? TopRightHeightMm,
    decimal? ArchRiseMm,
    decimal? CornerRadiusTlMm,
    decimal? CornerRadiusTrMm,
    decimal? CornerRadiusBrMm,
    decimal? CornerRadiusBlMm,
    string? ShapeKind = null,
    string? PointsJson = null)
{
    public static PanelCutShape? From(
        string? topShape,
        int? topRightHeightMm,
        int? archRiseMm,
        int? cornerRadiusTlMm,
        int? cornerRadiusTrMm,
        int? cornerRadiusBrMm,
        int? cornerRadiusBlMm,
        string? shapeKind = null,
        string? shapePointsJson = null)
    {
        var shape = new PanelCutShape(
            string.IsNullOrWhiteSpace(topShape) ? null : topShape,
            topRightHeightMm,
            archRiseMm,
            cornerRadiusTlMm,
            cornerRadiusTrMm,
            cornerRadiusBrMm,
            cornerRadiusBlMm,
            string.IsNullOrWhiteSpace(shapeKind) ? null : shapeKind,
            string.IsNullOrWhiteSpace(shapePointsJson) ? null : shapePointsJson);
        return PanelCutGeometry.IsShaped(shape) ? shape : null;
    }
}

public static class PanelCutGeometry
{
    private const decimal Pi = 3.1415926535897932m;

    public static bool IsShaped(PanelCutShape? shape)
    {
        if (shape is null) return false;
        if (shape.ShapeKind == "ellipse") return true;
        if (shape.ShapeKind == "polygon") return ParsePolygonPoints(shape.PointsJson).Count >= 3;
        var top = shape.TopShape ?? "flat";
        if (top == "raked") return true;
        if (top == "arched" && (shape.ArchRiseMm ?? 0m) > 0m) return true;
        return (shape.CornerRadiusTlMm ?? 0m) > 0m
            || (shape.CornerRadiusTrMm ?? 0m) > 0m
            || (shape.CornerRadiusBrMm ?? 0m) > 0m
            || (shape.CornerRadiusBlMm ?? 0m) > 0m;
    }

    // Glass is cut from a rectangular blank. A raked right edge or an arch crown
    // pushes the silhouette above the nominal head line, so the blank must grow.
    public static decimal BoundingHeightMm(decimal nominalHeightMm, PanelCutShape? shape)
    {
        var hL = Math.Max(1m, nominalHeightMm);
        if (shape is null) return hL;

        if (shape.ShapeKind == "polygon")
        {
            var pts = ParsePolygonPoints(shape.PointsJson);
            if (pts.Count < 3) return hL;
            var maxY = pts[0].Y;
            foreach (var p in pts) maxY = Math.Max(maxY, p.Y);
            return Math.Max(hL, maxY);
        }

        var top = shape.TopShape ?? "flat";
        var hR = top == "flat" ? hL : Math.Max(1m, shape.TopRightHeightMm ?? hL);
        var baseTop = Math.Max(hL, hR);
        var arch = top == "arched" ? Math.Max(0m, shape.ArchRiseMm ?? 0m) : 0m;
        return baseTop + arch;
    }

    // True glass silhouette area for costing: trapezoid body + arch lune − rounded-corner offcuts.
    public static decimal NetAreaMm2(decimal widthMm, decimal nominalHeightMm, PanelCutShape? shape)
    {
        var w = Math.Max(1m, widthMm);
        var hL = Math.Max(1m, nominalHeightMm);
        if (shape is null) return w * hL;

        if (shape.ShapeKind == "ellipse")
        {
            // Ellipse inscribed in the width × height blank: area = π·(w/2)·(h/2) = π·w·h/4.
            return Pi * w * hL / 4m;
        }

        if (shape.ShapeKind == "polygon")
        {
            var pts = ParsePolygonPoints(shape.PointsJson);
            return pts.Count >= 3 ? ShoelaceAreaMm2(pts) : w * hL;
        }

        var top = shape.TopShape ?? "flat";
        var hR = top == "flat" ? hL : Math.Max(1m, shape.TopRightHeightMm ?? hL);
        var body = w * (hL + hR) / 2m;

        var arch = 0m;
        if (top == "arched")
        {
            // ∫₀¹ sin(πt) dt = 2/π, so the crown adds rise·width·(2/π) above the head line.
            var rise = Math.Max(0m, shape.ArchRiseMm ?? 0m);
            arch = w * rise * 2m / Pi;
        }

        return Math.Max(0m, body + arch - CornerOffcutMm2(shape));
    }

    public static string Signature(PanelCutShape? shape)
    {
        if (shape is null) return "rect";
        return string.Join('|',
            shape.ShapeKind ?? "rect",
            shape.TopShape ?? "flat",
            Fmt(shape.TopRightHeightMm),
            Fmt(shape.ArchRiseMm),
            Fmt(shape.CornerRadiusTlMm),
            Fmt(shape.CornerRadiusTrMm),
            Fmt(shape.CornerRadiusBrMm),
            Fmt(shape.CornerRadiusBlMm),
            shape.ShapeKind == "polygon" ? shape.PointsJson ?? "" : "");
    }

    private static List<(decimal X, decimal Y)> ParsePolygonPoints(string? json)
    {
        var result = new List<(decimal X, decimal Y)>();
        if (string.IsNullOrWhiteSpace(json)) return result;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return result;
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object) continue;
                if (el.TryGetProperty("x", out var x) && el.TryGetProperty("y", out var y)
                    && x.ValueKind == JsonValueKind.Number && y.ValueKind == JsonValueKind.Number)
                {
                    result.Add((x.GetDecimal(), y.GetDecimal()));
                }
            }
        }
        catch (JsonException)
        {
            return new List<(decimal X, decimal Y)>();
        }
        return result;
    }

    private static decimal ShoelaceAreaMm2(IReadOnlyList<(decimal X, decimal Y)> points)
    {
        decimal sum = 0m;
        for (var i = 0; i < points.Count; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            sum += a.X * b.Y - b.X * a.Y;
        }
        return Math.Abs(sum) / 2m;
    }

    private static decimal CornerOffcutMm2(PanelCutShape shape)
    {
        // A quarter-round corner of radius r removes (1 − π/4)·r² from the square corner.
        var factor = 1m - Pi / 4m;
        return factor * (
            Square(shape.CornerRadiusTlMm)
            + Square(shape.CornerRadiusTrMm)
            + Square(shape.CornerRadiusBrMm)
            + Square(shape.CornerRadiusBlMm));
    }

    private static decimal Square(decimal? value)
    {
        var v = Math.Max(0m, value ?? 0m);
        return v * v;
    }

    private static string Fmt(decimal? value) =>
        (value ?? 0m).ToString(CultureInfo.InvariantCulture);
}
