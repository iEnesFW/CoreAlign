using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Reporting;

public class ReportSchedule : TenantEntity
{
    public string Name { get; private set; } = string.Empty;
    public string ReportKey { get; private set; } = string.Empty;
    public Guid? CustomReportDefinitionId { get; private set; }
    public ReportFrequency Frequency { get; private set; }
    public string? CronExpression { get; private set; }
    public string RecipientsJson { get; private set; } = "[]";
    public ReportDeliveryFormat Format { get; private set; } = ReportDeliveryFormat.Pdf;
    public string FiltersJson { get; private set; } = "{}";
    public bool IsActive { get; private set; } = true;
    public DateTime NextRunAtUtc { get; private set; }
    public DateTime? LastRunAtUtc { get; private set; }
    public string? LastRunStatus { get; private set; }
    public string? LastRunError { get; private set; }
    public Guid? CreatedByUserId { get; private set; }

    protected ReportSchedule() { }

    public ReportSchedule(
        string name,
        string reportKey,
        Guid? customReportDefinitionId,
        ReportFrequency frequency,
        string? cronExpression,
        string recipientsJson,
        ReportDeliveryFormat format,
        string filtersJson,
        DateTime nextRunAtUtc,
        Guid? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Schedule name is required.", nameof(name));
        }
        if (string.IsNullOrWhiteSpace(reportKey) && customReportDefinitionId is null)
        {
            throw new ArgumentException("Either reportKey or customReportDefinitionId must be provided.");
        }
        Name = name.Trim();
        ReportKey = reportKey?.Trim() ?? string.Empty;
        CustomReportDefinitionId = customReportDefinitionId;
        Frequency = frequency;
        CronExpression = cronExpression;
        RecipientsJson = recipientsJson ?? "[]";
        Format = format;
        FiltersJson = filtersJson ?? "{}";
        NextRunAtUtc = DateTime.SpecifyKind(nextRunAtUtc, DateTimeKind.Utc);
        CreatedByUserId = createdByUserId;
    }

    public void Update(
        string name,
        string reportKey,
        Guid? customReportDefinitionId,
        ReportFrequency frequency,
        string? cronExpression,
        string recipientsJson,
        ReportDeliveryFormat format,
        string filtersJson)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Schedule name is required.", nameof(name));
        }
        Name = name.Trim();
        ReportKey = reportKey?.Trim() ?? string.Empty;
        CustomReportDefinitionId = customReportDefinitionId;
        Frequency = frequency;
        CronExpression = cronExpression;
        RecipientsJson = recipientsJson ?? "[]";
        Format = format;
        FiltersJson = filtersJson ?? "{}";
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordRun(DateTime ranAtUtc, string status, string? error, DateTime nextRunAtUtc)
    {
        LastRunAtUtc = DateTime.SpecifyKind(ranAtUtc, DateTimeKind.Utc);
        LastRunStatus = status;
        LastRunError = error;
        NextRunAtUtc = DateTime.SpecifyKind(nextRunAtUtc, DateTimeKind.Utc);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public static DateTime ComputeNextRunAtUtc(ReportFrequency frequency, DateTime fromUtc)
    {
        var anchor = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc);
        return frequency switch
        {
            ReportFrequency.Hourly => anchor.AddHours(1),
            ReportFrequency.Daily => anchor.AddDays(1),
            ReportFrequency.Weekly => anchor.AddDays(7),
            ReportFrequency.Monthly => anchor.AddMonths(1),
            _ => anchor.AddDays(1),
        };
    }
}
