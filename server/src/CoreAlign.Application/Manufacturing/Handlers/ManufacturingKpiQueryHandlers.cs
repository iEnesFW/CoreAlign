using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Manufacturing.Handlers;

public class GetManufacturingKpiSummaryQueryHandler : IRequestHandler<Queries.GetManufacturingKpiSummaryQuery, Queries.ManufacturingKpiSummaryDto>
{
    private readonly IProductionJobRepository _jobs;
    private readonly IWorkCenterRepository _workCenters;
    private readonly ITenantContext _tenant;

    public GetManufacturingKpiSummaryQueryHandler(
        IProductionJobRepository jobs,
        IWorkCenterRepository workCenters,
        ITenantContext tenant)
    {
        _jobs = jobs;
        _workCenters = workCenters;
        _tenant = tenant;
    }

    public async Task<Queries.ManufacturingKpiSummaryDto> Handle(Queries.GetManufacturingKpiSummaryQuery request, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        
        var completedJobs = await _jobs.GetCompletedJobsInRangeAsync(tenantId, request.StartDateUtc, request.EndDateUtc, ct);

        var totalJobsCompleted = completedJobs.Count;
        var totalGood = 0m;
        var totalScrap = 0m;

        var stepStats = new Dictionary<Guid, (decimal Good, decimal Scrap, decimal RunMins)>();

        foreach (var job in completedJobs)
        {
            foreach (var step in job.Steps)
            {
                if (step.WorkCenterId.HasValue)
                {
                    var wcId = step.WorkCenterId.Value;
                    if (!stepStats.TryGetValue(wcId, out var current))
                    {
                        current = (0m, 0m, 0m);
                    }
                    stepStats[wcId] = (
                        current.Good + step.GoodQuantity,
                        current.Scrap + step.ScrappedQuantity,
                        current.RunMins + (step.ActualRunMinutes ?? 0m)
                    );

                    // For the overall we only count the final step's good quantity maybe?
                    // To keep it simple, we'll aggregate all steps or just use the job's completed qty (we don't store it on job directly, but we can look at the last step).
                }
            }

            var lastStep = job.Steps.OrderBy(s => s.StepNumber).LastOrDefault();
            if (lastStep != null)
            {
                totalGood += lastStep.GoodQuantity;
            }
            totalScrap += job.Steps.Sum(s => s.ScrappedQuantity);
        }

        var overallYield = (totalGood + totalScrap) > 0 ? (totalGood / (totalGood + totalScrap)) * 100m : 0m;

        var workCenterIds = stepStats.Keys.ToArray();
        var workCenters = await _workCenters.GetByIdsAsync(workCenterIds, ct);

        var wcKpis = new List<Queries.WorkCenterKpiDto>();
        foreach (var wc in workCenters)
        {
            if (stepStats.TryGetValue(wc.Id, out var stats))
            {
                var wcYield = (stats.Good + stats.Scrap) > 0 ? (stats.Good / (stats.Good + stats.Scrap)) * 100m : 0m;
                wcKpis.Add(new Queries.WorkCenterKpiDto(
                    wc.Id,
                    wc.Name,
                    stats.Scrap,
                    stats.Good,
                    wcYield,
                    stats.RunMins
                ));
            }
        }

        return new Queries.ManufacturingKpiSummaryDto(
            totalJobsCompleted,
            totalGood,
            totalScrap,
            overallYield,
            wcKpis
        );
    }
}
