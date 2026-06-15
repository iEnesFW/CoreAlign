using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassProjectCuttingPlan : TenantEntity
{
    public Guid ProjectId { get; private set; }
    public GlassCuttingPlanType PlanType { get; private set; } = GlassCuttingPlanType.Profile1D;
    public string PlanJson { get; private set; } = "{}";
    public decimal TotalWasteMm2 { get; private set; }
    public decimal TotalWasteMm { get; private set; }
    public decimal UtilizationPercent { get; private set; }
    public DateTime GeneratedAtUtc { get; private set; } = DateTime.UtcNow;
    public Guid GeneratedByUserId { get; private set; }

    protected GlassProjectCuttingPlan() { }

    public GlassProjectCuttingPlan(
        Guid projectId,
        GlassCuttingPlanType planType,
        string planJson,
        decimal totalWasteMm2,
        decimal totalWasteMm,
        decimal utilizationPercent,
        Guid generatedByUserId)
    {
        ProjectId = projectId;
        PlanType = planType;
        PlanJson = planJson;
        TotalWasteMm2 = totalWasteMm2;
        TotalWasteMm = totalWasteMm;
        UtilizationPercent = utilizationPercent;
        GeneratedByUserId = generatedByUserId;
    }
}
