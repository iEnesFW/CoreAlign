using CoreAlign.Domain.Entities.Reporting;

namespace CoreAlign.Application.Reports.Schedules;

public sealed record ReportScheduleDto(
    Guid Id,
    string Name,
    string ReportKey,
    Guid? CustomReportDefinitionId,
    string Frequency,
    string? CronExpression,
    IReadOnlyList<string> Recipients,
    string Format,
    string FiltersJson,
    bool IsActive,
    DateTime NextRunAtUtc,
    DateTime? LastRunAtUtc,
    string? LastRunStatus,
    string? LastRunError);

public sealed record CreateReportScheduleRequestDto(
    string Name,
    string? ReportKey,
    Guid? CustomReportDefinitionId,
    ReportFrequency Frequency,
    string? CronExpression,
    IReadOnlyList<string> Recipients,
    ReportDeliveryFormat Format,
    string? FiltersJson,
    DateTime? StartAtUtc);

public sealed record UpdateReportScheduleRequestDto(
    string Name,
    string? ReportKey,
    Guid? CustomReportDefinitionId,
    ReportFrequency Frequency,
    string? CronExpression,
    IReadOnlyList<string> Recipients,
    ReportDeliveryFormat Format,
    string? FiltersJson,
    bool? IsActive);
