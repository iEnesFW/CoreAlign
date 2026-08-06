using System.Text.Json;

namespace CoreAlign.Application.GlassEnclosure.Cutting;

/// <summary>
/// Server-side gate for a shaped pane's polygon outline — the same contract the designer enforces
/// client-side (normalizePanelOutline), because the number this outline produces is the one the BOM
/// prices and the cut list orders. A self-intersecting outline is not a rendering problem: its
/// shoelace lobes cancel, so <see cref="PanelCutGeometry"/> under-reports the silhouette area and
/// the customer is billed for glass the fabricator never cuts. The frontend cannot be the only
/// defence — any API caller can write ShapePointsJson directly.
/// </summary>
public static class PanelShapeGeometry
{
    public const decimal MinAreaMm2 = 10_000m;
    private const decimal BoxToleranceMm = 1m;
    private const decimal MinVertexGapMm = 1m;

    public enum ShapeRejection
    {
        None = 0,
        Unparsable = 1,
        TooFewPoints = 2,
        SelfIntersecting = 3,
        Degenerate = 4,
        OutOfBounds = 5,
    }

    public readonly record struct ShapeCheck(bool IsValid, ShapeRejection Rejection)
    {
        public static ShapeCheck Ok => new(true, ShapeRejection.None);
        public static ShapeCheck Fail(ShapeRejection rejection) => new(false, rejection);
    }

    /// <summary>
    /// Validates a polygon outline against the pane it claims to describe. Panel-local coordinates:
    /// bottom-centred, y-up — x within ±width/2, y within [0, height]. Height is unknown when the
    /// pane inherits the run height, so the vertical upper bound is only enforced when supplied.
    /// </summary>
    public static ShapeCheck CheckPolygonJson(string? json, decimal widthMm, decimal? heightMm)
    {
        var points = ParsePoints(json);
        if (points is null) return ShapeCheck.Fail(ShapeRejection.Unparsable);

        var distinct = DropRepeats(points);
        if (distinct.Count < 3) return ShapeCheck.Fail(ShapeRejection.TooFewPoints);

        var halfW = widthMm / 2m + BoxToleranceMm;
        foreach (var p in distinct)
        {
            if (Math.Abs(p.X) > halfW) return ShapeCheck.Fail(ShapeRejection.OutOfBounds);
            if (p.Y < -BoxToleranceMm) return ShapeCheck.Fail(ShapeRejection.OutOfBounds);
            if (heightMm.HasValue && p.Y > heightMm.Value + BoxToleranceMm)
                return ShapeCheck.Fail(ShapeRejection.OutOfBounds);
        }

        if (SelfIntersects(distinct)) return ShapeCheck.Fail(ShapeRejection.SelfIntersecting);
        if (ShoelaceAreaMm2(distinct) < MinAreaMm2) return ShapeCheck.Fail(ShapeRejection.Degenerate);
        return ShapeCheck.Ok;
    }

    private readonly record struct Pt(decimal X, decimal Y);

    private static List<Pt>? ParsePoints(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;
            var points = new List<Pt>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object) return null;
                if (!element.TryGetProperty("x", out var x) || !element.TryGetProperty("y", out var y))
                    return null;
                if (x.ValueKind != JsonValueKind.Number || y.ValueKind != JsonValueKind.Number)
                    return null;
                if (!x.TryGetDecimal(out var xv) || !y.TryGetDecimal(out var yv)) return null;
                points.Add(new Pt(xv, yv));
            }
            return points;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static List<Pt> DropRepeats(List<Pt> points)
    {
        var result = new List<Pt>();
        foreach (var p in points)
        {
            if (result.Count > 0 && Distance(result[^1], p) < MinVertexGapMm) continue;
            result.Add(p);
        }
        while (result.Count >= 2 && Distance(result[^1], result[0]) < MinVertexGapMm)
        {
            result.RemoveAt(result.Count - 1);
        }
        return result;
    }

    private static decimal Distance(Pt a, Pt b)
    {
        var dx = (double)(a.X - b.X);
        var dy = (double)(a.Y - b.Y);
        return (decimal)Math.Sqrt(dx * dx + dy * dy);
    }

    private static decimal Orient(Pt o, Pt a, Pt b) =>
        (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);

    private static bool OnCollinearSegment(Pt a, Pt b, Pt p) =>
        Math.Min(a.X, b.X) <= p.X && p.X <= Math.Max(a.X, b.X) &&
        Math.Min(a.Y, b.Y) <= p.Y && p.Y <= Math.Max(a.Y, b.Y);

    // Strict crossing + collinear-touch: a loop that passes exactly through one of its own vertices
    // pinches the contour just as fatally as a plain crossing (mirrors the designer's segmentsCross).
    private static bool SegmentsCross(Pt p1, Pt p2, Pt p3, Pt p4)
    {
        var d1 = Orient(p3, p4, p1);
        var d2 = Orient(p3, p4, p2);
        var d3 = Orient(p1, p2, p3);
        var d4 = Orient(p1, p2, p4);
        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            return true;
        if (d1 == 0 && OnCollinearSegment(p3, p4, p1)) return true;
        if (d2 == 0 && OnCollinearSegment(p3, p4, p2)) return true;
        if (d3 == 0 && OnCollinearSegment(p1, p2, p3)) return true;
        return d4 == 0 && OnCollinearSegment(p1, p2, p4);
    }

    private static bool SelfIntersects(List<Pt> pts)
    {
        var n = pts.Count;
        if (n < 4) return false;
        for (var i = 0; i < n; i += 1)
        {
            var a1 = pts[i];
            var a2 = pts[(i + 1) % n];
            for (var j = i + 2; j < n; j += 1)
            {
                if (i == 0 && j == n - 1) continue;
                if (SegmentsCross(a1, a2, pts[j], pts[(j + 1) % n])) return true;
            }
        }
        return false;
    }

    private static decimal ShoelaceAreaMm2(List<Pt> pts)
    {
        decimal sum = 0;
        for (var i = 0; i < pts.Count; i += 1)
        {
            var a = pts[i];
            var b = pts[(i + 1) % pts.Count];
            sum += a.X * b.Y - b.X * a.Y;
        }
        return Math.Abs(sum) / 2m;
    }
}
