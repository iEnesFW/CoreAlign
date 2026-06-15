using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassProjectOrderLink : TenantEntity
{
    public Guid ProjectId { get; private set; }
    public Guid OrderId { get; private set; }
    public DateTime LinkedAtUtc { get; private set; } = DateTime.UtcNow;

    protected GlassProjectOrderLink() { }

    public GlassProjectOrderLink(Guid projectId, Guid orderId)
    {
        ProjectId = projectId;
        OrderId = orderId;
    }
}
