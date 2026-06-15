using CoreAlign.Domain.Entities.Reporting;

namespace CoreAlign.Application.BI.DataSources;

public interface IBIDataSourceAggregator
{
    BIDataSource Source { get; }
    Task<BIResultDto> ExecuteAsync(BIQueryConfigDto config, CancellationToken cancellationToken);
}

public static class BIBucket
{
    public static string Month(DateTime utc) => utc.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
    public static string Day(DateTime utc) => utc.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    public static string Week(DateTime utc)
    {
        var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
        var week = cal.GetWeekOfYear(utc, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return $"{utc.Year}-W{week:00}";
    }
}
