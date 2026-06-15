namespace CoreAlign.Application.Reports.Common;

public interface IReportRenderer
{
    Task<byte[]> RenderPdfAsync(ReportDocument document, CancellationToken cancellationToken = default);
    Task<byte[]> RenderXlsxAsync(ReportDocument document, CancellationToken cancellationToken = default);
}

public enum ReportColumnType
{
    Text,
    Integer,
    Decimal,
    Currency,
    Date,
    DateTime,
    Percent,
}

public enum ReportColumnAlign
{
    Left,
    Right,
    Center,
}

public sealed record ReportColumn(
    string Key,
    string Label,
    ReportColumnType Type = ReportColumnType.Text,
    ReportColumnAlign Align = ReportColumnAlign.Left,
    string? Format = null,
    int? WidthHint = null);

public sealed record ReportCell(object? Value)
{
    public static ReportCell From(object? value) => new(value);
    public static ReportCell Empty { get; } = new((object?)null);
}

public sealed record ReportRow(IReadOnlyList<ReportCell> Cells)
{
    public static ReportRow Of(params object?[] values) =>
        new(values.Select(v => new ReportCell(v)).ToArray());
}

public sealed record ReportGroup(string Label, IReadOnlyList<ReportRow> Rows, IReadOnlyList<ReportCell>? FooterTotals = null);

public sealed record ReportHeader(
    string TenantName,
    string? TenantLegalName,
    string Title,
    string? Subtitle,
    DateTime GeneratedAtUtc,
    DateTime? PeriodFromUtc = null,
    DateTime? PeriodToUtc = null,
    string Currency = "TRY",
    string Locale = "en-US");

public sealed record ReportDocument(
    ReportHeader Header,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<ReportRow> Rows,
    IReadOnlyList<ReportGroup>? Groups = null,
    IReadOnlyList<ReportCell>? FooterTotals = null,
    string? Notes = null);
