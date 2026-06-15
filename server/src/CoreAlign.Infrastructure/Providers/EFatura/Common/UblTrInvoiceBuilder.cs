using System.Globalization;
using System.Xml.Linq;
using CoreAlign.Application.Providers.EFatura;

namespace CoreAlign.Infrastructure.Providers.EFatura.Common;

public static class UblTrInvoiceBuilder
{
    private static readonly XNamespace InvoiceNs = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private static readonly XNamespace CreditNoteNs = "urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2";
    private static readonly XNamespace CbcNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace CacNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    private const string UblVersion = "2.1";
    private const string CustomizationId = "TR1.2";
    private const string DefaultProfileId = "TICARIFATURA";

    public static XDocument Build(EFaturaDocument document, string documentUuid)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentUuid);

        return document.Type == EFaturaDocumentType.EArchive
            ? BuildInvoiceDocument(document, documentUuid, "EARSIVFATURA")
            : BuildInvoiceDocument(document, documentUuid, DefaultProfileId);
    }

    public static XDocument BuildCreditNote(EFaturaDocument document, string documentUuid, string originalInvoiceUuid)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentUuid);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalInvoiceUuid);

        var root = new XElement(CreditNoteNs + "CreditNote",
            new XAttribute(XNamespace.Xmlns + "cbc", CbcNs.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "cac", CacNs.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName),
            new XElement(CbcNs + "UBLVersionID", UblVersion),
            new XElement(CbcNs + "CustomizationID", CustomizationId),
            new XElement(CbcNs + "ProfileID", DefaultProfileId),
            new XElement(CbcNs + "ID", document.DocumentNumber),
            new XElement(CbcNs + "UUID", documentUuid),
            new XElement(CbcNs + "IssueDate", FormatDate(document.IssueDate)),
            new XElement(CbcNs + "IssueTime", FormatTime(document.IssueDate)),
            new XElement(CbcNs + "CreditNoteTypeCode", "381"),
            new XElement(CbcNs + "DocumentCurrencyCode", document.Currency),
            new XElement(CbcNs + "LineCountNumeric", document.Lines.Count.ToString(CultureInfo.InvariantCulture)),
            BuildBillingReference(originalInvoiceUuid),
            BuildSupplierParty(document),
            BuildCustomerParty(document),
            BuildTaxTotal(document),
            BuildLegalMonetaryTotal(document));

        for (var i = 0; i < document.Lines.Count; i++)
        {
            root.Add(BuildCreditNoteLine(document.Lines[i], i + 1, document.Currency));
        }

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
    }

    private static XDocument BuildInvoiceDocument(EFaturaDocument document, string documentUuid, string profileId)
    {
        var root = new XElement(InvoiceNs + "Invoice",
            new XAttribute(XNamespace.Xmlns + "cbc", CbcNs.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "cac", CacNs.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName),
            new XElement(CbcNs + "UBLVersionID", UblVersion),
            new XElement(CbcNs + "CustomizationID", CustomizationId),
            new XElement(CbcNs + "ProfileID", profileId),
            new XElement(CbcNs + "ID", document.DocumentNumber),
            new XElement(CbcNs + "UUID", documentUuid),
            new XElement(CbcNs + "IssueDate", FormatDate(document.IssueDate)),
            new XElement(CbcNs + "IssueTime", FormatTime(document.IssueDate)),
            new XElement(CbcNs + "InvoiceTypeCode", MapInvoiceTypeCode(document.Type)),
            new XElement(CbcNs + "DocumentCurrencyCode", document.Currency),
            new XElement(CbcNs + "LineCountNumeric", document.Lines.Count.ToString(CultureInfo.InvariantCulture)),
            BuildSupplierParty(document),
            BuildCustomerParty(document),
            BuildTaxTotal(document),
            BuildLegalMonetaryTotal(document));

        for (var i = 0; i < document.Lines.Count; i++)
        {
            root.Add(BuildInvoiceLine(document.Lines[i], i + 1, document.Currency));
        }

        return new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
    }

    private static XElement BuildBillingReference(string originalInvoiceUuid) =>
        new(CacNs + "BillingReference",
            new XElement(CacNs + "InvoiceDocumentReference",
                new XElement(CbcNs + "ID", originalInvoiceUuid),
                new XElement(CbcNs + "IssueDate", FormatDate(DateTime.UtcNow))));

    private static XElement BuildSupplierParty(EFaturaDocument _) =>
        new(CacNs + "AccountingSupplierParty",
            new XElement(CacNs + "Party",
                new XElement(CacNs + "PartyIdentification",
                    new XElement(CbcNs + "ID",
                        new XAttribute("schemeID", "VKN"),
                        string.Empty)),
                new XElement(CacNs + "PartyName",
                    new XElement(CbcNs + "Name", string.Empty)),
                new XElement(CacNs + "PostalAddress",
                    new XElement(CbcNs + "StreetName", string.Empty),
                    new XElement(CbcNs + "CityName", string.Empty),
                    new XElement(CacNs + "Country",
                        new XElement(CbcNs + "Name", "Türkiye"))),
                new XElement(CacNs + "PartyTaxScheme",
                    new XElement(CacNs + "TaxScheme",
                        new XElement(CbcNs + "Name", string.Empty)))));

    private static XElement BuildCustomerParty(EFaturaDocument document) =>
        new(CacNs + "AccountingCustomerParty",
            new XElement(CacNs + "Party",
                new XElement(CacNs + "PartyIdentification",
                    new XElement(CbcNs + "ID",
                        new XAttribute("schemeID", IsTckn(document.BuyerVkn) ? "TCKN" : "VKN"),
                        document.BuyerVkn)),
                new XElement(CacNs + "PartyName",
                    new XElement(CbcNs + "Name", document.BuyerName)),
                new XElement(CacNs + "PostalAddress",
                    new XElement(CbcNs + "StreetName", string.Empty),
                    new XElement(CbcNs + "CityName", string.Empty),
                    new XElement(CacNs + "Country",
                        new XElement(CbcNs + "Name", "Türkiye"))),
                new XElement(CacNs + "PartyTaxScheme",
                    new XElement(CacNs + "TaxScheme",
                        new XElement(CbcNs + "Name", string.Empty)))));

    private static XElement BuildTaxTotal(EFaturaDocument document)
    {
        var taxTotal = document.Lines.Sum(l => LineTaxAmount(l));

        var element = new XElement(CacNs + "TaxTotal",
            new XElement(CbcNs + "TaxAmount",
                new XAttribute("currencyID", document.Currency),
                FormatAmount(taxTotal)));

        foreach (var group in document.Lines.GroupBy(l => l.VatRate).OrderBy(g => g.Key))
        {
            var baseAmount = group.Sum(l => LineNet(l));
            var amount = group.Sum(l => LineTaxAmount(l));

            element.Add(new XElement(CacNs + "TaxSubtotal",
                new XElement(CbcNs + "TaxableAmount",
                    new XAttribute("currencyID", document.Currency),
                    FormatAmount(baseAmount)),
                new XElement(CbcNs + "TaxAmount",
                    new XAttribute("currencyID", document.Currency),
                    FormatAmount(amount)),
                new XElement(CbcNs + "Percent", FormatAmount(group.Key)),
                new XElement(CacNs + "TaxCategory",
                    new XElement(CacNs + "TaxScheme",
                        new XElement(CbcNs + "Name", "KDV"),
                        new XElement(CbcNs + "TaxTypeCode", "0015")))));
        }

        return element;
    }

    private static XElement BuildLegalMonetaryTotal(EFaturaDocument document)
    {
        var lineExtension = document.Lines.Sum(l => LineNet(l));
        var taxTotal = document.Lines.Sum(l => LineTaxAmount(l));
        var taxInclusive = lineExtension + taxTotal;

        return new XElement(CacNs + "LegalMonetaryTotal",
            new XElement(CbcNs + "LineExtensionAmount",
                new XAttribute("currencyID", document.Currency), FormatAmount(lineExtension)),
            new XElement(CbcNs + "TaxExclusiveAmount",
                new XAttribute("currencyID", document.Currency), FormatAmount(lineExtension)),
            new XElement(CbcNs + "TaxInclusiveAmount",
                new XAttribute("currencyID", document.Currency), FormatAmount(taxInclusive)),
            new XElement(CbcNs + "PayableAmount",
                new XAttribute("currencyID", document.Currency), FormatAmount(document.TotalAmount)));
    }

    private static XElement BuildInvoiceLine(EFaturaLine line, int lineNumber, string currency) =>
        new(CacNs + "InvoiceLine",
            new XElement(CbcNs + "ID", lineNumber.ToString(CultureInfo.InvariantCulture)),
            new XElement(CbcNs + "InvoicedQuantity",
                new XAttribute("unitCode", "C62"), FormatAmount(line.Quantity)),
            new XElement(CbcNs + "LineExtensionAmount",
                new XAttribute("currencyID", currency), FormatAmount(LineNet(line))),
            BuildLineTaxTotal(line, currency),
            new XElement(CacNs + "Item",
                new XElement(CbcNs + "Name", line.Name)),
            new XElement(CacNs + "Price",
                new XElement(CbcNs + "PriceAmount",
                    new XAttribute("currencyID", currency), FormatAmount(line.UnitPrice))));

    private static XElement BuildCreditNoteLine(EFaturaLine line, int lineNumber, string currency) =>
        new(CacNs + "CreditNoteLine",
            new XElement(CbcNs + "ID", lineNumber.ToString(CultureInfo.InvariantCulture)),
            new XElement(CbcNs + "CreditedQuantity",
                new XAttribute("unitCode", "C62"), FormatAmount(line.Quantity)),
            new XElement(CbcNs + "LineExtensionAmount",
                new XAttribute("currencyID", currency), FormatAmount(LineNet(line))),
            BuildLineTaxTotal(line, currency),
            new XElement(CacNs + "Item",
                new XElement(CbcNs + "Name", line.Name)),
            new XElement(CacNs + "Price",
                new XElement(CbcNs + "PriceAmount",
                    new XAttribute("currencyID", currency), FormatAmount(line.UnitPrice))));

    private static XElement BuildLineTaxTotal(EFaturaLine line, string currency) =>
        new(CacNs + "TaxTotal",
            new XElement(CbcNs + "TaxAmount",
                new XAttribute("currencyID", currency), FormatAmount(LineTaxAmount(line))),
            new XElement(CacNs + "TaxSubtotal",
                new XElement(CbcNs + "TaxableAmount",
                    new XAttribute("currencyID", currency), FormatAmount(LineNet(line))),
                new XElement(CbcNs + "TaxAmount",
                    new XAttribute("currencyID", currency), FormatAmount(LineTaxAmount(line))),
                new XElement(CbcNs + "Percent", FormatAmount(line.VatRate)),
                new XElement(CacNs + "TaxCategory",
                    new XElement(CacNs + "TaxScheme",
                        new XElement(CbcNs + "Name", "KDV"),
                        new XElement(CbcNs + "TaxTypeCode", "0015")))));

    private static decimal LineNet(EFaturaLine line) =>
        Math.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero);

    private static decimal LineTaxAmount(EFaturaLine line) =>
        Math.Round(LineNet(line) * (line.VatRate / 100m), 2, MidpointRounding.AwayFromZero);

    private static string MapInvoiceTypeCode(EFaturaDocumentType type) => type switch
    {
        EFaturaDocumentType.Invoice => "SATIS",
        EFaturaDocumentType.Despatch => "SATIS",
        EFaturaDocumentType.ProducerReceipt => "MUSTAHSIL",
        EFaturaDocumentType.EArchive => "SATIS",
        EFaturaDocumentType.SelfEmployedReceipt => "SMM",
        _ => "SATIS",
    };

    private static bool IsTckn(string identifier) =>
        !string.IsNullOrWhiteSpace(identifier) && identifier.Length == 11 && identifier.All(char.IsDigit);

    private static string FormatAmount(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("0.00", CultureInfo.InvariantCulture);

    private static string FormatDate(DateTime value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatTime(DateTime value) =>
        value.ToUniversalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture);
}
