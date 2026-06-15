using System.Text;
using ClosedXML.Excel;
using CoreAlign.Application.Reports.Common;
using CoreAlign.Infrastructure.Reports;
using QuestPDF.Infrastructure;

namespace CoreAlign.Application.Tests.Reports;

public class ReportRendererTests
{
    static ReportRendererTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static ReportDocument SampleDocument(int rowCount = 3)
    {
        var header = new ReportHeader(
            TenantName: "Acme",
            TenantLegalName: "Acme A.Ş.",
            Title: "Sample Report",
            Subtitle: "Test",
            GeneratedAtUtc: new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            PeriodFromUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodToUtc: new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc),
            Currency: "TRY",
            Locale: "tr-TR");

        var columns = new List<ReportColumn>
        {
            new("sku", "SKU", ReportColumnType.Text),
            new("name", "Name", ReportColumnType.Text),
            new("qty", "Qty", ReportColumnType.Decimal, ReportColumnAlign.Right),
            new("value", "Value", ReportColumnType.Currency, ReportColumnAlign.Right),
        };

        var rows = Enumerable.Range(1, rowCount)
            .Select(i => ReportRow.Of($"SKU-{i:D3}", $"Item {i}", 10m * i, 100m * i))
            .ToList();

        var footer = new List<ReportCell>
        {
            ReportCell.From("Total"),
            ReportCell.Empty,
            ReportCell.From(rows.Sum(_ => 10m)),
            ReportCell.From(rows.Sum(_ => 100m)),
        };

