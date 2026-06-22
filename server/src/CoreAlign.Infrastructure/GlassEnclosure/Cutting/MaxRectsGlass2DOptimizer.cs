using CoreAlign.Application.GlassEnclosure.Cutting;

namespace CoreAlign.Infrastructure.GlassEnclosure.Cutting;

public sealed class MaxRectsGlass2DOptimizer : IGlass2DNestingOptimizer
{
    public Task<Glass2DNestingResult> OptimizeAsync(
        IReadOnlyList<GlassPanelRequest> panels,
        IReadOnlyList<GlassSheet> stockSheets,
        NestingOptions options,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(panels);
        ArgumentNullException.ThrowIfNull(stockSheets);
        ArgumentNullException.ThrowIfNull(options);

        if (stockSheets.Count == 0)
        {
            throw new ArgumentException("At least one stock sheet must be supplied.", nameof(stockSheets));
        }

        var expanded = ExpandPanels(panels, stockSheets, out var oversize);

        var sortedItems = expanded
            .OrderByDescending(p => p.Width * p.Height)
            .ThenByDescending(p => Math.Max(p.Width, p.Height))
            .ToList();

        var openSheets = new List<MaxRectsSheet>();
        var unplaced = new List<UnplacedPanel>(oversize);
        var heuristic = ParseHeuristic(options.Heuristic);

        foreach (var item in sortedItems)
        {
            ct.ThrowIfCancellationRequested();
            var placed = false;

            foreach (var sheet in openSheets)
            {
                if (sheet.TryPlace(item, heuristic, options.GuillotineOnly))
                {
                    placed = true;
                    break;
                }
            }

            if (placed) continue;

            var template = ChooseStockSheet(stockSheets, openSheets.Count);
            var newSheet = new MaxRectsSheet(
                template.SheetId,
                openSheets.Count + 1,
                template.WidthMm,
                template.HeightMm,
                template.SawKerfMm,
                template.EdgeMarginMm);

            if (newSheet.TryPlace(item, heuristic, options.GuillotineOnly))
            {
                openSheets.Add(newSheet);
            }
            else
            {
                unplaced.Add(new UnplacedPanel(item.PanelId, item.Label, item.Width, item.Height, "NoFit"));
            }
        }

        var placedSheets = openSheets.Select(s => s.ToPlacedSheet()).ToList();
        var totalUsed = placedSheets.Sum(s => s.UsedAreaMm2);
        var totalWaste = placedSheets.Sum(s => s.WasteAreaMm2);
        var totalCapacity = totalUsed + totalWaste;
        var utilization = totalCapacity == 0m ? 0m : decimal.Round(totalUsed * 100m / totalCapacity, 3);

        var result = new Glass2DNestingResult(
            options.Algorithm,
            options.Heuristic,
            placedSheets,
            totalUsed,
            totalWaste,
            utilization,
            placedSheets.Count,
            unplaced);

        return Task.FromResult(result);
    }

    private static GlassSheet ChooseStockSheet(IReadOnlyList<GlassSheet> stockSheets, int currentCount)
    {
        return stockSheets[Math.Min(currentCount, stockSheets.Count - 1)];
    }

    private static FreeRectHeuristic ParseHeuristic(string heuristic) => heuristic switch
    {
        "BestAreaFit" or "BAF" => FreeRectHeuristic.BestAreaFit,
        "BestLongSideFit" or "BLSF" => FreeRectHeuristic.BestLongSideFit,
        "BottomLeft" or "BL" => FreeRectHeuristic.BottomLeft,
        _ => FreeRectHeuristic.BestShortSideFit,
    };

    private static List<NestingItem> ExpandPanels(
        IReadOnlyList<GlassPanelRequest> panels,
        IReadOnlyList<GlassSheet> stockSheets,
        out List<UnplacedPanel> oversize)
    {
        oversize = new List<UnplacedPanel>();
        var items = new List<NestingItem>();

        var maxUsableWidth = stockSheets.Max(s => s.WidthMm - 2m * s.EdgeMarginMm);
        var maxUsableHeight = stockSheets.Max(s => s.HeightMm - 2m * s.EdgeMarginMm);

        foreach (var panel in panels)
        {
            if (panel.Quantity <= 0 || panel.WidthMm <= 0m || panel.HeightMm <= 0m) continue;

            var fitsDirect = panel.WidthMm <= maxUsableWidth && panel.HeightMm <= maxUsableHeight;
            var fitsRotated = panel.AllowRotation && panel.HeightMm <= maxUsableWidth && panel.WidthMm <= maxUsableHeight;

            if (!fitsDirect && !fitsRotated)
            {
                for (var i = 0; i < panel.Quantity; i++)
                {
                    oversize.Add(new UnplacedPanel(panel.PanelId, panel.Label, panel.WidthMm, panel.HeightMm, "ExceedsSheet"));
                }
                continue;
            }

            for (var i = 0; i < panel.Quantity; i++)
            {
                items.Add(new NestingItem(
                    panel.PanelId,
                    panel.Label,
                    panel.WidthMm,
                    panel.HeightMm,
                    panel.AllowRotation,
                    panel.Shape,
                    panel.NominalHeightMm));
            }
        }

        return items;
    }

