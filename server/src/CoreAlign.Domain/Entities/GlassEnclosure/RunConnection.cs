using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class RunConnection : TenantEntity
{
    public Guid ProjectId { get; private set; }
    public Guid RunAId { get; private set; }
    public Guid RunBId { get; private set; }
    public decimal JointAngleDeg { get; private set; } = 90m;
    public decimal MitreCutDeg { get; private set; } = 45m;
    public bool UsesCornerPost { get; private set; }
    public Guid? CornerProfileId { get; private set; }

    protected RunConnection() { }

    public RunConnection(
        Guid projectId,
        Guid runAId,
        Guid runBId,
        decimal jointAngleDeg,
        decimal mitreCutDeg,
        bool usesCornerPost,
        Guid? cornerProfileId = null)
    {
        ProjectId = projectId;
        RunAId = runAId;
        RunBId = runBId;
        JointAngleDeg = jointAngleDeg;
        MitreCutDeg = mitreCutDeg;
        UsesCornerPost = usesCornerPost;
        CornerProfileId = cornerProfileId;
    }

    public void Update(
        decimal jointAngleDeg,
        decimal mitreCutDeg,
        bool usesCornerPost,
        Guid? cornerProfileId)
    {
        JointAngleDeg = jointAngleDeg;
        MitreCutDeg = mitreCutDeg;
        UsesCornerPost = usesCornerPost;
        CornerProfileId = cornerProfileId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
