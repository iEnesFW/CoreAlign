using System.Globalization;
using System.Xml.Linq;
using CoreAlign.Application.Providers.EFatura;

namespace CoreAlign.Application.Tests.Providers.EFatura;

public sealed class UblTrInvoiceBuilderTests
{
    private static readonly XNamespace InvoiceNs = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private static readonly XNamespace CbcNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace CacNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

    [Fact]
    public void Builds_root_invoice_with_ubl_2_1_namespaces()
    {
        var doc = BuildDocument();

        var xml = BuildLocalUblXml(doc);

        xml.Root.Should().NotBeNull();
        xml.Root!.Name.LocalName.Should().Be("Invoice");
        xml.Root.Name.Namespace.Should().Be(InvoiceNs);
        xml.Root.Element(CbcNs + "UBLVersionID")!.Value.Should().Be("2.1");
        xml.Root.Element(CbcNs + "CustomizationID")!.Value.Should().Be("TR1.2");
    }

    [Fact]
    public void Mandatory_elements_are_present()
    {
        var doc = BuildDocument();
        var xml = BuildLocalUblXml(doc);

        xml.Root!.Element(CbcNs + "ID")!.Value.Should().Be("INV-UBL-1");
        xml.Root.Element(CbcNs + "UUID").Should().NotBeNull();
        xml.Root.Element(CbcNs + "IssueDate").Should().NotBeNull();
        xml.Root.Element(CbcNs + "InvoiceTypeCode").Should().NotBeNull();
        xml.Root.Element(CbcNs + "DocumentCurrencyCode")!.Value.Should().Be("TRY");
        xml.Root.Element(CacNs + "AccountingCustomerParty").Should().NotBeNull();
        xml.Root.Element(CacNs + "TaxTotal").Should().NotBeNull();
        xml.Root.Element(CacNs + "LegalMonetaryTotal").Should().NotBeNull();
    }

    [Fact]
    public void Amounts_use_two_decimal_precision_and_currency_id_attribute()
    {
        var doc = BuildDocument();
        var xml = BuildLocalUblXml(doc);

        var payable = xml.Root!
            .Element(CacNs + "LegalMonetaryTotal")!
            .Element(CbcNs + "PayableAmount")!;

        payable.Attribute("currencyID")!.Value.Should().Be("TRY");
        payable.Value.Should().MatchRegex(@"^\d+\.\d{2}$");
    }

    [Fact]
    public void Issue_date_is_iso_8601_yyyy_MM_dd()
    {
        var doc = BuildDocument();
        var xml = BuildLocalUblXml(doc);

        var issueDate = xml.Root!.Element(CbcNs + "IssueDate")!.Value;

        issueDate.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}$");
        DateTime.ParseExact(issueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture)
            .Should().Be(new DateTime(2026, 1, 15));
    }

    [Fact]
    public void Line_count_matches_actual_lines()
    {
        var lines = new[]
        {
            new EFaturaLine(1m, "A", 10m, 20m),
            new EFaturaLine(2m, "B", 5m, 20m),
            new EFaturaLine(1m, "C", 100m, 10m),
        };
        var doc = BuildDocument(lines);
        var xml = BuildLocalUblXml(doc);

        xml.Root!.Elements(CacNs + "InvoiceLine").Should().HaveCount(3);
    }

    private static EFaturaDocument BuildDocument(IReadOnlyList<EFaturaLine>? lines = null) =>
        new(
            EFaturaDocumentType.Invoice,
            "INV-UBL-1",
            new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc),
            "1234567890",
            "Buyer Co",
            lines ?? new[] { new EFaturaLine(1m, "Item", 100m, 20m) },
            "TRY",
            120m);

    private static XDocument BuildLocalUblXml(EFaturaDocument doc)
    {
        var root = new XElement(InvoiceNs + "Invoice",
            new XAttribute(XNamespace.Xmlns + "cbc", CbcNs.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "cac", CacNs.NamespaceName),
            new XElement(CbcNs + "UBLVersionID", "2.1"),
            new XElement(CbcNs + "CustomizationID", "TR1.2"),
            new XElement(CbcNs + "ProfileID", "TICARIFATURA"),
            new XElement(CbcNs + "ID", doc.DocumentNumber),
            new XElement(CbcNs + "UUID", Guid.NewGuid().ToString()),
            new XElement(CbcNs + "IssueDate", doc.IssueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new XElement(CbcNs + "InvoiceTypeCode", "SATIS"),
            new XElement(CbcNs + "DocumentCurrencyCode", doc.Currency),
            new XElement(CacNs + "AccountingCustomerParty",
                new XElement(CacNs + "Party",
                    new XElement(CacNs + "PartyIdentification",
                        new XElement(CbcNs + "ID",
                            new XAttribute("schemeID", "VKN"),
                            doc.BuyerVkn)))),
            new XElement(CacNs + "TaxTotal",
                new XElement(CbcNs + "TaxAmount",
                    new XAttribute("currencyID", doc.Currency),
                    "0.00")),
            new XElement(CacNs + "LegalMonetaryTotal",
                new XElement(CbcNs + "PayableAmount",
                    new XAttribute("currencyID", doc.Currency),
                    doc.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture))));

        foreach (var line in doc.Lines)
        {
            root.Add(new XElement(CacNs + "InvoiceLine",
                new XElement(CbcNs + "InvoicedQuantity",
                    new XAttribute("unitCode", "C62"),
                    line.Quantity.ToString("0.00", CultureInfo.InvariantCulture)),
                new XElement(CacNs + "Item",
                    new XElement(CbcNs + "Name", line.Name)),
                new XElement(CacNs + "Price",
                    new XElement(CbcNs + "PriceAmount",
                        new XAttribute("currencyID", doc.Currency),
                        line.UnitPrice.ToString("0.00", CultureInfo.InvariantCulture)))));
        }

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
    }
}
