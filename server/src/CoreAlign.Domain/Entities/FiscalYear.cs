namespace CoreAlign.Domain.Entities;

/// <summary>
/// A fiscal year runs from the tenant's <c>FiscalYearStartMonth</c> for twelve months. The label is
/// the calendar year the period STARTS in, which is the Turkish convention: a year beginning in
/// October 2026 is "2026" even though most of it falls in 2027.
/// </summary>
public readonly record struct FiscalYearRange(int Year, DateTime StartUtc, DateTime EndExclusiveUtc)
{
    public bool Contains(DateTime instant) => instant >= StartUtc && instant < EndExclusiveUtc;
}

public static class FiscalYear
{
    public const int CalendarStartMonth = 1;

    // WHY the clamp instead of a throw: tenants.fiscal_year_start_month carried a DB default of 0
    // for its whole life, so a row written outside EF (a raw insert, a restore) can hold a month
    // that does not exist. Refusing to compute would take every list screen down; falling back to
    // January keeps the calendar year, which is what an unconfigured tenant means anyway.
    public static int NormalizeStartMonth(int startMonth) =>
        startMonth is >= 1 and <= 12 ? startMonth : CalendarStartMonth;

    public static FiscalYearRange For(int year, int startMonth)
    {
        var month = NormalizeStartMonth(startMonth);
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        return new FiscalYearRange(year, start, start.AddYears(1));
    }

    public static int YearOf(DateTime instant, int startMonth)
    {
        var month = NormalizeStartMonth(startMonth);
        var utc = instant.Kind == DateTimeKind.Utc ? instant : DateTime.SpecifyKind(instant, DateTimeKind.Utc);
        return utc.Month >= month ? utc.Year : utc.Year - 1;
    }

    public static FiscalYearRange Containing(DateTime instant, int startMonth) =>
        For(YearOf(instant, startMonth), startMonth);
}
