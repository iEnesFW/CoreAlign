using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Mrp;

public class MrpPegging : TenantEntity
{
    public Guid PlanRunId { get; private set; }
    public Guid ComponentProductId { get; private set; }
    public decimal RequirementQuantity { get; private set; }
    public DateTime DueDateUtc { get; private set; }
    public string SourceKind { get; private set; } = string.Empty;
    public Guid? SourceParentProductId { get; private set; }
    public Guid? SourceOrderLineId { get; private set; }

    public MrpPlanRun? PlanRun { get; private set; }

    protected MrpPegging() { }

    public MrpPegging(
        Guid componentProductId,
        decimal requirementQuantity,
        DateTime dueDateUtc,
        string sourceKind,
        Guid? sourceParentProductId,
        Guid? sourceOrderLineId)
    {
        ComponentProductId = componentProductId;
        RequirementQuantity = requirementQuantity;
        DueDateUtc = DateTime.SpecifyKind(dueDateUtc, DateTimeKind.Utc);
        SourceKind = sourceKind ?? string.Empty;
        SourceParentProductId = sourceParentProductId;
        SourceOrderLineId = sourceOrderLineId;
    }
}
