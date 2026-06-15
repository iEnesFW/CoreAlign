namespace CoreAlign.Application.GlassEnclosure.Services;

public record CuttingRequest2D(string Label, int WidthMm, int HeightMm, int Quantity);

public record CuttingPlacement2D(string Label, int X, int Y, int WidthMm, int HeightMm, bool Rotated);

public record CuttingSheet2D(
    int SheetIndex,
    int WidthMm,
    int HeightMm,
    IReadOnlyList<CuttingPlacement2D> Placements,
    long WasteMm2);

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
    IReadOnlyList<string> Unplaced);

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

        var rects = ExpandRequests(requests, sheetWidthMm, sheetHeightMm)
            .OrderByDescending(r => r.WidthMm * r.HeightMm)
            .ToList();

        var sheets = new List<MutableSheet>();
        var unplaced = new List<string>();

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

        var resultSheets = sheets.Select((s, i) => new CuttingSheet2D(
            i + 1, sheetWidthMm, sheetHeightMm, s.Placements, s.WasteAreaMm2)).ToList();

        var totalUsed = resultSheets.Sum(s => (long)s.Placements.Sum(p => p.WidthMm * p.HeightMm));
        var totalCapacity = (long)resultSheets.Count * sheetWidthMm * sheetHeightMm;
        var totalWaste = totalCapacity - totalUsed;
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
            unplaced);
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
                throw new InvalidOperationException(
                    $"Cut '{req.Label}' of {req.WidthMm}x{req.HeightMm} mm exceeds jumbo {sheetWidthMm}x{sheetHeightMm} mm.");
            }
            for (var i = 0; i < req.Quantity; i++)
            {
                yield return new CuttingRequest2D(req.Label, req.WidthMm, req.HeightMm, 1);
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
            (long)_widthMm * _heightMm - _placements.Sum(p => (long)p.WidthMm * p.HeightMm);

        public bool TryPlace(CuttingRequest2D rect, int kerfMm, bool guillotineOnly)
        {
            FreeRect? bestFit = null;
            var bestShortSide = int.MaxValue;
            var bestRotated = false;

            foreach (var free in _freeRects)
            {
                if (TryFit(rect.WidthMm, rect.HeightMm, kerfMm, free, out var shortSide))
                {
                    if (shortSide < bestShortSide)
                    {
                        bestFit = free;
                        bestShortSide = shortSide;
                        bestRotated = false;
                    }
                }
                if (rect.WidthMm != rect.HeightMm && TryFit(rect.HeightMm, rect.WidthMm, kerfMm, free, out shortSide))
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
            _placements.Add(new CuttingPlacement2D(rect.Label, placedX, placedY, placedWidth, placedHeight, bestRotated));

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

        private static bool TryFit(int wantedW, int wantedH, int kerfMm, FreeRect free, out int shortSide)
        {
            var w = wantedW + (free.X > 0 ? kerfMm : 0);
            var h = wantedH + (free.Y > 0 ? kerfMm : 0);
            if (w <= free.Width && h <= free.Height)
            {
                var leftover1 = free.Width - w;
                var leftover2 = free.Height - h;
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
                if (placedX > free.X) toAdd.Add(new FreeRect(free.X, free.Y, placedX - free.X, free.Height));
                if (placedX + placedWidth + kerfMm < free.X + free.Width)
                {
                    var rightX = placedX + placedWidth + kerfMm;
                    toAdd.Add(new FreeRect(rightX, free.Y, free.X + free.Width - rightX, free.Height));
                }
                if (placedY > free.Y) toAdd.Add(new FreeRect(free.X, free.Y, free.Width, placedY - free.Y));
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