        return new ReportDocument(header, columns, rows, FooterTotals: footer);
    }

    [Fact]
    public async Task QuestPdfRenderer_emits_pdf_magic_header()
    {
        var renderer = new QuestPdfReportRenderer();
        var bytes = await renderer.RenderPdfAsync(SampleDocument(5), CancellationToken.None);
        bytes.Length.Should().BeGreaterThan(500);
        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public async Task QuestPdfRenderer_handles_empty_rows()
    {
        var renderer = new QuestPdfReportRenderer();
        var bytes = await renderer.RenderPdfAsync(SampleDocument(0), CancellationToken.None);
        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public async Task QuestPdfRenderer_throws_on_xlsx_call()
    {
        var renderer = new QuestPdfReportRenderer();
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            renderer.RenderXlsxAsync(SampleDocument(), CancellationToken.None));
    }

    [Fact]
    public async Task ClosedXmlRenderer_emits_xlsx_with_zip_magic_header()
    {
        var renderer = new ClosedXmlReportRenderer();
        var bytes = await renderer.RenderXlsxAsync(SampleDocument(5), CancellationToken.None);
        bytes.Length.Should().BeGreaterThan(500);
        bytes[0].Should().Be(0x50);
        bytes[1].Should().Be(0x4B);
    }

    [Fact]
    public async Task ClosedXmlRenderer_writes_columns_and_rows_into_sheet()
    {
        var renderer = new ClosedXmlReportRenderer();
        var bytes = await renderer.RenderXlsxAsync(SampleDocument(3), CancellationToken.None);
        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheets.First();
        sheet.Name.Should().Be("Sample Report");
        sheet.Cell(1, 1).GetString().Should().Be("Sample Report");
        sheet.Cell(6, 1).GetString().Should().Be("SKU");
        sheet.Cell(6, 2).GetString().Should().Be("Name");
        sheet.Cell(7, 1).GetString().Should().StartWith("SKU-");
    }

    [Fact]
    public async Task ClosedXmlRenderer_throws_on_pdf_call()
    {
        var renderer = new ClosedXmlReportRenderer();
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            renderer.RenderPdfAsync(SampleDocument(), CancellationToken.None));
    }

    [Fact]
    public async Task ReportFileFactory_renders_pdf_via_quest_pdf_renderer()
    {
        var factory = new ReportFileFactory(new IReportRenderer[]
        {
            new QuestPdfReportRenderer(),
            new ClosedXmlReportRenderer(),
        });
        var file = await factory.RenderAsync(SampleDocument(2), ReportFormat.Pdf, "inventory-stock-on-hand", CancellationToken.None);
        file.ContentType.Should().Be("application/pdf");
        file.FileName.Should().StartWith("inventory-stock-on-hand-");
        file.FileName.Should().EndWith(".pdf");
        Encoding.ASCII.GetString(file.Content, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public async Task ReportFileFactory_renders_xlsx_via_closed_xml_renderer()
    {
        var factory = new ReportFileFactory(new IReportRenderer[]
        {
            new QuestPdfReportRenderer(),
            new ClosedXmlReportRenderer(),
        });
        var file = await factory.RenderAsync(SampleDocument(2), ReportFormat.Xlsx, "purchase-by-vendor", CancellationToken.None);
        file.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        file.FileName.Should().EndWith(".xlsx");
        file.Content[0].Should().Be(0x50);
    }

    [Fact]
    public void NeutralizeFormula_prefixes_dangerous_leading_chars_and_passes_safe_strings()
    {
        ClosedXmlReportRenderer.NeutralizeFormula("=cmd|'/c calc'!A0").Should().Be("'=cmd|'/c calc'!A0");
        ClosedXmlReportRenderer.NeutralizeFormula("+SUM(1+1)").Should().Be("'+SUM(1+1)");
        ClosedXmlReportRenderer.NeutralizeFormula("-2+3").Should().Be("'-2+3");
        ClosedXmlReportRenderer.NeutralizeFormula("@HYPERLINK").Should().Be("'@HYPERLINK");
        ClosedXmlReportRenderer.NeutralizeFormula("\tTabbed").Should().Be("'\tTabbed");
        ClosedXmlReportRenderer.NeutralizeFormula("\rRet").Should().Be("'\rRet");
        ClosedXmlReportRenderer.NeutralizeFormula("\nNew").Should().Be("'\nNew");
        ClosedXmlReportRenderer.NeutralizeFormula("Plain Name").Should().Be("Plain Name");
        ClosedXmlReportRenderer.NeutralizeFormula(string.Empty).Should().Be(string.Empty);
        ClosedXmlReportRenderer.NeutralizeFormula(null).Should().Be(string.Empty);
    }

    [Fact]
    public async Task ClosedXmlRenderer_does_not_persist_formulas_for_user_strings()
    {
        var header = new ReportHeader("Acme", null, "Inj Test", null, DateTime.UtcNow,
            PeriodToUtc: DateTime.UtcNow, Currency: "TRY", Locale: "tr-TR");
        var columns = new List<ReportColumn>
        {
            new("name", "Name", ReportColumnType.Text),
        };
        var rows = new List<ReportRow>
        {
            ReportRow.Of("=cmd|'/c calc'!A0"),
            ReportRow.Of("+SUM(1+1)"),
            ReportRow.Of("Plain Name"),
        };
        var doc = new ReportDocument(header, columns, rows);
        var renderer = new ClosedXmlReportRenderer();
        var bytes = await renderer.RenderXlsxAsync(doc, CancellationToken.None);
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var sheet = wb.Worksheets.First();
        sheet.Cell(7, 1).HasFormula.Should().BeFalse();
        sheet.Cell(8, 1).HasFormula.Should().BeFalse();
        sheet.Cell(9, 1).HasFormula.Should().BeFalse();
        sheet.Cell(9, 1).GetString().Should().Be("Plain Name");
    }

    [Fact]
    public async Task QuestPdfRenderer_renders_grouped_document()
    {
        var header = new ReportHeader("Acme", null, "Grouped", null, DateTime.UtcNow,
            PeriodFromUtc: DateTime.UtcNow.AddDays(-30), PeriodToUtc: DateTime.UtcNow, Currency: "TRY", Locale: "tr-TR");
        var columns = new List<ReportColumn>
        {
            new("date", "Date", ReportColumnType.Date),
            new("amount", "Amount", ReportColumnType.Currency, ReportColumnAlign.Right),
        };
        var group = new ReportGroup("Operating", new List<ReportRow>
        {
            ReportRow.Of((object?)DateTime.UtcNow, 100m),
            ReportRow.Of((object?)DateTime.UtcNow, 200m),
        }, new List<ReportCell>
        {
            ReportCell.From("Total"),
            ReportCell.From(300m),
        });
        var doc = new ReportDocument(header, columns, Array.Empty<ReportRow>(), Groups: new[] { group });

        var renderer = new QuestPdfReportRenderer();
        var bytes = await renderer.RenderPdfAsync(doc, CancellationToken.None);
        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }
}
