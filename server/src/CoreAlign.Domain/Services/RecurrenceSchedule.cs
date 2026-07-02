using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Services;

public static class RecurrenceSchedule
{
    public static DateOnly ComputeNext(
        RecurrenceFrequency frequency,
        int intervalCount,
        int? anchorDayOfMonth,
        DayOfWeek? anchorDayOfWeek,
        DateOnly from)
    {
        var interval = intervalCount < 1 ? 1 : intervalCount;
        return frequency switch
        {
            RecurrenceFrequency.Weekly => SnapToDayOfWeek(from.AddDays(7 * interval), anchorDayOfWeek),
            RecurrenceFrequency.Monthly => AddMonthsClamped(from, interval, anchorDayOfMonth),
            RecurrenceFrequency.Quarterly => AddMonthsClamped(from, 3 * interval, anchorDayOfMonth),
            RecurrenceFrequency.Yearly => AddMonthsClamped(from, 12 * interval, anchorDayOfMonth),
            _ => from.AddMonths(interval)
        };
    }

    private static DateOnly SnapToDayOfWeek(DateOnly date, DayOfWeek? anchor)
    {
        if (anchor is null) return date;
        var diff = ((int)anchor.Value - (int)date.DayOfWeek + 7) % 7;
        return date.AddDays(diff);
    }

    private static DateOnly AddMonthsClamped(DateOnly from, int months, int? anchorDayOfMonth)
    {
        var target = from.AddMonths(months);
        var desiredDay = anchorDayOfMonth ?? from.Day;
        var lastDay = DateTime.DaysInMonth(target.Year, target.Month);
        var day = desiredDay < lastDay ? desiredDay : lastDay;
        return new DateOnly(target.Year, target.Month, day);
    }
}
