using System.Text;
using CoreAlign.Application.Documents;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Documents;
using QuestPDF.Infrastructure;

namespace CoreAlign.Application.Tests.Documents;

public class QuestPdfDocumentRendererTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    static QuestPdfDocumentRendererTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task Renders_invoice_pdf_with_pdf_magic_header()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var model = BuildInvoiceModel(lineCount: 3);

        var pdf = await renderer.RenderInvoiceAsync(model, CancellationToken.None);

        pdf.Should().NotBeNull();
        pdf.Length.Should().BeGreaterThan(500);
        Encoding.ASCII.GetString(pdf, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public async Task Renders_credit_note_pdf_with_pdf_magic_header()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var model = BuildInvoiceModel(lineCount: 1);

        var pdf = await renderer.RenderCreditNoteAsync(model, CancellationToken.None);

        pdf.Length.Should().BeGreaterThan(500);
        Encoding.ASCII.GetString(pdf, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public async Task Renders_multi_page_invoice_when_lines_exceed_single_page()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var model = BuildInvoiceModel(lineCount: 120);

        var pdf = await renderer.RenderInvoiceAsync(model, CancellationToken.None);

        Encoding.ASCII.GetString(pdf, 0, 4).Should().Be("%PDF");
        var pdfText = Encoding.ASCII.GetString(pdf);
        var pageCount = CountPageObjects(pdfText);
        pageCount.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task Renders_order_confirmation_pdf()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var model = BuildOrderModel(lineCount: 2);

        var pdf = await renderer.RenderOrderConfirmationAsync(model, CancellationToken.None);

        pdf.Length.Should().BeGreaterThan(500);
        Encoding.ASCII.GetString(pdf, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public async Task Renders_packing_slip_pdf()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var model = BuildShipmentModel();

        var pdf = await renderer.RenderPackingSlipAsync(model, CancellationToken.None);

        pdf.Length.Should().BeGreaterThan(500);
        Encoding.ASCII.GetString(pdf, 0, 4).Should().Be("%PDF");
    }

    private static int CountPageObjects(string pdfText)
    {
        var count = 0;
        var idx = 0;
        while ((idx = pdfText.IndexOf("/Type /Page", idx, StringComparison.Ordinal)) >= 0)
        {
            if (idx + 11 < pdfText.Length && pdfText[idx + 11] != 's')
            {
                count++;
            }
            idx += 11;
        }
        return count;
    }

    private static InvoiceDocumentModel BuildInvoiceModel(int lineCount)
    {
        var tenant = BuildTenant();
        var customer = BuildCustomer();
        var invoice = new Invoice("INV-DOC-0099", CustomerId, "Demo Müşteri", "TRY", InvoiceType.SalesInvoice)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };

        var lines = new List<InvoiceLine>();
        for (var i = 1; i <= lineCount; i++)
        {
            var line = new InvoiceLine(Guid.NewGuid(), $"SKU-{i:D4}", $"Item {i}", 1m, 25m)
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
            };
            line.SetLineNumber(i);
            line.ApplyPricing(1m, 25m, 0m, 0m, 20m, null, false, 0m, null, "EA", $"Description for item {i}", null, null, null, null);
            lines.Add(line);
        }
        invoice.ReplaceLines(lines);
        return invoice.ToInvoiceDocumentModel(tenant, customer, null);
    }

    private static OrderDocumentModel BuildOrderModel(int lineCount)
    {
        var tenant = BuildTenant();
        var customer = BuildCustomer();
        var order = new Order("ORD-DOC-0001", CustomerId, DateTime.UtcNow, "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        var orderLines = new List<OrderLine>();
        for (var i = 1; i <= lineCount; i++)
        {
            var line = new OrderLine(Guid.NewGuid(), $"SKU-{i}", $"Item {i}", 2m, 30m)
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
            };
            line.SetLineNumber(i);
            line.ApplyPricing(2m, 30m, 30m, 0m, 0m, false, 20m, null, false, 0m, 15m, null, "EA", 1m, null, null, null, false, null);
            orderLines.Add(line);
        }
        order.ReplaceLines(orderLines);
        return order.ToOrderDocumentModel(tenant, customer, null);
    }

    private static ShipmentDocumentModel BuildShipmentModel()
    {
        var tenant = BuildTenant();
        var customer = BuildCustomer();
        var order = new Order("ORD-DOC-0002", CustomerId, DateTime.UtcNow, "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        var shipment = new Shipment("SHP-0001", order.Id, CustomerId, Guid.NewGuid(), null)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        return shipment.ToShipmentDocumentModel(order, tenant, customer, warehouse: null);
    }

    private static Tenant BuildTenant()
    {
        var tenant = new Tenant("Acme", "acme")
        {
            Id = TenantId,
        };
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