    private enum FreeRectHeuristic
    {
        BestShortSideFit,
        BestAreaFit,
        BestLongSideFit,
        BottomLeft,
    }

    private sealed record NestingItem(
        Guid PanelId,
        string Label,
        decimal Width,
        decimal Height,
        bool AllowRotation,
        PanelCutShape? Shape,
        decimal? NominalHeightMm);

    private sealed class MaxRectsSheet
    {
        private readonly Guid _sheetId;
        private readonly int _index;
        private readonly decimal _sheetWidth;
        private readonly decimal _sheetHeight;
        private readonly decimal _kerf;
        private readonly decimal _edgeMargin;
        private readonly List<FreeRect> _freeRects;
        private readonly List<PlacedPanel> _placements = new();

        public MaxRectsSheet(
            Guid sheetId,
            int index,
            decimal sheetWidth,
            decimal sheetHeight,
            decimal kerf,
            decimal edgeMargin)
        {
            _sheetId = sheetId;
            _index = index;
            _sheetWidth = sheetWidth;
            _sheetHeight = sheetHeight;
            _kerf = kerf;
            _edgeMargin = edgeMargin;
            _freeRects = new List<FreeRect>
            {
                new(edgeMargin, edgeMargin, sheetWidth - 2m * edgeMargin, sheetHeight - 2m * edgeMargin),
            };
        }

        public bool TryPlace(NestingItem item, FreeRectHeuristic heuristic, bool guillotineOnly)
        {
            if (!FindBestFit(item, heuristic, out var bestRect, out var bestWidth, out var bestHeight, out var rotated))
            {
                return false;
            }

            var placed = new PlacedPanel(
                item.PanelId, item.Label, bestRect.X, bestRect.Y, bestWidth, bestHeight, rotated,
                item.Shape, item.NominalHeightMm);
            _placements.Add(placed);

            if (guillotineOnly)
            {
                SplitGuillotine(bestRect, bestWidth, bestHeight);
            }
            else
            {
                SplitMaximalRectangles(bestRect, bestWidth, bestHeight);
            }
            PruneFreeRects();
            return true;
        }

        public PlacedSheet ToPlacedSheet()
        {
            var used = _placements.Sum(p => p.WidthMm * p.HeightMm);
            var capacity = _sheetWidth * _sheetHeight;
            var waste = capacity - used;
            var util = capacity == 0m ? 0m : decimal.Round(used * 100m / capacity, 3);
            return new PlacedSheet(_sheetId, _index, _sheetWidth, _sheetHeight, _placements, used, waste, util);
        }

        private bool FindBestFit(
            NestingItem item,
            FreeRectHeuristic heuristic,
            out FreeRect bestRect,
            out decimal bestWidth,
            out decimal bestHeight,
            out bool rotated)
        {
            bestRect = default!;
            bestWidth = 0m;
            bestHeight = 0m;
            rotated = false;
            var bestScore1 = decimal.MaxValue;
            var bestScore2 = decimal.MaxValue;
            var found = false;

            foreach (var free in _freeRects)
            {
                EvaluateOrientation(item.Width, item.Height, free, heuristic, out var fit1, out var score1, out var score2);
                if (fit1 && (score1 < bestScore1 || (score1 == bestScore1 && score2 < bestScore2)))
                {
                    bestRect = free;
                    bestWidth = item.Width;
                    bestHeight = item.Height;
                    rotated = false;
                    bestScore1 = score1;
                    bestScore2 = score2;
                    found = true;
                }

                if (item.AllowRotation && item.Width != item.Height)
                {
                    EvaluateOrientation(item.Height, item.Width, free, heuristic, out var fit2, out var rotScore1, out var rotScore2);
                    if (fit2 && (rotScore1 < bestScore1 || (rotScore1 == bestScore1 && rotScore2 < bestScore2)))
                    {
                        bestRect = free;
                        bestWidth = item.Height;
                        bestHeight = item.Width;
                        rotated = true;
                        bestScore1 = rotScore1;
                        bestScore2 = rotScore2;
                        found = true;
                    }
                }
            }
            return found;
        }

