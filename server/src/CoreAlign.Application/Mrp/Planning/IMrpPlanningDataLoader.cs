namespace CoreAlign.Application.Mrp.Planning;

public interface IMrpPlanningDataLoader
{
    Task<MrpPlanningSnapshot> LoadAsync(DateTime asOfUtc, int horizonDays, CancellationToken cancellationToken = default);
}
