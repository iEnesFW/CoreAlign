using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Domain.Entities.GlassEnclosure;

namespace CoreAlign.Application.GlassEnclosure.Mapping;

public static class ProjectTemplateMappers
{
    public static ProjectTemplateRunPresetDto ToDto(ProjectTemplateRunPreset preset) => new(
        preset.Id, preset.OrderIndex, preset.LabelKey, preset.LengthMm, preset.HeightMm,
        preset.OriginX, preset.OriginY, preset.RotationDeg,
        preset.DefaultPanelCount, preset.DefaultPanelWidthMm, preset.DefaultOpeningType,
        preset.HasTopDrip, preset.HasBottomThreshold,
        preset.ConnectsToPreviousAsCorner, preset.CornerJointAngleDeg, preset.CornerUsesPost);

    public static ProjectTemplateSummaryDto ToSummary(ProjectTemplate template) => new(
        template.Id, template.Code, template.DisplayNameKey,
        template.Category, template.Subtype, template.GeometryMode, template.MountingTopology,
        template.DefaultConnectorKind, template.RoofPitchDeg, template.ThumbnailUrl,
        template.DescriptionKey, template.RunPresets.Count,
        template.IsSystemTemplate, template.IsActive, template.SortOrder);

    public static ProjectTemplateDetailDto ToDetail(ProjectTemplate template) => new(
        template.Id, template.Code, template.DisplayNameKey,
        template.Category, template.Subtype, template.GeometryMode, template.MountingTopology,
        template.DefaultConnectorKind, template.RoofPitchDeg, template.RidgeHeightMm, template.EaveHeightMm,
        template.ThumbnailUrl, template.DescriptionKey, template.MetadataJson,
        template.IsSystemTemplate, template.IsActive, template.SortOrder,
        template.RunPresets.OrderBy(p => p.OrderIndex).Select(ToDto).ToList());
}
