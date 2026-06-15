using System.Globalization;
using CoreAlign.Application.Reports.Common;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CoreAlign.Infrastructure.Reports;

public sealed class QuestPdfReportRenderer : IReportRenderer
{
    private const string Slate900 = "#0F172A";
    private const string Slate700 = "#334155";
    private const string Slate500 = "#64748B";
    private const string Slate200 = "#E2E8F0";
    private const string Slate100 = "#F1F5F9";
    private const string Slate50 = "#F8FAFC";
    private const string Brand = "#2563EB";

    public Task<byte[]> RenderPdfAsync(ReportDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Task.FromResult(BuildPdf(document));
    }

    public Task<byte[]> RenderXlsxAsync(ReportDocument document, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("QuestPdfReportRenderer renders PDF only; use ClosedXmlReportRenderer for XLSX.");

    private static byte[] BuildPdf(ReportDocument document)
    {
        var culture = ResolveCulture(document.Header.Locale, document.Header.Currency);
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(25);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(t => t.FontFamily("Helvetica").FontSize(9).FontColor(Slate900));

                page.Header().Element(h => RenderHeader(h, document.Header, culture));
                page.Content().Element(c => RenderBody(c, document, culture));
                page.Footer().Element(RenderFooter);
            });
        }).GeneratePdf();
    }

    private static void RenderHeader(IContainer container, ReportHeader header, CultureInfo culture)
    {
        container.PaddingBottom(8).BorderBottom(1).BorderColor(Slate200).PaddingBottom(6).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(header.TenantName).FontSize(13).Bold().FontColor(Slate900);
                if (!string.IsNullOrWhiteSpace(header.TenantLegalName))
                {
                    col.Item().Text(header.TenantLegalName!).FontSize(8).FontColor(Slate500);
                }
            });
            row.ConstantItem(280).Column(col =>
            {
                col.Item().AlignRight().Text(header.Title).FontSize(14).Bold().FontColor(Brand);
                if (!string.IsNullOrWhiteSpace(header.Subtitle))
                {
                    col.Item().AlignRight().Text(header.Subtitle!).FontSize(9).FontColor(Slate700);
                }
                col.Item().PaddingTop(4).AlignRight().Text(ComposeMeta(header, culture)).FontSize(8).FontColor(Slate500);
            });
        });
    }

    private static string ComposeMeta(ReportHeader header, CultureInfo culture)
    {
        var parts = new List<string>();
        if (header.PeriodFromUtc.HasValue && header.PeriodToUtc.HasValue)
        {
            parts.Add($"{header.PeriodFromUtc.Value.ToString("yyyy-MM-dd", culture)} → {header.PeriodToUtc.Value.ToString("yyyy-MM-dd", culture)}");
        }
        else if (header.PeriodToUtc.HasValue)
        {
            parts.Add($"As of {header.PeriodToUtc.Value.ToString("yyyy-MM-dd", culture)}");
        }
        parts.Add($"Generated {header.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture)}");
        parts.Add($"Currency: {header.Currency}");
        return string.Join(" · ", parts);
    }

    private static void RenderBody(IContainer container, ReportDocument document, CultureInfo culture)
    {
        container.PaddingVertical(8).Column(col =>
        {
            if (document.Groups is { Count: > 0 })
            {
                foreach (var group in document.Groups)
                {
                    col.Item().PaddingTop(6).Text(group.Label).FontSize(10).Bold().FontColor(Slate700);
                    col.Item().Element(c => RenderTable(c, document.Columns, group.Rows, group.FooterTotals, culture));
                }
            }
            else
            {
                col.Item().Element(c => RenderTable(c, document.Columns, document.Rows, document.FooterTotals, culture));
            }

            if (!string.IsNullOrWhiteSpace(document.Notes))
            {
                col.Item().PaddingTop(8).Text(document.Notes!).FontSize(8).FontColor(Slate500);
            }
        });
    }

    private static void RenderTable(IContainer container, IReadOnlyList<ReportColumn> columns, IReadOnlyList<ReportRow> rows, IReadOnlyList<ReportCell>? totals, CultureInfo culture)
    {
        if (columns.Count == 0)
        {
            return;
        }
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                foreach (var col in columns)
                {
                    if (col.WidthHint is int w && w > 0)
                    {
                        c.ConstantColumn(w);
                    }
                    else
                    {
                        c.RelativeColumn();
                    }
                }
            });

            table.Header(h =>
            {
                foreach (var col in columns)
                {
                    var cell = h.Cell();
                    var aligned = ApplyAlign(cell, col.Align);
                    HeaderCell(aligned, col.Label);
                }
            });

            if (rows.Count == 0)
            {
                table.Cell().ColumnSpan((uint)columns.Count).Background(Slate50).Padding(6).AlignCenter()
                    .Text("No data").FontSize(9).FontColor(Slate500).Italic();
            }
            else
            {
                foreach (var row in rows)
                {
                    for (var i = 0; i < columns.Count; i++)
                    {
                        var col = columns[i];
                        var cell = table.Cell();
                        var aligned = ApplyAlign(cell, col.Align);
                        var value = i < row.Cells.Count ? row.Cells[i].Value : null;
                        BodyCell(aligned, FormatValue(value, col, culture));
                    }
                }
            }

            if (totals is { Count: > 0 })
            {
                for (var i = 0; i < columns.Count; i++)
                {
                    var col = columns[i];
                    var cell = table.Cell();
                    var aligned = ApplyAlign(cell, col.Align);
                    var value = i < totals.Count ? totals[i].Value : null;
                    TotalCell(aligned, FormatValue(value, col, culture));
                }
            }
        });
    }

    private static IContainer ApplyAlign(IContainer container, ReportColumnAlign align) =>
        align switch
        {
            ReportColumnAlign.Right => container.AlignRight(),
            ReportColumnAlign.Center => container.AlignCenter(),
            _ => container,
        };

    private static void HeaderCell(IContainer container, string text) =>
        container.Background(Slate100).BorderBottom(1).BorderColor(Slate200).PaddingVertical(4).PaddingHorizontal(4)
            .Text(text).FontSize(8).Bold().FontColor(Slate700);

    private static void BodyCell(IContainer container, string text) =>
        container.BorderBottom(1).BorderColor(Slate100).PaddingVertical(3).PaddingHorizontal(4)
            .Text(text).FontSize(9).FontColor(Slate900);

    private static void TotalCell(IContainer container, string text) =>
        container.Background(Slate50).BorderTop(1).BorderColor(Slate200).PaddingVertical(4).PaddingHorizontal(4)
            .Text(text).FontSize(9).Bold().FontColor(Slate900);

    private static string FormatValue(object? value, ReportColumn col, CultureInfo culture)
    {
        if (value is null)
        {
            return string.Empty;
        }
        if (value is string s)
        {
            return s;
        }
        return col.Type switch
        {
            ReportColumnType.Integer => Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(col.Format ?? "N0", culture),
            ReportColumnType.Decimal => Convert.ToDecimal(value, CultureInfo.InvariantCulture).ToString(col.Format ?? "N2", culture),
            ReportColumnType.Currency => Convert.ToDecimal(value, CultureInfo.InvariantCulture).ToString(col.Format ?? "N2", culture),
            ReportColumnType.Percent => (Convert.ToDecimal(value, CultureInfo.InvariantCulture)).ToString(col.Format ?? "0.##\\%", culture),
            ReportColumnType.Date => value is DateTime dt ? dt.ToString(col.Format ?? "yyyy-MM-dd", culture) : value.ToString() ?? string.Empty,
            ReportColumnType.DateTime => value is DateTime dtm ? dtm.ToString(col.Format ?? "yyyy-MM-dd HH:mm", culture) : value.ToString() ?? string.Empty,
            _ => value.ToString() ?? string.Empty,
        };
    }

    private static void RenderFooter(IContainer container)
    {
        container.PaddingTop(8).BorderTop(1).BorderColor(Slate200).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text("CoreAlign Reports").FontSize(7).FontColor(Slate500);
            row.ConstantItem(80).AlignRight().Text(text =>
            {
                text.Span("Page ").FontSize(7).FontColor(Slate500);
                text.CurrentPageNumber().FontSize(7).FontColor(Slate500);
                text.Span(" / ").FontSize(7).FontColor(Slate500);
                text.TotalPages().FontSize(7).FontColor(Slate500);
            });
        });
    }

    private static CultureInfo ResolveCulture(string locale, string currency)
    {
        if (!string.IsNullOrWhiteSpace(locale))
        {
            try { return CultureInfo.GetCultureInfo(locale); }
            catch (CultureNotFoundException) { }
        }
        return currency switch
        {
            "TRY" => CultureInfo.GetCultureInfo("tr-TR"),
            "EUR" => CultureInfo.GetCultureInfo("de-DE"),
            "USD" => CultureInfo.GetCultureInfo("en-US"),
            _ => CultureInfo.InvariantCulture,
        };
    }
}
