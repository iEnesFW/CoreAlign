using CoreAlign.Application.GlassEnclosure.Cutting;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.GlassEnclosure;

/// <summary>
/// The BOM priced the SILHOUETTE while the technical summary and the scene validator multiplied
/// the raw blank rectangle by the run height — so weight, wind load and the max-area guard all
/// disagreed with the money the customer was quoted. One writer now serves all three.
/// </summary>
public class GlassPanelAreaMathTests
{
    private static GlassProjectRun Run(int heightMm = 2000) =>
        new(Guid.NewGuid(), 0, "R1", 3000, heightMm, Guid.NewGuid());

    private static GlassProjectPanel Panel(GlassProjectRun run, int widthMm = 1000) =>
        new(run.Id, 0, widthMm, GlassOpeningType.Fixed, Guid.NewGuid());

    [Fact]
    public void A_plain_panel_is_width_times_the_run_height()
    {
        var run = Run();
        var panel = Panel(run);

        GlassPanelAreaMath.NetAreaM2(run, panel).Should().Be(2m);
    }

    [Fact]
    public void A_panel_height_override_wins_over_the_run_height()
    {
        var run = Run(2000);
        var panel = Panel(run);
        panel.UpdateShape(1500, null, null, null, null, null, null, null);

        // The old formula ignored the override entirely and still reported 2.00 m².
        GlassPanelAreaMath.NetAreaM2(run, panel).Should().Be(1.5m);
    }

    [Fact]
    public void A_raked_top_bills_the_silhouette_not_the_blank_rectangle()
    {
        var run = Run(2000);
        var panel = Panel(run);
        // Left edge 2000, right edge 1000 → the cut piece is a trapezoid averaging 1500 mm tall.
        panel.UpdateShape(2000, "raked", 1000, null, null, null, null, null);

        var area = GlassPanelAreaMath.NetAreaM2(run, panel);

        area.Should().BeLessThan(2m);
        area.Should().BeApproximately(1.5m, 0.001m);
    }

    [Fact]
    public void The_summary_and_the_BOM_now_read_the_same_number()
    {
        var run = Run(2000);
        var panel = Panel(run);
        panel.UpdateShape(2000, "raked", 1000, null, null, null, null, null);

        var shared = GlassPanelAreaMath.NetAreaM2(run, panel);
        var bomStyle = PanelCutGeometry.NetAreaMm2(
            panel.WidthMm,
            panel.HeightMm ?? run.HeightMm,
            PanelCutShapeMapper.FromPanel(panel)) / 1_000_000m;

        shared.Should().Be(bomStyle);
    }
}
