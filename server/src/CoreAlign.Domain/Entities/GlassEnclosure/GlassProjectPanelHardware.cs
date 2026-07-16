using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassProjectPanelHardware : TenantEntity, IHasConcurrencyToken
{
    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

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
