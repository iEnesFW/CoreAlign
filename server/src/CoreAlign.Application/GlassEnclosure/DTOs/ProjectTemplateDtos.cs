using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.DTOs;

public record ProjectTemplateRunPresetDto(
    Guid Id,
    int OrderIndex,
    string LabelKey,
    int LengthMm,
    int HeightMm,
    decimal OriginX,
    decimal OriginY,
    decimal RotationDeg,
    int DefaultPanelCount,
    int DefaultPanelWidthMm,
    GlassOpeningType DefaultOpeningType,
    bool HasTopDrip,
    bool HasBottomThreshold,
    bool ConnectsToPreviousAsCorner,
    decimal? CornerJointAngleDeg,
    bool CornerUsesPost);

public record ProjectTemplateSummaryDto(
    Guid Id,
    string Code,
    string DisplayNameKey,
    EnclosureCategory Category,
    EnclosureSubtype Subtype,
    GeometryMode GeometryMode,
    MountingTopology MountingTopology,
    ConnectorKind DefaultConnectorKind,
    decimal? RoofPitchDeg,
    string? ThumbnailUrl,
    string? DescriptionKey,
    int RunPresetCount,
    bool IsSystemTemplate,
    bool IsActive,
    int SortOrder);

public record ProjectTemplateDetailDto(
    Guid Id,
    string Code,
    string DisplayNameKey,
    EnclosureCategory Category,
    EnclosureSubtype Subtype,
    GeometryMode GeometryMode,
    MountingTopology MountingTopology,
    ConnectorKind DefaultConnectorKind,
    decimal? RoofPitchDeg,
    int? RidgeHeightMm,
    int? EaveHeightMm,
    string? ThumbnailUrl,
    string? DescriptionKey,
    string? MetadataJson,
    bool IsSystemTemplate,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<ProjectTemplateRunPresetDto> RunPresets);

public record CreateProjectFromTemplateDto(
    Guid TemplateId,
    Guid CustomerId,
    string ProjectName,
    string? Currency);
