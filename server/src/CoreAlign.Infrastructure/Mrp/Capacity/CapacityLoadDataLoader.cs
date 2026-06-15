using CoreAlign.Application.Mrp.Capacity;
using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Mrp.Planning;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Mrp.Capacity;

public sealed class CapacityLoadDataLoader : ICapacityLoadDataLoader
{
    private readonly CoreAlignDbContext _db;

    public CapacityLoadDataLoader(CoreAlignDbContext db) => _db = db;

    public async Task<CapacityLoadContext> BuildAsync(
        MrpPlanResult plan,
        DateTime asOfUtc,
        MrpBucketKind bucketKind,
        int horizonDays,
        CancellationToken cancellationToken = default)
    {
        var calendar = new BucketCalendar(asOfUtc, bucketKind, horizonDays);
        var daysPerBucket = bucketKind == MrpBucketKind.Week ? 7 : 1;

        var productionOrders = plan.Items
            .SelectMany(item => item.ProductionOrders)
            .ToList();

        var workCenters = await _db.Set<WorkCenter>().AsNoTracking()
            .Where(w => w.IsActive)
            .Select(w => new CrpWorkCenterSnapshot(w.Id, w.Code, w.Name, w.DailyCapacityMinutes))
            .ToListAsync(cancellationToken);

        if (productionOrders.Count == 0)
        {
            return new CapacityLoadContext(
                new CrpInput(Array.Empty<CrpProductionLoad>(), workCenters, calendar.Count, daysPerBucket),
                calendar.Starts);
        }

        var productIds = productionOrders.Select(o => o.ProductId).Distinct().ToList();

        var routing = await _db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.WorkCenterId, p.RunTimeMinutesPerUnit })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var loads = productionOrders
            .Select(order =>
            {
                routing.TryGetValue(order.ProductId, out var route);
                return new CrpProductionLoad(
                    order.ProductId,
                    route?.WorkCenterId,
                    route?.RunTimeMinutesPerUnit ?? 0m,
                    order.Quantity,
                    calendar.IndexFor(order.DueDateUtc));
            })
            .ToList();

        return new CapacityLoadContext(
            new CrpInput(loads, workCenters, calendar.Count, daysPerBucket),
            calendar.Starts);
    }
}
