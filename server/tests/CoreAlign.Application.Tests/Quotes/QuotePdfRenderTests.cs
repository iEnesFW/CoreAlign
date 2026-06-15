using System.Text;
using CoreAlign.Application.Documents;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Documents;
using QuestPDF.Infrastructure;

namespace CoreAlign.Application.Tests.Quotes;

public class QuotePdfRenderTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    static QuotePdfRenderTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task Renders_quote_pdf_with_pdf_magic_header()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var model = BuildQuoteModel(lineCount: 3);

        var pdf = await renderer.RenderQuoteAsync(model, CancellationToken.None);

        pdf.Should().NotBeNull();
        pdf.Length.Should().BeGreaterThan(500);
        Encoding.ASCII.GetString(pdf, 0, 4).Should().Be("%PDF");
    }

    private static QuoteDocumentModel BuildQuoteModel(int lineCount)
    {
        var tenant = BuildTenant();
        var customer = BuildCustomer();
        var quote = new Quote(
            "QUO-PDF-0001",
            CustomerId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(15),
            "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };

        var lines = new List<QuoteLine>();
        for (var i = 1; i <= lineCount; i++)
        {
            var line = new QuoteLine(ProductId, $"SKU-{i}", $"Item {i}", 2m, 30m)
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
            };
            line.SetLineNumber(i);
            line.ApplyPricing(2m, 30m, 30m, 0m, 0m, false, 20m, null, false, 0m, null, "EA", 1m, null, $"Desc {i}");
            lines.Add(line);
        }
        quote.ReplaceLines(lines);
        return quote.ToQuoteDocumentModel(tenant, customer, null);
    }

    private static Tenant BuildTenant()
    {
        var tenant = new Tenant("Acme", "acme") { Id = TenantId };
        tenant.UpdateProfile(
            "Acme", "Acme A.Ş.", "Acme", "1234567890", "Kadıköy",
            null, null, null, "Software", null, null,
            "Atatürk Cd 1", null, "İstanbul", null, "34000", "TR",
            "+90 212 0000000", null, "info@acme.test", "https://acme.test",
            "TRY", null, "tr-TR", "Europe/Istanbul", 1, null, null);
        return tenant;
    }

    private static Customer BuildCustomer()
    {
        return new Customer(
            name: "Demo Müşteri",
            type: CustomerType.Business,
            code: "C-0001",
            legalName: "Demo Müşteri A.Ş.",
            tradeName: "Demo",
            email: "demo@customer.test",
            phone: "+90 212 1111111",
            taxNumber: "9876543210")
        {
            Id = CustomerId,
            TenantId = TenantId,
        };
    }
}
