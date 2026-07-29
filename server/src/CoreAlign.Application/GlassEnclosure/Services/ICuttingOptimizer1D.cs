using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.GlassEnclosure.Services;

/// <param name="StockBarLengthMm">
/// Bar stock this profile is actually supplied in. 0 falls back to the tenant default, so callers
/// that do not track it per profile keep working unchanged.
/// </param>
public record CuttingRequest1D(string Label, int LengthMm, int Quantity, int StockBarLengthMm = 0);

public record CuttingPattern1D(int BarIndex, int StockBarLengthMm, IReadOnlyList<CuttingCut1D> Cuts, int WasteMm);

/// <param name="PieceCount">
/// How many bar-length pieces this cut was spliced into. 1 for everything that fits a single bar.
/// </param>
public record CuttingCut1D(
    string Label,
    int LengthMm,
    int OffsetMm,
    int PieceIndex = 1,
    int PieceCount = 1);

public record CuttingResult1D(
    int StockBarLengthMm,
    int KerfMm,
    int TotalBars,
    int TotalCuts,
    long TotalUsedMm,
    long TotalWasteMm,
    decimal UtilizationPercent,
    IReadOnlyList<CuttingPattern1D> Patterns);

public interface ICuttingOptimizer1D
{
    CuttingResult1D Plan(IEnumerable<CuttingRequest1D> requests, int stockBarLengthMm, int kerfMm);
}

public class FirstFitDecreasingOptimizer1D : ICuttingOptimizer1D
{
    /// <summary>
    /// Shortest piece a splice may produce. A joint is a real fabrication step, so a 40 mm stub at
    /// one end is not an acceptable answer.
    /// </summary>
    public const int MinSplicePieceMm = 300;

    /// <summary>
    /// Length lost to squaring the ends of a stock bar. 0 keeps today's numbers exactly; it is a
    /// parameter rather than a magic number so a real value can be introduced without touching the
    /// splice arithmetic.
    /// </summary>
    public const int EndTrimMm = 0;

    public CuttingResult1D Plan(IEnumerable<CuttingRequest1D> requests, int stockBarLengthMm, int kerfMm)
    {
        if (stockBarLengthMm <= 0) throw new ArgumentOutOfRangeException(nameof(stockBarLengthMm));
        if (kerfMm < 0) throw new ArgumentOutOfRangeException(nameof(kerfMm));

        var expanded = ExpandRequests(requests, stockBarLengthMm).ToList();

        // WHY grouped by bar length: a profile stocked in 7 m bars cannot share a bar with one
        // stocked in 6 m. Bin-pack each stock length on its own, then concatenate the patterns.
        var bars = new List<MutableBar>();
        foreach (var group in expanded.GroupBy(c => c.BarLengthMm).OrderBy(g => g.Key))
        {
            foreach (var cut in group.OrderByDescending(c => c.LengthMm))
            {
                var placed = false;
                foreach (var bar in bars)
                {
                    if (bar.StockBarLengthMm != cut.BarLengthMm) continue;
                    var needed = cut.LengthMm + (bar.Cuts.Count > 0 ? kerfMm : 0);
                    if (bar.RemainingMm >= needed)
                    {
                        bar.Place(cut, kerfMm);
                        placed = true;
                        break;
                    }
                }
                if (!placed)
                {
                    var newBar = new MutableBar(cut.BarLengthMm);
                    newBar.Place(cut, kerfMm);
                    bars.Add(newBar);
                }
            }
        }

        var patterns = bars.Select((bar, index) => new CuttingPattern1D(
            index + 1,
            bar.StockBarLengthMm,
            bar.Cuts.Select(p => new CuttingCut1D(p.Label, p.LengthMm, p.OffsetMm, p.PieceIndex, p.PieceCount)).ToList(),
            bar.RemainingMm)).ToList();

        var totalCuts = patterns.Sum(p => p.Cuts.Count);
        var totalUsed = patterns.Sum(p => (long)p.Cuts.Sum(c => c.LengthMm));
        var totalCapacity = patterns.Sum(p => (long)p.StockBarLengthMm);
        var totalWaste = totalCapacity - totalUsed;
        var utilization = totalCapacity == 0 ? 0m : (decimal)totalUsed * 100m / totalCapacity;

        return new CuttingResult1D(stockBarLengthMm, kerfMm, patterns.Count, totalCuts, totalUsed, totalWaste, decimal.Round(utilization, 3), patterns);
    }

    private readonly record struct ExpandedCut(string Label, int LengthMm, int BarLengthMm, int PieceIndex, int PieceCount);

    private static IEnumerable<ExpandedCut> ExpandRequests(IEnumerable<CuttingRequest1D> requests, int fallbackBarLengthMm)
    {
        foreach (var req in requests)
        {
            if (req.Quantity <= 0) continue;
            if (req.LengthMm <= 0) continue;

            var barLengthMm = req.StockBarLengthMm > 0 ? req.StockBarLengthMm : fallbackBarLengthMm;
            var usableMm = barLengthMm - EndTrimMm;
            if (usableMm <= 0)
            {
                throw new GlassCutCannotBeSplicedException(req.Label, req.LengthMm, usableMm, MinSplicePieceMm);
            }

            // A curved rail's developed length legitimately exceeds a stock bar — it is fabricated
            // from several pieces with joints, not declared impossible. Splitting EVENLY (rather
            // than "one full bar plus the remainder") is what a fabricator does: it maximises the
            // shortest piece, so a 6098 mm rail becomes 2 x 3049 mm instead of 6000 + 98.
            var pieces = (req.LengthMm + usableMm - 1) / usableMm;
            var basePieceMm = req.LengthMm / pieces;
            var longPieces = req.LengthMm - (basePieceMm * pieces);
            if (pieces > 1 && basePieceMm < MinSplicePieceMm)
            {
                throw new GlassCutCannotBeSplicedException(req.Label, req.LengthMm, usableMm, MinSplicePieceMm);
            }

            for (var i = 0; i < req.Quantity; i++)
            {
                for (var piece = 0; piece < pieces; piece++)
                {
                    var lengthMm = basePieceMm + (piece < longPieces ? 1 : 0);
                    yield return new ExpandedCut(req.Label, lengthMm, barLengthMm, piece + 1, pieces);
                }
            }
        }
    }

    private sealed class MutableBar
    {
        public int StockBarLengthMm { get; }
        public int RemainingMm { get; private set; }
        public List<(string Label, int LengthMm, int OffsetMm, int PieceIndex, int PieceCount)> Cuts { get; } = new();

        public MutableBar(int stockBarLengthMm)
        {
            StockBarLengthMm = stockBarLengthMm;
            RemainingMm = stockBarLengthMm;
        }

        public void Place(ExpandedCut cut, int kerfMm)
        {
            var offsetMm = StockBarLengthMm - RemainingMm;
            if (Cuts.Count > 0)
            {
                offsetMm += kerfMm;
                RemainingMm -= kerfMm;
            }
            Cuts.Add((cut.Label, cut.LengthMm, offsetMm, cut.PieceIndex, cut.PieceCount));
            RemainingMm -= cut.LengthMm;
        }
    }
}
