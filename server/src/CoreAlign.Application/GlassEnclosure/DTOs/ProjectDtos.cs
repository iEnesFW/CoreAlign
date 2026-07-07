using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.DTOs;

public record PanelCornerRadiiDto(int? Tl, int? Tr, int? Br, int? Bl);

public record PanelHardwareDto(Guid HardwareItemId, decimal Quantity);

public record GlassProjectPanelDto(
    Guid Id,
    Guid RunId,
    int PanelIndex,
    int WidthMm,
    GlassOpeningType OpeningType,
    Guid GlassTypeId,
    bool HasHandle,
    bool HasLock,
    bool HasBrushSeal,
    string? Notes,
    int? HeightMm = null,
    string? TopShape = null,
    int? TopRightHeightMm = null,
    int? ArchRiseMm = null,
    PanelCornerRadiiDto? CornerRadiiMm = null,
    string? ShapeKind = null,
    string? ShapePointsJson = null,
    IReadOnlyList<PanelHardwareDto>? Hardware = null);

public record GlassProjectRunDto(
    Guid Id,
    Guid ProjectId,
    int OrderIndex,
    string Label,
    int LengthMm,
    int HeightMm,
    decimal OriginX,
    decimal OriginY,
    decimal RotationDeg,
    Guid ProfileSystemId,
    Guid? ColorId,
    bool HasTopDrip,
    bool HasBottomThreshold,
    string? Notes,
    int? GeomZ,
    decimal? GeomTiltDeg,
    int? GeomArcRadiusMm,
    decimal? GeomArcSweepDeg,
    bool ArcGlassBent,
    IReadOnlyList<GlassProjectPanelDto> Panels);

public record RunConnectionDto(
    Guid Id,
    Guid ProjectId,
    Guid RunAId,
    Guid RunBId,
    decimal JointAngleDeg,
    decimal MitreCutDeg,
    bool UsesCornerPost,
    Guid? CornerProfileId);

public record GlassProjectDto(
    Guid Id,
    string Code,
    Guid CustomerId,
    string? CustomerName,
    string ProjectName,
    string? SiteAddressLine1,
    string? SiteAddressLine2,
    string? SiteCity,
    string? SiteDistrict,
    string? SitePostalCode,
    string? SiteCountryCode,
    GlassProjectStatus Status,
    Guid CreatedByUserId,
    Guid? AssignedDesignerUserId,
    Guid? AssignedSalespersonUserId,
    int? FloorNumber,
    decimal? BuildingHeightM,
    Guid? WindZoneId,
    Guid? ClimateZoneId,
    string? FireSafetyClass,
    bool ScaffoldingRequired,
    bool CraneRequired,
    decimal TotalAreaM2,
    int TotalPanels,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string Currency,
    decimal FxRateToBase,
    DateTime? FxRateLockedAtUtc,
    decimal? WindLoadPaCalculated,
    decimal? WeightedUValue,
    decimal? WeightedSoundDb,
    DateTime? ValidUntilDate,
    int CurrentSceneVersion,
    string? Notes,
    bool IsBomStale,
    string? BomStaleReason,
    DateTime? StaleSinceUtc,
    EnclosureCategory EnclosureCategory,
    EnclosureSubtype EnclosureSubtype,
    GeometryMode GeometryMode,
    MountingTopology MountingTopology,
    decimal? RoofPitchDeg,
    int? RidgeHeightMm,
    int? EaveHeightMm,
    string? PolygonVerticesJson,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<GlassProjectRunDto> Runs,
    IReadOnlyList<RunConnectionDto> Connections);

public record GlassProjectListItemDto(
    Guid Id,
    string Code,
    string ProjectName,
    Guid CustomerId,
    string? CustomerName,
    GlassProjectStatus Status,
    decimal GrandTotal,
    string Currency,
    int TotalPanels,
    decimal TotalAreaM2,
    DateTime UpdatedAtUtc);

public record CreateGlassProjectDto(
    Guid CustomerId,
    string ProjectName,
    string? SiteAddressLine1,
    string? SiteAddressLine2,
    string? SiteCity,
    string? SiteDistrict,
    string? SitePostalCode,
    string? SiteCountryCode,
    int? FloorNumber,
    decimal? BuildingHeightM,
    string Currency,
    DateTime? ValidUntilDate,
    string? Notes);

