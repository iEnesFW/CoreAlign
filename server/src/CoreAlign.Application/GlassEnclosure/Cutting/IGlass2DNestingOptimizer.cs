namespace CoreAlign.Application.GlassEnclosure.Cutting;

public sealed record GlassPanelRequest(
    Guid PanelId,
    string Label,
    decimal WidthMm,
    decimal HeightMm,
    int Quantity,
    bool AllowRotation = true,
    PanelCutShape? Shape = null,
    decimal? NominalHeightMm = null);

public sealed record GlassSheet(
    Guid SheetId,
    decimal WidthMm,
    decimal HeightMm,
    decimal SawKerfMm = 4m,
    decimal EdgeMarginMm = 5m);

public sealed record NestingOptions(
    string Algorithm = "MaxRects",
    string Heuristic = "BestShortSideFit",
    bool MinimizeSheets = true,
    decimal AcceptableUtilization = 0.85m,
    bool GuillotineOnly = false);

public sealed record PlacedPanel(
    Guid PanelId,
    string Label,
    decimal X,
    decimal Y,
    decimal WidthMm,
    decimal HeightMm,
    bool Rotated,
    PanelCutShape? Shape = null,
    decimal? NominalHeightMm = null);

public sealed record PlacedSheet(
    Guid SheetId,
    int SheetIndex,
    decimal SheetWidthMm,
    decimal SheetHeightMm,
    IReadOnlyList<PlacedPanel> Panels,
    decimal UsedAreaMm2,
    decimal WasteAreaMm2,
    decimal UtilizationPercent);

public sealed record UnplacedPanel(
    Guid PanelId,
    string Label,
    decimal WidthMm,
    decimal HeightMm,
    string Reason);

public sealed record Glass2DNestingResult(
    string Algorithm,
    string Heuristic,
    IReadOnlyList<PlacedSheet> Sheets,
    decimal TotalUsedAreaMm2,
    decimal TotalWasteAreaMm2,
    decimal TotalUtilizationPercent,
    int SheetsUsed,
    IReadOnlyList<UnplacedPanel> UnplacedPanels);

public interface IGlass2DNestingOptimizer
{
    Task<Glass2DNestingResult> OptimizeAsync(
        IReadOnlyList<GlassPanelRequest> panels,
        IReadOnlyList<GlassSheet> stockSheets,
        NestingOptions options,
        CancellationToken ct);
}
