using ClosedXML.Excel;
using CoreAlign.Application.BI;
using CoreAlign.Domain.Entities.Reporting;

namespace CoreAlign.Infrastructure.BI.Export;

public sealed class ExcelExportProvider : IExportProvider
{
    public BIExportFormat Format => BIExportFormat.Xlsx;

    public Task<byte[]> ExportAsync(string title, BIResultDto result, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        using var workbook = new XLWorkbook();
        var sheetName = string.IsNullOrWhiteSpace(title) ? "Report" : Truncate(title, 31);
        var sheet = workbook.Worksheets.Add(sheetName);

        for (var i = 0; i < result.Columns.Count; i++)
        {
            sheet.Cell(1, i + 1).Value = result.Columns[i].Label;
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
        }
        var rowIndex = 2;
        foreach (var row in result.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var c = 0; c < result.Columns.Count; c++)
            {
                var key = result.Columns[c].Key;
                if (row.TryGetValue(key, out var value) && value is not null)
                {
                    sheet.Cell(rowIndex, c + 1).Value = XLCellValue.FromObject(value);
                }
            }
            rowIndex++;
        }
        sheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max);
}