public record UpdateGlassProjectHeaderDto(
    string ProjectName,
    string? SiteAddressLine1,
    string? SiteAddressLine2,
    string? SiteCity,
    string? SiteDistrict,
    string? SitePostalCode,
    string? SiteCountryCode,
    int? FloorNumber,
    decimal? BuildingHeightM,
    Guid? WindZoneId,
    Guid? ClimateZoneId,
    string? FireSafetyClass,
    bool ScaffoldingRequired,
    bool CraneRequired,
    DateTime? ValidUntilDate,
    string? Notes);

public record AssignProjectTeamDto(Guid? DesignerUserId, Guid? SalespersonUserId);

public record ConfigureEnclosureDto(
    EnclosureCategory Category,
    EnclosureSubtype Subtype,
    GeometryMode? GeometryMode,
    MountingTopology? MountingTopology,
    decimal? RoofPitchDeg,
    int? RidgeHeightMm,
    int? EaveHeightMm,
    string? CurtainWallCassetteSpecJson,
    string? PolygonVerticesJson,
    string? MetadataJson);

public record TransitionProjectStatusDto(GlassProjectStatus TargetStatus);

public record AddRunDto(
    int LengthMm,
    int HeightMm,
    Guid? ProfileSystemId,
    decimal OriginX,
    decimal OriginY,
    decimal RotationDeg,
    string? Label,
    Guid? ColorId,
    bool HasTopDrip,
    bool HasBottomThreshold,
    string? Notes,
    int? PanelCount = null,
    int? GeomZ = null,
    decimal? GeomTiltDeg = null,
    int? GeomArcRadiusMm = null,
    decimal? GeomArcSweepDeg = null,
    bool? ArcGlassBent = null);

public record UpdateRunDto(
    int LengthMm,
    int HeightMm,
    decimal OriginX,
    decimal OriginY,
    decimal RotationDeg,
    string Label,
    Guid ProfileSystemId,
    Guid? ColorId,
    bool HasTopDrip,
    bool HasBottomThreshold,
    string? Notes,
    int? GeomZ = null,
    decimal? GeomTiltDeg = null,
    int? GeomArcRadiusMm = null,
    decimal? GeomArcSweepDeg = null,
    bool? ArcGlassBent = null);

public record AddPanelDto(
    int WidthMm,
    GlassOpeningType OpeningType,
    Guid GlassTypeId,
    bool HasHandle,
    bool HasLock,
    bool HasBrushSeal,
    string? Notes,
    int? HeightMm = null,
    string? TopShape = null,
    int? TopRightHeightMm = null,
    int? ArchRiseMm = null,
    PanelCornerRadiiDto? CornerRadiiMm = null,
    string? ShapeKind = null,
    string? ShapePointsJson = null,
    IReadOnlyList<PanelHardwareDto>? Hardware = null);

public record UpdatePanelDto(
    int WidthMm,
    GlassOpeningType OpeningType,
    Guid GlassTypeId,
    bool HasHandle,
    bool HasLock,
    bool HasBrushSeal,
    string? Notes,
    int? HeightMm = null,
    string? TopShape = null,
    int? TopRightHeightMm = null,
    int? ArchRiseMm = null,
    PanelCornerRadiiDto? CornerRadiiMm = null,
    string? ShapeKind = null,
    string? ShapePointsJson = null,
    IReadOnlyList<PanelHardwareDto>? Hardware = null);

public record BulkRebalancePanelsDto(
    int PanelCount,
    GlassOpeningType DefaultOpeningType,
    Guid DefaultGlassTypeId);

public record AddRunConnectionDto(
    Guid RunAId,
    Guid RunBId,
    decimal JointAngleDeg,
    decimal MitreCutDeg,
    bool UsesCornerPost,
    Guid? CornerProfileId);

public record UpdateRunConnectionDto(
    decimal JointAngleDeg,
    decimal MitreCutDeg,
    bool UsesCornerPost,
    Guid? CornerProfileId);

public record SaveSceneDto(
    string SceneJson,
    string? ThumbnailDataUrl,
    string? CameraStateJson,
    string? Label);

public record SceneVersionDto(
    Guid Id,
    int Version,
    string? Label,
    string? ThumbnailUrl,
    Guid SavedByUserId,
    DateTime SavedAtUtc,
    bool IsCustomerApproved);

public record SceneLatestDto(
    int Version,
    string SceneJson,
    string? CameraStateJson,
    string? ThumbnailUrl,
    DateTime SavedAtUtc);

public record GlassValidationFindingDto(
    GlassValidationSeverity Severity,
    string Code,
    string MessageKey,
    string? MessageArgs,
    Guid? AffectedRunId,
    Guid? AffectedPanelId);

public record GlassProjectValidationResultDto(IReadOnlyList<GlassValidationFindingDto> Findings);
