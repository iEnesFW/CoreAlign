namespace CoreAlign.Application.Mrp.Distribution;

public interface IDistributionPlanner
{
    DistributionPlan Plan(DistributionInput input);
}
