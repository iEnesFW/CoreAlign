using System.Globalization;
using ClosedXML.Excel;
using CoreAlign.Application.Reports.Common;

namespace CoreAlign.Infrastructure.Reports;

public sealed class ClosedXmlReportRenderer : IReportRenderer
{
    public Task<byte[]> RenderPdfAsync(ReportDocument document, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("ClosedXmlReportRenderer renders XLSX only; use QuestPdfReportRenderer for PDF.");

    public Task<byte[]> RenderXlsxAsync(ReportDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        using var workbook = new XLWorkbook();
        var sheetName = SanitizeSheetName(document.Header.Title);
        var sheet = workbook.AddWorksheet(sheetName);

        WriteHeader(sheet, document);
        var startDataRow = 6;
        WriteTable(sheet, document, startDataRow);

        sheet.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }

    private static void WriteHeader(IXLWorksheet sheet, ReportDocument document)
    {
        var titleCell = sheet.Cell(1, 1);
        titleCell.Value = NeutralizeFormula(document.Header.Title);
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 14;

        sheet.Cell(2, 1).Value = NeutralizeFormula(document.Header.TenantName);
        if (!string.IsNullOrWhiteSpace(document.Header.Subtitle))
        {
            sheet.Cell(3, 1).Value = NeutralizeFormula(document.Header.Subtitle);
        }

        var period = ComposePeriod(document.Header);
        sheet.Cell(4, 1).Value = NeutralizeFormula(period);
        sheet.Cell(5, 1).Value = $"Currency: {document.Header.Currency} · Generated {document.Header.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC";
        sheet.Cell(5, 1).Style.Font.Italic = true;
        sheet.Cell(5, 1).Style.Font.FontColor = XLColor.Gray;
    }

    private static string ComposePeriod(ReportHeader header)
    {
        if (header.PeriodFromUtc.HasValue && header.PeriodToUtc.HasValue)
        {
            return $"Period: {header.PeriodFromUtc.Value:yyyy-MM-dd} → {header.PeriodToUtc.Value:yyyy-MM-dd}";
        }
        if (header.PeriodToUtc.HasValue)
        {
            return $"As of {header.PeriodToUtc.Value:yyyy-MM-dd}";
        }
        return string.Empty;
    }

    private static void WriteTable(IXLWorksheet sheet, ReportDocument document, int startRow)
    {
        var columns = document.Columns;
        if (columns.Count == 0)
        {
            return;
        }
        var row = startRow;

        for (var i = 0; i < columns.Count; i++)
        {
            var cell = sheet.Cell(row, i + 1);
            cell.Value = NeutralizeFormula(columns[i].Label);
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            ApplyAlign(cell, columns[i].Align);
        }
        row++;

        if (document.Groups is { Count: > 0 })
        {
            foreach (var group in document.Groups)
            {
                var groupCell = sheet.Cell(row, 1);
                groupCell.Value = NeutralizeFormula(group.Label);
                groupCell.Style.Font.Bold = true;
                groupCell.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
                sheet.Range(row, 1, row, columns.Count).Merge();
                row++;
                row = WriteRows(sheet, columns, group.Rows, row);
                if (group.FooterTotals is { Count: > 0 })
                {
                    row = WriteTotalRow(sheet, columns, group.FooterTotals, row);
                }
            }
        }
        else
        {
            row = WriteRows(sheet, columns, document.Rows, row);
        }

        if (document.FooterTotals is { Count: > 0 })
        {
            _ = WriteTotalRow(sheet, columns, document.FooterTotals, row);
        }
    }

    private static int WriteRows(IXLWorksheet sheet, IReadOnlyList<ReportColumn> columns, IReadOnlyList<ReportRow> rows, int startRow)
    {
        var row = startRow;
        foreach (var dataRow in rows)
        {
            for (var i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                var cell = sheet.Cell(row, i + 1);
                var value = i < dataRow.Cells.Count ? dataRow.Cells[i].Value : null;
                ApplyCell(cell, value, col);
            }
            row++;
        }
        return row;
    }

    private static int WriteTotalRow(IXLWorksheet sheet, IReadOnlyList<ReportColumn> columns, IReadOnlyList<ReportCell> totals, int startRow)
    {
        for (var i = 0; i < columns.Count; i++)
        {
            var col = columns[i];
            var cell = sheet.Cell(startRow, i + 1);
            var value = i < totals.Count ? totals[i].Value : null;
            ApplyCell(cell, value, col);
            cell.Style.Font.Bold = true;
            cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            cell.Style.Fill.BackgroundColor = XLColor.Snow;
        }
        return startRow + 1;
    }

    private static void ApplyAlign(IXLCell cell, ReportColumnAlign align)
    {
        cell.Style.Alignment.Horizontal = align switch
        {
            ReportColumnAlign.Right => XLAlignmentHorizontalValues.Right,
            ReportColumnAlign.Center => XLAlignmentHorizontalValues.Center,
            _ => XLAlignmentHorizontalValues.Left,
        };
    }

    private static void ApplyCell(IXLCell cell, object? value, ReportColumn col)
    {
        ApplyAlign(cell, col.Align);
        if (value is null)
        {
            cell.Value = string.Empty;
            return;
        }
        if (value is string str)
        {
            cell.Value = NeutralizeFormula(str);
            return;
        }
        switch (col.Type)
        {
            case ReportColumnType.Integer:
                cell.Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                cell.Style.NumberFormat.Format = col.Format ?? "#,##0";
                break;
            case ReportColumnType.Decimal:
            case ReportColumnType.Currency:
                cell.Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                cell.Style.NumberFormat.Format = col.Format ?? "#,##0.00";
                break;
            case ReportColumnType.Percent:
                cell.Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                cell.Style.NumberFormat.Format = col.Format ?? "0.00%";
                break;
            case ReportColumnType.Date:
                if (value is DateTime dt)
                {
                    cell.Value = dt;
                    cell.Style.DateFormat.Format = col.Format ?? "yyyy-mm-dd";
                }
                else
                {
                    cell.Value = NeutralizeFormula(value.ToString());
                }
                break;
            case ReportColumnType.DateTime:
                if (value is DateTime dtm)
                {
                    cell.Value = dtm;
                    cell.Style.DateFormat.Format = col.Format ?? "yyyy-mm-dd hh:mm";
                }
                else
                {
                    cell.Value = NeutralizeFormula(value.ToString());
                }
                break;
            default:
                cell.Value = NeutralizeFormula(value.ToString());
                break;
        }
    }

    public static string NeutralizeFormula(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }
        var first = value[0];
        if (first == '=' || first == '+' || first == '-' || first == '@' || first == '\t' || first == '\r' || first == '\n')
        {
            return "'" + value;
        }
        return value;
    }

    private static string SanitizeSheetName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "Report";
        }
        var trimmed = raw.Trim();
        var cleaned = new string(trimmed.Where(c => c != ':' && c != '/' && c != '\\' && c != '?' && c != '*' && c != '[' && c != ']').ToArray());
        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }
}
