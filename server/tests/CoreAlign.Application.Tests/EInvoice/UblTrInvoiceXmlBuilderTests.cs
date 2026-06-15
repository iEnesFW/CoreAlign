using System.Xml.Linq;
using CoreAlign.Application.EInvoice;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.EInvoice;

public class UblTrInvoiceXmlBuilderTests
{
    private static readonly XNamespace InvoiceNs = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private static readonly XNamespace CreditNoteNs = "urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2";
    private static readonly XNamespace CbcNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace CacNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    [Fact]
    public void Builds_well_formed_invoice_with_required_ubl_elements()
    {
        var invoice = BuildInvoice(InvoiceType.SalesInvoice, lineCount: 2);
        var seller = new SellerParty("Acme Corp", "1234567890", null, "Kadıköy VD", "Atatürk Cd 1", "İstanbul", "34000", "Türkiye");
        var buyer = new BuyerParty("Demo Müşteri", "1234567890", null, "Beşiktaş VD", "Barbaros Bul 1", "İstanbul", "34000", "Türkiye");

        var xml = UblTrInvoiceXmlBuilder.Build(invoice, seller, buyer);

        var doc = XDocument.Parse(xml);
        doc.Root.Should().NotBeNull();
        doc.Root!.Name.Should().Be(InvoiceNs + "Invoice");

        doc.Root.Element(CbcNs + "UBLVersionID")!.Value.Should().Be("2.1");
        doc.Root.Element(CbcNs + "CustomizationID")!.Value.Should().Be("TR1.2");
        doc.Root.Element(CbcNs + "ProfileID")!.Value.Should().Be("TEMELFATURA");
        doc.Root.Element(CbcNs + "ID")!.Value.Should().Be(invoice.InvoiceNumber);
        doc.Root.Element(CbcNs + "InvoiceTypeCode")!.Value.Should().Be("SATIS");
        doc.Root.Element(CbcNs + "DocumentCurrencyCode")!.Value.Should().Be("TRY");

        doc.Root.Element(CacNs + "AccountingSupplierParty").Should().NotBeNull();
        doc.Root.Element(CacNs + "AccountingCustomerParty").Should().NotBeNull();
        doc.Root.Element(CacNs + "TaxTotal").Should().NotBeNull();
        doc.Root.Element(CacNs + "LegalMonetaryTotal").Should().NotBeNull();

        doc.Root.Elements(CacNs + "InvoiceLine").Should().HaveCount(2);
    }

    [Fact]
    public void Builds_credit_note_for_credit_note_type()
    {
        var invoice = BuildInvoice(InvoiceType.CreditNote, lineCount: 1);
        var seller = new SellerParty("Acme Corp", "1234567890", null, "Kadıköy VD", "Atatürk Cd 1", "İstanbul", "34000", "Türkiye");
        var buyer = new BuyerParty("Demo Müşteri", "1234567890", null, "Beşiktaş VD", "Barbaros Bul 1", "İstanbul", "34000", "Türkiye");

        var xml = UblTrInvoiceXmlBuilder.Build(invoice, seller, buyer);

        var doc = XDocument.Parse(xml);
        doc.Root!.Name.Should().Be(CreditNoteNs + "CreditNote");
        doc.Root.Element(CbcNs + "CreditNoteTypeCode")!.Value.Should().Be("381");
        doc.Root.Elements(CacNs + "CreditNoteLine").Should().HaveCount(1);
    }

    [Fact]
    public void Line_subtotals_match_invoice_line_net_amounts()
    {
        var invoice = BuildInvoice(InvoiceType.SalesInvoice, lineCount: 3);
        var seller = new SellerParty("Acme", "1234567890", null, null, null, null, null, null);
        var buyer = new BuyerParty("Buyer", "1234567890", null, null, null, null, null, null);

        var xml = UblTrInvoiceXmlBuilder.Build(invoice, seller, buyer);
        var doc = XDocument.Parse(xml);

        var lineExtensionSums = doc.Root!.Elements(CacNs + "InvoiceLine")
            .Select(l => decimal.Parse(l.Element(CbcNs + "LineExtensionAmount")!.Value, System.Globalization.CultureInfo.InvariantCulture))
            .Sum();
        var headerSubtotal = decimal.Parse(
            doc.Root.Element(CacNs + "LegalMonetaryTotal")!.Element(CbcNs + "LineExtensionAmount")!.Value,
            System.Globalization.CultureInfo.InvariantCulture);

        Math.Round(lineExtensionSums, 2).Should().Be(Math.Round(headerSubtotal, 2));
    }

    [Fact]
    public void Uses_tckn_scheme_for_individual_buyer()
    {
        var invoice = BuildInvoice(InvoiceType.SalesInvoice, lineCount: 1);
        var seller = new SellerParty("Acme", "1234567890", null, null, null, null, null, null);
        var buyer = new BuyerParty("Individual Buyer", null, "10000000146", null, null, null, null, null);

        var xml = UblTrInvoiceXmlBuilder.Build(invoice, seller, buyer);
        var doc = XDocument.Parse(xml);

        var idElement = doc.Root!
            .Element(CacNs + "AccountingCustomerParty")!
            .Element(CacNs + "Party")!
            .Element(CacNs + "PartyIdentification")!
            .Element(CbcNs + "ID")!;
        idElement.Attribute("schemeID")!.Value.Should().Be("TCKN");
        idElement.Value.Should().Be("10000000146");
    }

    private static Invoice BuildInvoice(InvoiceType type, int lineCount)
    {
        var invoice = new Invoice("INV-0001", CustomerId, "Demo Müşteri", "TRY", type)
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
}