        private static void EvaluateOrientation(
            decimal wantW,
            decimal wantH,
            FreeRect free,
            FreeRectHeuristic heuristic,
            out bool fits,
            out decimal score1,
            out decimal score2)
        {
            fits = wantW <= free.Width && wantH <= free.Height;
            if (!fits)
            {
                score1 = decimal.MaxValue;
                score2 = decimal.MaxValue;
                return;
            }
            var leftoverHoriz = free.Width - wantW;
            var leftoverVert = free.Height - wantH;
            var shortSide = Math.Min(leftoverHoriz, leftoverVert);
            var longSide = Math.Max(leftoverHoriz, leftoverVert);

            switch (heuristic)
            {
                case FreeRectHeuristic.BestAreaFit:
                    score1 = free.Width * free.Height - wantW * wantH;
                    score2 = shortSide;
                    break;
                case FreeRectHeuristic.BestLongSideFit:
                    score1 = longSide;
                    score2 = shortSide;
                    break;
                case FreeRectHeuristic.BottomLeft:
                    score1 = free.Y + wantH;
                    score2 = free.X;
                    break;
                default:
                    score1 = shortSide;
                    score2 = longSide;
                    break;
            }
        }

        private void SplitGuillotine(FreeRect parent, decimal placedW, decimal placedH)
        {
            _freeRects.Remove(parent);

            var horizontalArea = (parent.Width - placedW - _kerf) * parent.Height;
            var verticalArea = parent.Width * (parent.Height - placedH - _kerf);

            if (horizontalArea >= verticalArea)
            {
                var rightWidth = parent.Width - placedW - _kerf;
                if (rightWidth > 0m)
                {
                    _freeRects.Add(new FreeRect(parent.X + placedW + _kerf, parent.Y, rightWidth, parent.Height));
                }
                var topHeight = parent.Height - placedH - _kerf;
                if (topHeight > 0m)
                {
                    _freeRects.Add(new FreeRect(parent.X, parent.Y + placedH + _kerf, placedW, topHeight));
                }
            }
            else
            {
                var topHeight = parent.Height - placedH - _kerf;
                if (topHeight > 0m)
                {
                    _freeRects.Add(new FreeRect(parent.X, parent.Y + placedH + _kerf, parent.Width, topHeight));
                }
                var rightWidth = parent.Width - placedW - _kerf;
                if (rightWidth > 0m)
                {
                    _freeRects.Add(new FreeRect(parent.X + placedW + _kerf, parent.Y, rightWidth, placedH));
                }
            }
        }

        private void SplitMaximalRectangles(FreeRect parent, decimal placedW, decimal placedH)
        {
            var placedX = parent.X;
            var placedY = parent.Y;
            var placedRight = placedX + placedW + _kerf;
            var placedTop = placedY + placedH + _kerf;

            var toRemove = new List<FreeRect>();
            var toAdd = new List<FreeRect>();

            foreach (var free in _freeRects)
            {
                if (!Intersects(free, placedX, placedY, placedW + _kerf, placedH + _kerf)) continue;
                toRemove.Add(free);

                if (placedX > free.X)
                {
                    toAdd.Add(new FreeRect(free.X, free.Y, placedX - free.X, free.Height));
                }
                if (placedRight < free.X + free.Width)
                {
                    toAdd.Add(new FreeRect(placedRight, free.Y, free.X + free.Width - placedRight, free.Height));
                }
                if (placedY > free.Y)
                {
                    toAdd.Add(new FreeRect(free.X, free.Y, free.Width, placedY - free.Y));
                }
                if (placedTop < free.Y + free.Height)
                {
                    toAdd.Add(new FreeRect(free.X, placedTop, free.Width, free.Y + free.Height - placedTop));
                }
            }

            foreach (var r in toRemove) _freeRects.Remove(r);
            foreach (var r in toAdd)
            {
                if (r.Width > 0m && r.Height > 0m) _freeRects.Add(r);
            }
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

        private static bool Intersects(FreeRect a, decimal bx, decimal by, decimal bw, decimal bh) =>
            bx < a.X + a.Width && bx + bw > a.X && by < a.Y + a.Height && by + bh > a.Y;

        private static bool Contains(FreeRect outer, FreeRect inner) =>
            outer.X <= inner.X && outer.Y <= inner.Y &&
            outer.X + outer.Width >= inner.X + inner.Width &&
            outer.Y + outer.Height >= inner.Y + inner.Height;
    }

    private sealed record FreeRect(decimal X, decimal Y, decimal Width, decimal Height);
}
