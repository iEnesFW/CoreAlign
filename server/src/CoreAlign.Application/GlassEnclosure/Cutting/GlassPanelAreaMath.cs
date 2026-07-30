using CoreAlign.Domain.Entities.GlassEnclosure;

namespace CoreAlign.Application.GlassEnclosure.Cutting;

public static class GlassPanelAreaMath
{
    // WHY one writer: the BOM priced the SILHOUETTE (raked/arched/elliptical/polygon, panel height
    // override honoured) while the technical summary and the validator multiplied the raw blank
    // rectangle by the run height — so weight, wind load, U-value and the max-area guard all
    // disagreed with the money the customer was quoted.
    public static decimal NetAreaMm2(GlassProjectRun run, GlassProjectPanel panel) =>
        PanelCutGeometry.NetAreaMm2(
            panel.WidthMm,
            panel.HeightMm ?? run.HeightMm,
            PanelCutShapeMapper.FromPanel(panel));

    public static decimal NetAreaM2(GlassProjectRun run, GlassProjectPanel panel) =>
        NetAreaMm2(run, panel) / 1_000_000m;
}
