using CoreAlign.Application.Reports.Common;
using CoreAlign.Domain.Entities.Reporting;

namespace CoreAlign.Application.Reports.Custom;

public static class CustomReportDocumentBuilder
{
    public static ReportDocument Build(
        string title,
        string tenantName,
        string? tenantLegalName,
        string currency,
        string locale,
        CustomReportPreviewDto preview)
    {
        var header = new ReportHeader(
            TenantName: tenantName,
            TenantLegalName: tenantLegalName,
            Title: title,
            Subtitle: null,
            GeneratedAtUtc: DateTime.UtcNow,
            PeriodFromUtc: null,
            PeriodToUtc: DateTime.UtcNow,
            Currency: currency,
            Locale: locale);

        var columns = preview.Columns
            .Select(c => new ReportColumn(c, c, GuessType(c, preview)))
            .ToList();
        var rows = preview.Rows
            .Select(r => new ReportRow(preview.Columns.Select(c => new ReportCell(r.Cells.TryGetValue(c, out var v) ? v : null)).ToList()))
            .ToList();
        return new ReportDocument(header, columns, rows);
    }

    private static ReportColumnType GuessType(string column, CustomReportPreviewDto preview)
    {
        var first = preview.Rows.FirstOrDefault();
        if (first is null) return ReportColumnType.Text;
        if (!first.Cells.TryGetValue(column, out var value) || value is null) return ReportColumnType.Text;
        return value switch
        {
            decimal => ReportColumnType.Decimal,
            int => ReportColumnType.Integer,
            long => ReportColumnType.Integer,
            DateTime => ReportColumnType.DateTime,
            _ => ReportColumnType.Text,
        };
    }
}
