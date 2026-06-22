using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Domain.Entities.GlassEnclosure;

namespace CoreAlign.Application.GlassEnclosure.Mapping;

public static class ProjectMappers
{
    public static GlassProjectPanelDto ToDto(GlassProjectPanel panel) => new(
        panel.Id, panel.RunId, panel.PanelIndex, panel.WidthMm, panel.OpeningType,
        panel.GlassTypeId, panel.HasHandle, panel.HasLock, panel.HasBrushSeal, panel.Notes,
        panel.HeightMm, panel.TopShape, panel.TopRightHeightMm, panel.ArchRiseMm,
        ToCornerRadii(panel), panel.ShapeKind, panel.ShapePointsJson);

    private static PanelCornerRadiiDto? ToCornerRadii(GlassProjectPanel panel) =>
        panel.CornerRadiusTlMm is null && panel.CornerRadiusTrMm is null
        && panel.CornerRadiusBrMm is null && panel.CornerRadiusBlMm is null
            ? null
            : new PanelCornerRadiiDto(
                panel.CornerRadiusTlMm, panel.CornerRadiusTrMm,
                panel.CornerRadiusBrMm, panel.CornerRadiusBlMm);

    public static GlassProjectRunDto ToDto(GlassProjectRun run) => new(
        run.Id, run.ProjectId, run.OrderIndex, run.Label, run.LengthMm, run.HeightMm,
        run.OriginX, run.OriginY, run.RotationDeg,
        run.ProfileSystemId, run.ColorId, run.HasTopDrip, run.HasBottomThreshold, run.Notes,
        run.GeomZ, run.GeomTiltDeg, run.GeomArcRadiusMm, run.GeomArcSweepDeg, run.ArcGlassBent,
        run.Panels.OrderBy(p => p.PanelIndex).Select(ToDto).ToList());

    public static RunConnectionDto ToDto(RunConnection conn) => new(
        conn.Id, conn.ProjectId, conn.RunAId, conn.RunBId,
        conn.JointAngleDeg, conn.MitreCutDeg, conn.UsesCornerPost, conn.CornerProfileId);

    public static GlassProjectDto ToDto(GlassProject project, string? customerName = null) => new(
        project.Id, project.Code, project.CustomerId, customerName, project.ProjectName,
        project.SiteAddressLine1, project.SiteAddressLine2, project.SiteCity, project.SiteDistrict,
        project.SitePostalCode, project.SiteCountryCode,
        project.Status, project.CreatedByUserId,
        project.AssignedDesignerUserId, project.AssignedSalespersonUserId,
        project.FloorNumber, project.BuildingHeightM,
        project.WindZoneId, project.ClimateZoneId, project.FireSafetyClass,
        project.ScaffoldingRequired, project.CraneRequired,
        project.TotalAreaM2, project.TotalPanels,
        project.Subtotal, project.DiscountTotal, project.TaxTotal, project.GrandTotal,
        project.Currency, project.FxRateToBase, project.FxRateLockedAtUtc,
        project.WindLoadPaCalculated, project.WeightedUValue, project.WeightedSoundDb,
        project.ValidUntilDate, project.CurrentSceneVersion, project.Notes,
        project.IsBomStale, project.BomStaleReason, project.StaleSinceUtc,
        project.EnclosureCategory, project.EnclosureSubtype, project.GeometryMode, project.MountingTopology,
        project.RoofPitchDeg, project.RidgeHeightMm, project.EaveHeightMm,
        project.PolygonVerticesJson,
        project.CreatedAtUtc, project.UpdatedAtUtc,
        project.Runs.OrderBy(r => r.OrderIndex).Select(ToDto).ToList(),
        project.Connections.Select(ToDto).ToList());

    public static GlassProjectListItemDto ToDto(Domain.Interfaces.GlassProjectListItem item) => new(
        item.Id, item.Code, item.ProjectName, item.CustomerId, item.CustomerName,
        item.Status, item.GrandTotal, item.Currency, item.TotalPanels, item.TotalAreaM2, item.UpdatedAtUtc);
}
