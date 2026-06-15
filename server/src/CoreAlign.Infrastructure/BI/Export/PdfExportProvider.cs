using System.Globalization;
using CoreAlign.Application.BI;
using CoreAlign.Domain.Entities.Reporting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CoreAlign.Infrastructure.BI.Export;

public sealed class PdfExportProvider : IExportProvider
{
    public BIExportFormat Format => BIExportFormat.Pdf;

    public Task<byte[]> ExportAsync(string title, BIResultDto result, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        var safeTitle = string.IsNullOrWhiteSpace(title) ? "BI Report" : title;
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(25);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(t => t.FontFamily("Helvetica").FontSize(9));

                page.Header().Text(safeTitle).FontSize(16).Bold();
                page.Content().Element(c => RenderTable(c, result));
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generated ").FontColor(Colors.Grey.Medium);
                    t.Span(DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture)).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
        return Task.FromResult(bytes);
    }

    private static void RenderTable(IContainer container, BIResultDto result)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                foreach (var _ in result.Columns)
                {
                    cols.RelativeColumn();
                }
            });
            table.Header(header =>
            {
                foreach (var col in result.Columns)
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(col.Label).Bold();
                }
            });
            foreach (var row in result.Rows)
            {
                foreach (var col in result.Columns)
                {
                    var v = row.TryGetValue(col.Key, out var val) ? val : null;
                    var text = v switch
                    {
                        null => string.Empty,
                        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                        _ => v.ToString() ?? string.Empty,
                    };
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(text);
                }
            }
        });
    }
}
