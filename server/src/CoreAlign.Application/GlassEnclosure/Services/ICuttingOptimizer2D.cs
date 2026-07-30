using CoreAlign.Application.GlassEnclosure.Cutting;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.GlassEnclosure.Services;

public record CuttingRequest2D(
    string Label,
    int WidthMm,
    int HeightMm,
    int Quantity,
    PanelCutShape? Shape = null,
    int? NominalHeightMm = null)
{
    /// <summary>
    /// Cuts that may share a jumbo sheet, i.e. same glass type and thickness. Each key gets its own
    /// sheet pool. <c>null</c> puts every cut in one pool, which is what callers that do not track
    /// glass identity keep getting.
    /// </summary>
    public string? GroupKey { get; init; }
}

public record CuttingPlacement2D(
    string Label,
    int X,
    int Y,
    int WidthMm,
    int HeightMm,
    bool Rotated,
    PanelCutShape? Shape = null,
    int? NominalHeightMm = null);

/// <summary>
/// Net glass a placement consumes. A triangle/arch/trapezoid still occupies its bounding blank on
/// the sheet (packing is unchanged), but the offcut cut away INSIDE that blank is real waste — the
/// MaxRects optimizer already accounted for it this way while this one counted the whole blank, so
/// the two cutting screens reported different utilisation for the very same panels.
/// Rotation-invariant: the authoring dimensions are used, not the placed ones.
/// </summary>
public static class CuttingPlacementArea
{
    public static long NetAreaMm2(CuttingPlacement2D p)
    {
        var authoredWidth = p.Rotated ? p.HeightMm : p.WidthMm;
        var authoredHeight = p.NominalHeightMm ?? (p.Rotated ? p.WidthMm : p.HeightMm);
        return (long)PanelCutGeometry.NetAreaMm2(authoredWidth, authoredHeight, p.Shape);
    }
}

public record CuttingSheet2D(
    int SheetIndex,
    int WidthMm,
    int HeightMm,
    IReadOnlyList<CuttingPlacement2D> Placements,
    long WasteMm2)
{
    /// <summary>Sheet pool this sheet was cut from. See <see cref="CuttingRequest2D.GroupKey"/>.</summary>
    public string? GroupKey { get; init; }
}

/// <summary>Per-sheet-pool totals. Summing the groups reproduces the result totals exactly.</summary>
public record CuttingGroup2D(
    string? GroupKey,
    int TotalSheets,
    long TotalUsedMm2,
    long TotalWasteMm2,
    decimal UtilizationPercent);

public record CuttingResult2D(
    int SheetWidthMm,
    int SheetHeightMm,
    int KerfMm,
    bool GuillotineOnly,
    int TotalSheets,
    long TotalUsedMm2,
    long TotalWasteMm2,
    decimal UtilizationPercent,
    IReadOnlyList<CuttingSheet2D> Sheets,
    IReadOnlyList<string> Unplaced)
{
    public IReadOnlyList<CuttingGroup2D> Groups { get; init; } = Array.Empty<CuttingGroup2D>();
}

public interface ICuttingOptimizer2D
{
    CuttingResult2D Plan(
        IEnumerable<CuttingRequest2D> requests,
        int sheetWidthMm,
        int sheetHeightMm,
        int kerfMm,
        bool guillotineOnly);
}

public class MaximalRectanglesOptimizer2D : ICuttingOptimizer2D
{
    public CuttingResult2D Plan(
        IEnumerable<CuttingRequest2D> requests,
        int sheetWidthMm,
        int sheetHeightMm,
        int kerfMm,
        bool guillotineOnly)
    {
        if (sheetWidthMm <= 0) throw new ArgumentOutOfRangeException(nameof(sheetWidthMm));
        if (sheetHeightMm <= 0) throw new ArgumentOutOfRangeException(nameof(sheetHeightMm));
        if (kerfMm < 0) throw new ArgumentOutOfRangeException(nameof(kerfMm));

        var expanded = ExpandRequests(requests, sheetWidthMm, sheetHeightMm).ToList();

        var resultSheets = new List<CuttingSheet2D>();
        var groups = new List<CuttingGroup2D>();
        var unplaced = new List<string>();

        // WHY: 6 mm and 8 mm glass cannot come off one jumbo sheet — one sheet pool per group, then concatenate.
        foreach (var group in expanded.GroupBy(r => r.GroupKey ?? string.Empty))
        {
            var sheets = PackGroup(group, sheetWidthMm, sheetHeightMm, kerfMm, guillotineOnly, unplaced);

            var groupUsed = 0L;
            foreach (var sheet in sheets)
            {
                groupUsed += sheet.Placements.Sum(CuttingPlacementArea.NetAreaMm2);
                resultSheets.Add(new CuttingSheet2D(
                    resultSheets.Count + 1, sheetWidthMm, sheetHeightMm, sheet.Placements, sheet.WasteAreaMm2)
                {
                    GroupKey = group.Key.Length == 0 ? null : group.Key,
                });
            }

            var groupCapacity = (long)sheets.Count * sheetWidthMm * sheetHeightMm;
            groups.Add(new CuttingGroup2D(
                group.Key.Length == 0 ? null : group.Key,
                sheets.Count,
                groupUsed,
                groupCapacity - groupUsed,
                groupCapacity == 0 ? 0m : decimal.Round((decimal)groupUsed * 100m / groupCapacity, 3)));
        }

        var totalUsed = groups.Sum(g => g.TotalUsedMm2);
        var totalWaste = groups.Sum(g => g.TotalWasteMm2);
        var totalCapacity = totalUsed + totalWaste;
        var utilization = totalCapacity == 0 ? 0m : (decimal)totalUsed * 100m / totalCapacity;

        return new CuttingResult2D(
            sheetWidthMm,
            sheetHeightMm,
            kerfMm,
            guillotineOnly,
            resultSheets.Count,
            totalUsed,
            totalWaste,
            decimal.Round(utilization, 3),
            resultSheets,
            unplaced)
        {
            Groups = groups,
        };
    }

