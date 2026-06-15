namespace CoreAlign.Application.GlassEnclosure.Services;

public record CuttingRequest1D(string Label, int LengthMm, int Quantity);

public record CuttingPattern1D(int BarIndex, int StockBarLengthMm, IReadOnlyList<CuttingCut1D> Cuts, int WasteMm);

public record CuttingCut1D(string Label, int LengthMm, int OffsetMm);

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
    public CuttingResult1D Plan(IEnumerable<CuttingRequest1D> requests, int stockBarLengthMm, int kerfMm)
    {
        if (stockBarLengthMm <= 0) throw new ArgumentOutOfRangeException(nameof(stockBarLengthMm));
        if (kerfMm < 0) throw new ArgumentOutOfRangeException(nameof(kerfMm));

        var cuts = ExpandRequests(requests, stockBarLengthMm)
            .OrderByDescending(c => c.LengthMm)
            .ToList();

        var bars = new List<MutableBar>();

        foreach (var cut in cuts)
        {
            var placed = false;
            foreach (var bar in bars)
            {
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
                var newBar = new MutableBar(stockBarLengthMm);
                newBar.Place(cut, kerfMm);
                bars.Add(newBar);
            }
        }

        var patterns = bars.Select((bar, index) => new CuttingPattern1D(
            index + 1,
            stockBarLengthMm,
            bar.Cuts.Select(p => new CuttingCut1D(p.Label, p.LengthMm, p.OffsetMm)).ToList(),
            bar.RemainingMm)).ToList();

        var totalCuts = patterns.Sum(p => p.Cuts.Count);
        var totalUsed = patterns.Sum(p => (long)p.Cuts.Sum(c => c.LengthMm));
        var totalCapacity = (long)patterns.Count * stockBarLengthMm;
        var totalWaste = totalCapacity - totalUsed;
        var utilization = totalCapacity == 0 ? 0m : (decimal)totalUsed * 100m / totalCapacity;

        return new CuttingResult1D(stockBarLengthMm, kerfMm, patterns.Count, totalCuts, totalUsed, totalWaste, decimal.Round(utilization, 3), patterns);
    }

    private static IEnumerable<(string Label, int LengthMm)> ExpandRequests(IEnumerable<CuttingRequest1D> requests, int stockBarLengthMm)
    {
        foreach (var req in requests)
        {
            if (req.Quantity <= 0) continue;
            if (req.LengthMm <= 0) continue;
            if (req.LengthMm > stockBarLengthMm)
            {
                throw new InvalidOperationException(
                    $"Cut '{req.Label}' of {req.LengthMm} mm exceeds the stock bar length of {stockBarLengthMm} mm.");
            }
            for (var i = 0; i < req.Quantity; i++) yield return (req.Label, req.LengthMm);
        }
    }

    private sealed class MutableBar
    {
        public int StockBarLengthMm { get; }
        public int RemainingMm { get; private set; }
        public List<(string Label, int LengthMm, int OffsetMm)> Cuts { get; } = new();

        public MutableBar(int stockBarLengthMm)
        {
            StockBarLengthMm = stockBarLengthMm;
            RemainingMm = stockBarLengthMm;
        }

        public void Place((string Label, int LengthMm) cut, int kerfMm)
        {
            var offsetMm = StockBarLengthMm - RemainingMm;
            if (Cuts.Count > 0)
            {
                offsetMm += kerfMm;
                RemainingMm -= kerfMm;
            }
            Cuts.Add((cut.Label, cut.LengthMm, offsetMm));
            RemainingMm -= cut.LengthMm;
        }
    }
}
