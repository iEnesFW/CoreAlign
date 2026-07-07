using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

// A catalog HardwareItem placed on a glass panel. Structural (real FK to HardwareItem) so the item
// flows into the BOM, quote and cutting list instead of living only in the render-only scene blob.
public class GlassProjectPanelHardware : TenantEntity
{
    public Guid PanelId { get; private set; }
    public Guid HardwareItemId { get; private set; }
    public decimal Quantity { get; private set; }

    protected GlassProjectPanelHardware() { }

    public GlassProjectPanelHardware(Guid panelId, Guid hardwareItemId, decimal quantity)
    {
        PanelId = panelId;
        HardwareItemId = hardwareItemId;
        Quantity = quantity < 0m ? 0m : quantity;
    }
}
