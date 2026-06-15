using CoreAlign.Domain.Entities.Reporting;

namespace CoreAlign.Application.BI;

public sealed record BIQueryFilterDto(string Field, string Operator, string? Value, string? Value2);

public sealed record BIQueryConfigDto(
    string? GroupBy,
    string? Aggregation,
    string? MeasureField,
    DateTime? FromUtc,
    DateTime? ToUtc,
    IReadOnlyList<BIQueryFilterDto>? Filters,
    int? Limit);

public sealed record BIResultColumnDto(string Key, string Label, string DataType);

public sealed record BIResultDto(
    IReadOnlyList<BIResultColumnDto> Columns,
    IReadOnlyList<IDictionary<string, object?>> Rows,
    int TotalRowCount);

public sealed record DashboardWidgetDto(
    Guid Id,
    Guid? UserId,
    string Title,
    DashboardWidgetType Type,
    BIDataSource DataSource,
    string QueryConfigJson,
    int GridX,
    int GridY,
    int Width,
    int Height,
    int DisplayOrder,
    bool IsActive);

public sealed record DashboardWidgetUpsertDto(
    Guid? Id,
    string Title,
    DashboardWidgetType Type,
    BIDataSource DataSource,
    string QueryConfigJson,
    int GridX,
    int GridY,
    int Width,
    int Height,
    int DisplayOrder);

public sealed record SavedReportDto(
    Guid Id,
    Guid OwnerUserId,
    string Name,
    string? Description,
    BIDataSource DataSource,
    string QueryConfigJson,
    bool IsPublic,
    DateTime? LastRunAtUtc,
    int? LastRunRowCount);

public sealed record SavedReportUpsertDto(
    Guid? Id,
    string Name,
    string? Description,
    BIDataSource DataSource,
    string QueryConfigJson,
    bool IsPublic);

public sealed record ReportRunDto(
    Guid Id,
    Guid SavedReportId,
    Guid RanByUserId,
    DateTime RanAtUtc,
    int ResultRowCount,
    BIExportFormat? ExportFormat,
    long? DurationMs,
    string? ErrorMessage);
