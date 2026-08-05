using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.DTOs;

public record BOMLineDto(
    Guid Id,
    GlassBOMLineKind Kind,
    Guid? RefId,
    string Description,
    decimal Quantity,
    string Unit,
    decimal UnitCost,
    decimal LineCost,
    string Currency,
    string? Source,
    int SortOrder,
    Guid? ProductId,
    bool IsService,
    string? CutSpecJson,
    bool IsManual = false,
    decimal? UnitPriceOverride = null);

public record AddManualBomLineDto(
    string Description,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    GlassBOMLineKind? Kind);

public record PushBomLinePriceResultDto(
    Guid LineId,
    Guid CatalogItemId,
    GlassBOMLineKind Kind,
    decimal PushedUnitPrice,
    decimal NewCatalogPrice,
    string Currency);

public record BOMSummaryDto(
    decimal TotalAreaM2,
    int TotalPanels,
    decimal TotalWeightKg,
    decimal ProfileCost,
    decimal GlassCost,
    decimal HardwareCost,
    decimal LaborCost,
    decimal WasteCost,
    decimal TransportCost,
    decimal ScaffoldingCost,
    decimal CraneCost,
    decimal Subtotal,
    decimal MarginAmount,
    decimal TaxAmount,
    decimal GrandTotal,
    string Currency,
    IReadOnlyList<BOMLineDto> Lines,
    bool HasStockShortage = false,
    IReadOnlyList<BomShortageDto>? Shortages = null);

/// <param name="PieceIndex">Which spliced piece of the cut this is. 1 for an unspliced cut.</param>
/// <param name="PieceCount">How many bar-length pieces the cut was spliced into. 1 means no joint.</param>
public record CuttingCut1DDto(
    string Label,
    int LengthMm,
    int OffsetMm,
    int PieceIndex = 1,
    int PieceCount = 1);

public record CuttingPattern1DDto(
    int BarIndex,
    int StockBarLengthMm,
    IReadOnlyList<CuttingCut1DDto> Cuts,
    int WasteMm,
    int OffcutMm);

public record CuttingResult1DDto(
    int StockBarLengthMm,
    int KerfMm,
    int TotalBars,
    int TotalCuts,
    long TotalUsedMm,
    long TotalWasteMm,
    decimal UtilizationPercent,
    IReadOnlyList<CuttingPattern1DDto> Patterns);

public record PanelCutShapeDto(
    string? TopShape,
    decimal NominalHeightMm,
    decimal? TopRightHeightMm,
    decimal? ArchRiseMm,
    decimal? CornerRadiusTlMm,
    decimal? CornerRadiusTrMm,
    decimal? CornerRadiusBrMm,
    decimal? CornerRadiusBlMm,
    decimal NetAreaMm2,
    string? ShapeKind = null,
    string? ShapePointsJson = null);

public record CuttingPlacement2DDto(
    string Label,
    int X,
    int Y,
    int WidthMm,
    int HeightMm,
    bool Rotated,
    PanelCutShapeDto? Shape = null);

public record CuttingSheet2DDto(
    int SheetIndex,
    int WidthMm,
    int HeightMm,
    IReadOnlyList<CuttingPlacement2DDto> Placements,
    long WasteMm2)
{
    public string? GroupKey { get; init; }
}

public record CuttingGroup2DDto(
    string? GroupKey,
    int TotalSheets,
    long TotalUsedMm2,
    long TotalWasteMm2,
    decimal UtilizationPercent);

public record CuttingResult2DDto(
    int SheetWidthMm,
    int SheetHeightMm,
    int KerfMm,
    bool GuillotineOnly,
    int TotalSheets,
    long TotalUsedMm2,
    long TotalWasteMm2,
    decimal UtilizationPercent,
    IReadOnlyList<CuttingSheet2DDto> Sheets,
    IReadOnlyList<string> Unplaced)
{
    public IReadOnlyList<CuttingGroup2DDto> Groups { get; init; } = Array.Empty<CuttingGroup2DDto>();
}

public record CuttingReportDto(
    Guid ProjectId,
    DateTime GeneratedAtUtc,
    CuttingResult1DDto Profile1D,
    CuttingResult2DDto Glass2D);

public record Glass2DPlacedPanelDto(
    Guid PanelId,
    string Label,
    decimal X,
    decimal Y,
    decimal WidthMm,
    decimal HeightMm,
    bool Rotated,
    PanelCutShapeDto? Shape = null);

public record Glass2DPlacedSheetDto(
    Guid SheetId,
    int SheetIndex,
    decimal SheetWidthMm,
    decimal SheetHeightMm,
    IReadOnlyList<Glass2DPlacedPanelDto> Panels,
    decimal UsedAreaMm2,
    decimal WasteAreaMm2,
    decimal UtilizationPercent,
    string GlassLabel = "");

public record Glass2DUnplacedPanelDto(
    Guid PanelId,
    string Label,
    decimal WidthMm,
    decimal HeightMm,
    string Reason);

public record Glass2DNestingReportDto(
    Guid ProjectId,
    DateTime GeneratedAtUtc,
    string Algorithm,
    string Heuristic,
    int SheetsUsed,
    decimal TotalUsedAreaMm2,
    decimal TotalWasteAreaMm2,
    decimal TotalUtilizationPercent,
    IReadOnlyList<Glass2DPlacedSheetDto> Sheets,
    IReadOnlyList<Glass2DUnplacedPanelDto> UnplacedPanels);

public record WindLoadPanelDto(
    Guid RunId,
    Guid PanelId,
    decimal AppliedPressurePa,
    int CurrentThicknessMm,
    int RequiredMinThicknessMm,
    bool IsSufficient,
    decimal ShortSpanMm,
    decimal AspectRatio,
    decimal MaxBendingStressMPa,
    decimal DesignStrengthMPa,
    decimal StressUtilisation,
    decimal MaxDeflectionMm,
    decimal DeflectionLimitMm,
    decimal DeflectionUtilisation,
    string GoverningLimit);

/// <param name="BasicWindSpeedMs">v_b,0 from the wind map.</param>
/// <param name="PeakVelocityPressurePa">q_p(z), after terrain roughness and turbulence.</param>
/// <param name="AppliedPressurePa">Governing NET pressure on the glass: q_p x (c_pe - c_pi).</param>
public record WindLoadDto(
    decimal BasePressurePa,
    decimal HeightFactor,
    decimal AppliedPressurePa,
    IReadOnlyList<WindLoadPanelDto> Panels,
    decimal BasicWindSpeedMs,
    decimal DesignWindSpeedMs,
    decimal ReferenceHeightM,
    decimal RoughnessFactor,
    decimal MeanWindSpeedMs,
    decimal TurbulenceIntensity,
    decimal PeakVelocityPressurePa,
    decimal ExternalPressureCoefficient,
    decimal InternalPressureCoefficient,
    string TerrainCategory,
    string StandardReference);

public record ThermalAcousticDto(
    decimal TotalAreaM2,
    decimal WeightedUValue,
    decimal WeightedSoundDb,
    decimal EstimatedWinterHeatLossKwh,
    decimal EstimatedDbReductionVsOpen);

public record TechnicalSummaryDto(
    Guid ProjectId,
    WindLoadDto? WindLoad,
    ThermalAcousticDto Thermal,
    int PanelCount,
    int RunCount,
    decimal TotalAreaM2,
    decimal TotalWeightKg);

public record BomShortageDto(
    Guid BomLineId,
    Guid ProductId,
    string ProductSku,
    decimal RequiredQty,
    decimal AvailableQty,
    decimal ShortageQty,
    int SubstituteCount);