    private static List<MutableSheet> PackGroup(
        IEnumerable<CuttingRequest2D> groupRequests,
        int sheetWidthMm,
        int sheetHeightMm,
        int kerfMm,
        bool guillotineOnly,
        List<string> unplaced)
    {
        var sheets = new List<MutableSheet>();
        var rects = groupRequests.OrderByDescending(r => (long)r.WidthMm * r.HeightMm);

        foreach (var rect in rects)
        {
            var placed = false;
            foreach (var sheet in sheets)
            {
                if (sheet.TryPlace(rect, kerfMm, guillotineOnly))
                {
                    placed = true;
                    break;
                }
            }
            if (!placed)
            {
                var sheet = new MutableSheet(sheetWidthMm, sheetHeightMm);
                if (sheet.TryPlace(rect, kerfMm, guillotineOnly))
                {
                    sheets.Add(sheet);
                }
                else
                {
                    unplaced.Add(rect.Label);
                }
            }
        }

        return sheets;
    }

    private static IEnumerable<CuttingRequest2D> ExpandRequests(
        IEnumerable<CuttingRequest2D> requests,
        int sheetWidthMm,
        int sheetHeightMm)
    {
        foreach (var req in requests)
        {
            if (req.Quantity <= 0 || req.WidthMm <= 0 || req.HeightMm <= 0) continue;
            var fitsDirect = req.WidthMm <= sheetWidthMm && req.HeightMm <= sheetHeightMm;
            var fitsRotated = req.HeightMm <= sheetWidthMm && req.WidthMm <= sheetHeightMm;
            if (!fitsDirect && !fitsRotated)
            {
                // WHY: an oversized panel is user input, not a server fault — name the cut instead of a 500.
                throw new GlassCutExceedsJumboSheetException(
                    req.Label, req.WidthMm, req.HeightMm, sheetWidthMm, sheetHeightMm);
            }
            for (var i = 0; i < req.Quantity; i++)
            {
                yield return new CuttingRequest2D(req.Label, req.WidthMm, req.HeightMm, 1, req.Shape, req.NominalHeightMm)
                {
                    GroupKey = req.GroupKey,
                };
            }
        }
    }

    private sealed class MutableSheet
    {
        private readonly int _widthMm;
        private readonly int _heightMm;
        private readonly List<FreeRect> _freeRects;
        private readonly List<CuttingPlacement2D> _placements = new();

        public MutableSheet(int widthMm, int heightMm)
        {
            _widthMm = widthMm;
            _heightMm = heightMm;
            _freeRects = new List<FreeRect> { new(0, 0, widthMm, heightMm) };
        }

        public IReadOnlyList<CuttingPlacement2D> Placements => _placements;
        public long WasteAreaMm2 =>
            (long)_widthMm * _heightMm - _placements.Sum(CuttingPlacementArea.NetAreaMm2);

        public bool TryPlace(CuttingRequest2D rect, int kerfMm, bool guillotineOnly)
        {
            FreeRect? bestFit = null;
            var bestShortSide = int.MaxValue;
            var bestRotated = false;

            foreach (var free in _freeRects)
            {
                if (TryFit(rect.WidthMm, rect.HeightMm, free, out var shortSide))
                {
                    if (shortSide < bestShortSide)
                    {
                        bestFit = free;
                        bestShortSide = shortSide;
                        bestRotated = false;
                    }
                }
                if (rect.WidthMm != rect.HeightMm && TryFit(rect.HeightMm, rect.WidthMm, free, out shortSide))
                {
                    if (shortSide < bestShortSide)
                    {
                        bestFit = free;
                        bestShortSide = shortSide;
                        bestRotated = true;
                    }
                }
            }

            if (bestFit is null) return false;

            var placedWidth = bestRotated ? rect.HeightMm : rect.WidthMm;
            var placedHeight = bestRotated ? rect.WidthMm : rect.HeightMm;
            var placedX = bestFit.X;
            var placedY = bestFit.Y;
            _placements.Add(new CuttingPlacement2D(
                rect.Label, placedX, placedY, placedWidth, placedHeight, bestRotated, rect.Shape, rect.NominalHeightMm));

            if (guillotineOnly)
            {
                SplitGuillotine(bestFit, placedWidth, placedHeight, kerfMm);
            }
            else
            {
                SplitMaximalRectangles(bestFit, placedX, placedY, placedWidth, placedHeight, kerfMm);
            }

            PruneFreeRects();
            return true;
        }

