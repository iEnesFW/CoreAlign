namespace CoreAlign.Application.Mrp.Planning;

public interface IMrpChangeImpactAnalyzer
{
    ChangeImpactResult Trace(MrpPlanResult plan, Guid sourceOrderLineId);
}
