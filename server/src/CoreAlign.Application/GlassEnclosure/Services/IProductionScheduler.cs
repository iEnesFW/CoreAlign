using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.GlassEnclosure.Services;

public record ScheduleProposal(DateTime ScheduledStartUtc, DateTime ScheduledEndUtc);

public interface IProductionScheduler
{
    Task<ScheduleProposal> AllocateAsync(
        decimal workloadM2,
        DateTime requestedStartUtc,
        CancellationToken cancellationToken = default);
}

public class ProductionScheduler : IProductionScheduler
{
    private readonly IGlassWorkOrderRepository _workOrderRepo;
    private readonly IGlassEnclosureSettingsRepository _settingsRepo;

    public ProductionScheduler(
        IGlassWorkOrderRepository workOrderRepo,
        IGlassEnclosureSettingsRepository settingsRepo)
    {
        _workOrderRepo = workOrderRepo;
        _settingsRepo = settingsRepo;
    }

    public async Task<ScheduleProposal> AllocateAsync(
        decimal workloadM2,
        DateTime requestedStartUtc,
        CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepo.GetOrCreateForCurrentTenantAsync(cancellationToken);
        var dailyCapacity = Math.Max(1m, settings.WorkshopDailyCapacityM2);
        var daysNeeded = (int)Math.Ceiling(workloadM2 / dailyCapacity);
        daysNeeded = Math.Max(1, daysNeeded);

        var attempt = NormalizeToWorkday(requestedStartUtc);
        for (var safety = 0; safety < 90; safety++)
        {
            var (start, end) = SpanWorkdays(attempt, daysNeeded);
            var occupied = await _workOrderRepo.GetWorkloadM2ForDayAsync(start, cancellationToken);
            if (occupied + workloadM2 <= dailyCapacity * daysNeeded)
            {
                return new ScheduleProposal(start, end);
            }
            attempt = attempt.AddDays(1);
        }

        throw new GlassWorkOrderScheduleConflictException(attempt);
    }

    private static DateTime NormalizeToWorkday(DateTime utc)
    {
        var result = new DateTime(utc.Year, utc.Month, utc.Day, 8, 0, 0, DateTimeKind.Utc);
        while (result.DayOfWeek == DayOfWeek.Saturday || result.DayOfWeek == DayOfWeek.Sunday)
        {
            result = result.AddDays(1);
        }
        return result;
    }

    private static (DateTime Start, DateTime End) SpanWorkdays(DateTime start, int days)
    {
        var current = start;
        var workdaysCounted = 0;
        while (workdaysCounted < days)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
            {
                workdaysCounted += 1;
                if (workdaysCounted == days)
                {
                    var endOfDay = new DateTime(current.Year, current.Month, current.Day, 17, 0, 0, DateTimeKind.Utc);
                    return (start, endOfDay);
                }
            }
            current = current.AddDays(1);
        }
        return (start, current);
    }
}
