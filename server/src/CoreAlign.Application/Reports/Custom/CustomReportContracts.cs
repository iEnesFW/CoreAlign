using CoreAlign.Domain.Entities.Reporting;

namespace CoreAlign.Application.Reports.Custom;

public sealed record CustomReportFieldDto(
    string Key,
    string LabelEn,
    string LabelTr,
    string DataType,
    bool IsDimension,
    bool IsMeasureEligible,
    IReadOnlyList<string> AllowedOperators,
    IReadOnlyList<string>? AllowedAggregations);

public sealed record CustomReportFieldGroupDto(
    string EntityType,
    IReadOnlyList<CustomReportFieldDto> Fields);

public sealed record CustomReportFilterDto(
    string Field,
    string Operator,
    string? Value,
    string? Value2);

public sealed record CustomReportMeasureDto(
    string Field,
    string Function,
    string? Alias = null);

public sealed record CustomReportSortDto(
    string Field,
    bool Descending);

public sealed record CustomReportDefinitionDto(
    ReportEntityType EntityType,
    IReadOnlyList<string> Dimensions,
    IReadOnlyList<CustomReportMeasureDto> Measures,
    IReadOnlyList<CustomReportFilterDto>? Filters = null,
    CustomReportSortDto? SortBy = null,
    int? Limit = null);

public sealed record CustomReportPreviewRowDto(IReadOnlyDictionary<string, object?> Cells);

public sealed record CustomReportPreviewDto(
    IReadOnlyList<string> Columns,
    IReadOnlyList<CustomReportPreviewRowDto> Rows,
    int RowCount,
    bool Truncated);

public sealed record CustomReportSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    string EntityType,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record SaveCustomReportRequestDto(
    string Name,
    string? Description,
    CustomReportDefinitionDto Definition);