        // WHY: the split already excised the kerf, so charging it here reserved it twice per cut plane.
        private static bool TryFit(int wantedW, int wantedH, FreeRect free, out int shortSide)
        {
            if (wantedW <= free.Width && wantedH <= free.Height)
            {
                var leftover1 = free.Width - wantedW;
                var leftover2 = free.Height - wantedH;
                shortSide = Math.Min(leftover1, leftover2);
                return true;
            }
            shortSide = int.MaxValue;
            return false;
        }

        private void SplitGuillotine(FreeRect parent, int placedWidth, int placedHeight, int kerfMm)
        {
            _freeRects.Remove(parent);
            var horizontalArea = (parent.Width - placedWidth - kerfMm) * parent.Height;
            var verticalArea = parent.Width * (parent.Height - placedHeight - kerfMm);

            if (horizontalArea >= verticalArea)
            {
                var rightX = parent.X + placedWidth + kerfMm;
                var rightWidth = parent.Width - placedWidth - kerfMm;
                if (rightWidth > 0)
                {
                    _freeRects.Add(new FreeRect(rightX, parent.Y, rightWidth, parent.Height));
                }
                var topY = parent.Y + placedHeight + kerfMm;
                var topHeight = parent.Height - placedHeight - kerfMm;
                if (topHeight > 0)
                {
                    _freeRects.Add(new FreeRect(parent.X, topY, placedWidth, topHeight));
                }
            }
            else
            {
                var topY = parent.Y + placedHeight + kerfMm;
                var topHeight = parent.Height - placedHeight - kerfMm;
                if (topHeight > 0)
                {
                    _freeRects.Add(new FreeRect(parent.X, topY, parent.Width, topHeight));
                }
                var rightX = parent.X + placedWidth + kerfMm;
                var rightWidth = parent.Width - placedWidth - kerfMm;
                if (rightWidth > 0)
                {
                    _freeRects.Add(new FreeRect(rightX, parent.Y, rightWidth, placedHeight));
                }
            }
        }

        private void SplitMaximalRectangles(FreeRect parent, int placedX, int placedY, int placedWidth, int placedHeight, int kerfMm)
        {
            var toRemove = new List<FreeRect>();
            var toAdd = new List<FreeRect>();
            foreach (var free in _freeRects)
            {
                if (!Intersects(free, placedX, placedY, placedWidth + kerfMm, placedHeight + kerfMm)) continue;
                toRemove.Add(free);
                // WHY: every side of the piece is a cut plane, so left/bottom free material stops one kerf short too.
                if (placedX - kerfMm > free.X) toAdd.Add(new FreeRect(free.X, free.Y, placedX - kerfMm - free.X, free.Height));
                if (placedX + placedWidth + kerfMm < free.X + free.Width)
                {
                    var rightX = placedX + placedWidth + kerfMm;
                    toAdd.Add(new FreeRect(rightX, free.Y, free.X + free.Width - rightX, free.Height));
                }
                if (placedY - kerfMm > free.Y) toAdd.Add(new FreeRect(free.X, free.Y, free.Width, placedY - kerfMm - free.Y));
                if (placedY + placedHeight + kerfMm < free.Y + free.Height)
                {
                    var topY = placedY + placedHeight + kerfMm;
                    toAdd.Add(new FreeRect(free.X, topY, free.Width, free.Y + free.Height - topY));
                }
            }
            foreach (var r in toRemove) _freeRects.Remove(r);
            _freeRects.AddRange(toAdd);
        }

        private void PruneFreeRects()
        {
            for (var i = 0; i < _freeRects.Count; i++)
            {
                for (var j = _freeRects.Count - 1; j > i; j--)
                {
                    if (Contains(_freeRects[i], _freeRects[j]))
                    {
                        _freeRects.RemoveAt(j);
                    }
                    else if (Contains(_freeRects[j], _freeRects[i]))
                    {
                        _freeRects.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
        }

        private static bool Intersects(FreeRect a, int bx, int by, int bw, int bh) =>
            bx < a.X + a.Width && bx + bw > a.X && by < a.Y + a.Height && by + bh > a.Y;

        private static bool Contains(FreeRect outer, FreeRect inner) =>
            outer.X <= inner.X && outer.Y <= inner.Y &&
            outer.X + outer.Width >= inner.X + inner.Width &&
            outer.Y + outer.Height >= inner.Y + inner.Height;
    }

    private sealed record FreeRect(int X, int Y, int Width, int Height);
}
