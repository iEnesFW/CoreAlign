using CoreAlign.Application.Documents;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.Documents;

public class InvoiceDocumentModelMapperTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    [Fact]
    public void Maps_invoice_lines_with_sku_name_and_unit()
    {
        var invoice = BuildInvoice(InvoiceType.SalesInvoice, lineCount: 3);
        var tenant = BuildTenant();
        var customer = BuildCustomer();

        var model = invoice.ToInvoiceDocumentModel(tenant, customer, null);

        model.Lines.Should().HaveCount(3);
        model.Lines.Select(l => l.Sku).Should().BeEquivalentTo(new[] { "SKU-1", "SKU-2", "SKU-3" });
        model.Lines.Select(l => l.LineNumber).Should().BeEquivalentTo(new[] { 1, 2, 3 });
        model.Lines.All(l => l.UnitCode == "C62").Should().BeTrue();
    }

    [Fact]
    public void Computes_totals_consistent_with_invoice_aggregate()
    {
        var invoice = BuildInvoice(InvoiceType.SalesInvoice, lineCount: 4);
        var tenant = BuildTenant();
        var customer = BuildCustomer();

        var model = invoice.ToInvoiceDocumentModel(tenant, customer, null);

        model.Subtotal.Should().Be(invoice.Subtotal);
        model.TaxTotal.Should().Be(invoice.TaxTotal);
        model.GrandTotal.Should().Be(invoice.Total);
        model.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Tax_breakdown_groups_lines_by_rate()
    {
        var invoice = BuildInvoiceWithMixedRates();
        var tenant = BuildTenant();
        var customer = BuildCustomer();

        var model = invoice.ToInvoiceDocumentModel(tenant, customer, null);

        model.TaxBreakdown.Should().HaveCount(2);
        model.TaxBreakdown.Select(b => b.RatePercent).Should().BeEquivalentTo(new[] { 10m, 20m });
        model.TaxBreakdown.Sum(b => b.TaxAmount).Should().Be(invoice.TaxTotal);
    }

    [Fact]
    public void Credit_note_title_uses_credit_note_label()
    {
        var invoice = BuildInvoice(InvoiceType.CreditNote, lineCount: 1);
        var tenant = BuildTenant();
        var customer = BuildCustomer();

        var model = invoice.ToInvoiceDocumentModel(tenant, customer, null);

        model.DocumentTitle.Should().Contain("Credit Note");
    }

    [Fact]
    public void Buyer_party_prefers_snapshot_over_customer_master_data()
    {
        var invoice = BuildInvoice(InvoiceType.SalesInvoice, lineCount: 1);
        invoice.ApplySnapshots(
            new CustomerSnapshot { LegalName = "Snapshot Co", TaxNumber = "9999999999" },
            new AddressSnapshot { Line1 = "Snapshot Street 1", City = "İstanbul", Country = "TR" },
            null);
        var tenant = BuildTenant();
        var customer = BuildCustomer();

        var model = invoice.ToInvoiceDocumentModel(tenant, customer, null);

        model.Buyer.LegalName.Should().Be("Snapshot Co");
        model.Buyer.TaxNumber.Should().Be("9999999999");
        model.Buyer.AddressLine1.Should().Be("Snapshot Street 1");
    }

    private static Invoice BuildInvoice(InvoiceType type, int lineCount)
    {
        var invoice = new Invoice("INV-DOC-0001", CustomerId, "Demo Müşteri", "TRY", type)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };

        var lines = new List<InvoiceLine>();
        for (var i = 1; i <= lineCount; i++)
        {
            var line = new InvoiceLine(Guid.NewGuid(), $"SKU-{i}", $"Item {i}", 2m, 50m)
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
            };
            line.SetLineNumber(i);
            line.ApplyPricing(
                quantity: 2m,
                unitPrice: 50m,
                lineDiscountPercent: 0m,
                lineDiscountAmount: 0m,
                taxRatePercent: 20m,
                taxRateId: null,
                isTaxInclusive: false,
                withholdingRatePercent: 0m,
                uomId: null,
                uomCode: "C62",
                description: null,
                revenueAccountCode: null,
                costCenter: null,
                project: null,
                originOrderLineId: null);
            lines.Add(line);
        }
        invoice.ReplaceLines(lines);
        return invoice;
    }

    private static Invoice BuildInvoiceWithMixedRates()
    {
        var invoice = new Invoice("INV-MIX-0001", CustomerId, "Demo Müşteri", "TRY", InvoiceType.SalesInvoice)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };

        var line1 = new InvoiceLine(Guid.NewGuid(), "SKU-A", "Item A", 1m, 100m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        line1.SetLineNumber(1);
        line1.ApplyPricing(1m, 100m, 0m, 0m, 20m, null, false, 0m, null, "EA", null, null, null, null, null);

        var line2 = new InvoiceLine(Guid.NewGuid(), "SKU-B", "Item B", 1m, 50m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        line2.SetLineNumber(2);
        line2.ApplyPricing(1m, 50m, 0m, 0m, 10m, null, false, 0m, null, "EA", null, null, null, null, null);

        invoice.ReplaceLines(new[] { line1, line2 });
        return invoice;
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
