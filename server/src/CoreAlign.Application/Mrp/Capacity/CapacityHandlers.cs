using MediatR;

namespace CoreAlign.Application.Mrp.Capacity;

public class GetMrpCapacityLoadHandler : IRequestHandler<GetMrpCapacityLoadQuery, MrpCapacityLoadResultDto>
{
    private readonly IMrpPlanningService _planning;
    private readonly ICapacityLoadDataLoader _loader;
    private readonly ICrpCalculator _calculator;

    public GetMrpCapacityLoadHandler(
        IMrpPlanningService planning,
        ICapacityLoadDataLoader loader,
        ICrpCalculator calculator)
    {
        _planning = planning;
        _loader = loader;
        _calculator = calculator;
    }

    public async Task<MrpCapacityLoadResultDto> Handle(GetMrpCapacityLoadQuery q, CancellationToken ct)
    {
        var asOf = q.AsOfDateUtc ?? DateTime.UtcNow;
        var plan = await _planning.RunPreviewAsync(asOf, q.BucketKind, q.HorizonDays, ct);

        var context = await _loader.BuildAsync(plan, asOf, q.BucketKind, q.HorizonDays, ct);
        var result = _calculator.Compute(context.Input);

        var workCenters = result.WorkCenters
            .Select(wc => new MrpCapacityWorkCenterDto(
                wc.WorkCenterId,
                wc.Code,
                wc.Name,
                wc.DailyCapacityMinutes,
                wc.Buckets
                    .Select(b => new MrpCapacityBucketLoadDto(
                        context.BucketStarts[b.BucketIndex],
                        b.LoadMinutes,
                        b.CapacityMinutes,
                        b.IsOverloaded))
                    .ToList()))
            .ToList();

        return new MrpCapacityLoadResultDto(
            asOf,
            q.BucketKind,
            q.HorizonDays,
            context.BucketStarts,
            workCenters,
            result.UnroutedProductionOrderCount);
    }
}
