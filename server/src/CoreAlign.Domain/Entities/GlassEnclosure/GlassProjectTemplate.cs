using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassProjectTemplate : TenantEntity, IHasConcurrencyToken
{
    public string Name { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public string PayloadJson { get; private set; } = "{}";
    public int WallCount { get; private set; }
    public int SlabCount { get; private set; }
    public int RunCount { get; private set; }
    public long ConcurrencyToken { get; private set; }

    protected GlassProjectTemplate() { }

    public GlassProjectTemplate(
        string name,
        Guid createdByUserId,
        string payloadJson,
        int wallCount,
        int slabCount,
        int runCount)
    {
        Name = (name ?? string.Empty).Trim();
        CreatedByUserId = createdByUserId;
        PayloadJson = payloadJson;
        WallCount = wallCount;
        SlabCount = slabCount;
        RunCount = runCount;
    }

    public void BumpConcurrencyToken() => ConcurrencyToken++;
}
