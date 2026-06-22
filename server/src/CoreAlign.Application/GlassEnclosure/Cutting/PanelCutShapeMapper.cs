using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Domain.Entities.GlassEnclosure;

namespace CoreAlign.Application.GlassEnclosure.Cutting;

public static class PanelCutShapeMapper
{
    public static PanelCutShape? FromPanel(GlassProjectPanel panel) => PanelCutShape.From(
        panel.TopShape,
        panel.TopRightHeightMm,
        panel.ArchRiseMm,
        panel.CornerRadiusTlMm,
        panel.CornerRadiusTrMm,
        panel.CornerRadiusBrMm,
        panel.CornerRadiusBlMm,
        panel.ShapeKind,
        panel.ShapePointsJson);

    public static PanelCutShapeDto? ToDto(
        PanelCutShape? shape,
        decimal? nominalHeightMm,
        decimal placedWidthMm,
        decimal placedHeightMm,
        bool rotated)
    {
        if (shape is null || nominalHeightMm is null) return null;

        var originalWidth = rotated ? placedHeightMm : placedWidthMm;
        var nominal = nominalHeightMm.Value;
        return new PanelCutShapeDto(
            shape.TopShape,
            nominal,
            shape.TopRightHeightMm,
            shape.ArchRiseMm,
            shape.CornerRadiusTlMm,
            shape.CornerRadiusTrMm,
            shape.CornerRadiusBrMm,
            shape.CornerRadiusBlMm,
            decimal.Round(PanelCutGeometry.NetAreaMm2(originalWidth, nominal, shape), 2),
            shape.ShapeKind,
            shape.PointsJson);
    }
}
