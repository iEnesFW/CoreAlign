using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.Services;

public record BomQuoteTotals(decimal Subtotal, decimal MarginAmount, decimal TaxAmount, decimal GrandTotal);

public static class BomQuoteTotalsCalculator
{
    public const decimal TaxRate = 0.20m;

    public static BomQuoteTotals Calculate(IEnumerable<GlassProjectBOMLine> lines, decimal marginPercent)
    {
        var subtotal = lines.Sum(l => l.LineCost);
        var marginAmount = subtotal * (marginPercent / 100m);
        var afterMargin = subtotal + marginAmount;
        var taxAmount = afterMargin * TaxRate;
        var grandTotal = afterMargin + taxAmount;
        return new BomQuoteTotals(
            decimal.Round(subtotal, 4),
            decimal.Round(marginAmount, 4),
            decimal.Round(taxAmount, 4),
            decimal.Round(grandTotal, 4));
    }
}

public static class BomLineSummaryBuilder
{
    public static BOMSummaryDto Build(GlassProject project, IReadOnlyList<GlassProjectBOMLine> lines)
    {
        decimal SumForKind(GlassBOMLineKind kind) => lines.Where(l => l.Kind == kind).Sum(l => l.LineCost);
        var profile = SumForKind(GlassBOMLineKind.ProfileCut);
        var glass = SumForKind(GlassBOMLineKind.GlassPiece);
        var hardware = SumForKind(GlassBOMLineKind.HardwarePiece);
        var labor = SumForKind(GlassBOMLineKind.Labor);
        var transport = SumForKind(GlassBOMLineKind.Transport);
        var installation = SumForKind(GlassBOMLineKind.Installation);
        var currency = lines.Select(l => l.Currency).FirstOrDefault() ?? project.Currency;

        var subtotal = project.Subtotal;
        var taxAmount = project.TaxTotal;
        var grandTotal = project.GrandTotal;
        var marginAmount = grandTotal - taxAmount - subtotal;

        var dtoLines = lines
            .OrderBy(l => l.Kind)
            .ThenBy(l => l.SortOrder)
            .Select(MapLine)
            .ToList();

        return new BOMSummaryDto(
            project.TotalAreaM2, project.TotalPanels, 0m,
            profile, glass, hardware, labor,
            0m, transport, installation, 0m,
            subtotal, marginAmount, taxAmount, grandTotal,
            currency, dtoLines);
    }

    public static BOMLineDto MapLine(GlassProjectBOMLine l) => new(
        l.Id, l.Kind, l.RefId, l.Description, l.Quantity, l.Unit, l.UnitCost, l.LineCost, l.Currency,
        l.Source, l.SortOrder, l.ProductId, l.IsService, l.CutSpecJson, l.IsManual, l.UnitPriceOverride);
}

public static class BomRecomputePreservation
{
    public static IReadOnlyDictionary<string, decimal> CaptureOverrides(IEnumerable<GlassProjectBOMLine> lines)
    {
        var map = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var line in lines.Where(l => !l.IsManual && l.UnitPriceOverride.HasValue))
        {
            map[ExactKey(line)] = line.UnitPriceOverride!.Value;
            if (line.RefId.HasValue)
            {
                map.TryAdd(MaterialKey(line.Kind, line.RefId.Value), line.UnitPriceOverride.Value);
            }
        }
        return map;
    }

    public static void ReapplyOverrides(
        IEnumerable<GlassProjectBOMLine> lines,
        IReadOnlyDictionary<string, decimal> overrides)
    {
        if (overrides.Count == 0) return;
        foreach (var line in lines)
        {
            if (overrides.TryGetValue(ExactKey(line), out var price)
                || (line.RefId.HasValue && overrides.TryGetValue(MaterialKey(line.Kind, line.RefId.Value), out price)))
            {
                line.ApplyUnitPriceOverride(price);
            }
        }
    }

    public static IReadOnlyList<GlassProjectBOMLine> CloneManualLines(
        IEnumerable<GlassProjectBOMLine> lines,
        Guid projectId,
        int startSortOrder)
    {
        var sortOrder = startSortOrder;
        return lines
            .Where(l => l.IsManual)
            .OrderBy(l => l.SortOrder)
            .Select(l => new GlassProjectBOMLine(
                projectId,
                l.Kind,
                l.Description,
                l.Quantity,
                l.Unit,
                l.UnitCost,
                l.Currency,
                l.RefId,
                l.Source,
                sortOrder++,
                l.ProductId,
                l.IsService,
                l.CutSpecJson,
                isManual: true))
            .ToList();
    }

    private static string ExactKey(GlassProjectBOMLine line) =>
        $"{line.Kind}|{line.RefId}|{line.Description}";

    private static string MaterialKey(GlassBOMLineKind kind, Guid refId) =>
        $"{kind}|{refId}";
}
