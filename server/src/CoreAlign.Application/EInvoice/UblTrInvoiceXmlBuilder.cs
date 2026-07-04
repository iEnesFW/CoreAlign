using System.Globalization;
using System.Xml.Linq;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.EInvoice;

public static class UblTrInvoiceXmlBuilder
{
    private static readonly XNamespace InvoiceNs = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private static readonly XNamespace CreditNoteNs = "urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2";
    private static readonly XNamespace CbcNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace CacNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

    private static readonly XNamespace SellerXsi = "http://www.w3.org/2001/XMLSchema-instance";

    public static string Build(Invoice invoice, SellerParty seller, BuyerParty buyer)
    {
        if (invoice is null) throw new ArgumentNullException(nameof(invoice));
        if (seller is null) throw new ArgumentNullException(nameof(seller));
        if (buyer is null) throw new ArgumentNullException(nameof(buyer));

        var isCreditNote = invoice.Type == InvoiceType.CreditNote;
        return isCreditNote
            ? BuildCreditNote(invoice, seller, buyer)
            : BuildInvoice(invoice, seller, buyer);
    }

    private static string BuildInvoice(Invoice invoice, SellerParty seller, BuyerParty buyer)
    {
        var profileId = string.IsNullOrWhiteSpace(invoice.EInvoiceProfile) ? "TEMELFATURA" : invoice.EInvoiceProfile;
        var root = new XElement(InvoiceNs + "Invoice",
            new XAttribute(XNamespace.Xmlns + "cbc", CbcNs.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "cac", CacNs.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "xsi", SellerXsi.NamespaceName),
            new XElement(CbcNs + "UBLVersionID", "2.1"),
            new XElement(CbcNs + "CustomizationID", "TR1.2"),
            new XElement(CbcNs + "ProfileID", profileId),
            new XElement(CbcNs + "ID", invoice.InvoiceNumber),
            new XElement(CbcNs + "UUID", invoice.Id.ToString()),
            new XElement(CbcNs + "IssueDate", FormatDate(invoice.IssueDate)),
            new XElement(CbcNs + "IssueTime", FormatTime(invoice.IssueDate)),
            new XElement(CbcNs + "InvoiceTypeCode", ResolveInvoiceTypeCode(invoice)),
            new XElement(CbcNs + "DocumentCurrencyCode", invoice.Currency),
            new XElement(CbcNs + "LineCountNumeric", invoice.Lines.Count.ToString(CultureInfo.InvariantCulture)),
            BuildSupplierParty(seller),
            BuildCustomerParty(buyer),
            BuildTaxTotal(invoice));

        var withholdingTotal = BuildWithholdingTaxTotal(invoice);
        if (withholdingTotal is not null)
        {
            root.Add(withholdingTotal);
        }

        root.Add(BuildLegalMonetaryTotal(invoice));

        foreach (var line in invoice.Lines.OrderBy(l => l.LineNumber))
        {
            root.Add(BuildInvoiceLine(line, invoice.Currency, invoice.VatExemptionCode, invoice.VatExemptionReason));
        }

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private static string BuildCreditNote(Invoice invoice, SellerParty seller, BuyerParty buyer)
    {
        var root = new XElement(CreditNoteNs + "CreditNote",
            new XAttribute(XNamespace.Xmlns + "cbc", CbcNs.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "cac", CacNs.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "xsi", SellerXsi.NamespaceName),
            new XElement(CbcNs + "UBLVersionID", "2.1"),
            new XElement(CbcNs + "CustomizationID", "TR1.2"),
            new XElement(CbcNs + "ProfileID", "TEMELFATURA"),
            new XElement(CbcNs + "ID", invoice.InvoiceNumber),
            new XElement(CbcNs + "UUID", invoice.Id.ToString()),
            new XElement(CbcNs + "IssueDate", FormatDate(invoice.IssueDate)),
            new XElement(CbcNs + "IssueTime", FormatTime(invoice.IssueDate)),
            new XElement(CbcNs + "CreditNoteTypeCode", "381"),
            new XElement(CbcNs + "DocumentCurrencyCode", invoice.Currency),
            new XElement(CbcNs + "LineCountNumeric", invoice.Lines.Count.ToString(CultureInfo.InvariantCulture)),
            BuildSupplierParty(seller),
            BuildCustomerParty(buyer),
            BuildTaxTotal(invoice),
            BuildLegalMonetaryTotal(invoice));

        foreach (var line in invoice.Lines.OrderBy(l => l.LineNumber))
        {
            root.Add(BuildCreditNoteLine(line, invoice.Currency));
        }

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private static XElement BuildSupplierParty(SellerParty seller) =>
        new(CacNs + "AccountingSupplierParty",
            new XElement(CacNs + "Party",
                new XElement(CacNs + "PartyIdentification",
                    new XElement(CbcNs + "ID",
                        new XAttribute("schemeID", string.IsNullOrEmpty(seller.NationalId) ? "VKN" : "TCKN"),
                        string.IsNullOrEmpty(seller.NationalId) ? seller.TaxNumber : seller.NationalId)),
                new XElement(CacNs + "PartyName",
                    new XElement(CbcNs + "Name", seller.Name)),
                BuildPostalAddress(seller.AddressLine, seller.City, seller.PostalCode, seller.Country),
                new XElement(CacNs + "PartyTaxScheme",
                    new XElement(CacNs + "TaxScheme",
                        new XElement(CbcNs + "Name", seller.TaxOffice ?? string.Empty)))));

    private static XElement BuildCustomerParty(BuyerParty buyer) =>
        new(CacNs + "AccountingCustomerParty",
            new XElement(CacNs + "Party",
                new XElement(CacNs + "PartyIdentification",
                    new XElement(CbcNs + "ID",
                        new XAttribute("schemeID", string.IsNullOrEmpty(buyer.NationalId) ? "VKN" : "TCKN"),
                        string.IsNullOrEmpty(buyer.NationalId) ? buyer.TaxNumber ?? string.Empty : buyer.NationalId)),
                new XElement(CacNs + "PartyName",
                    new XElement(CbcNs + "Name", buyer.Name)),
                BuildPostalAddress(buyer.AddressLine, buyer.City, buyer.PostalCode, buyer.Country),
                new XElement(CacNs + "PartyTaxScheme",
                    new XElement(CacNs + "TaxScheme",
                        new XElement(CbcNs + "Name", buyer.TaxOffice ?? string.Empty)))));

    private static XElement BuildPostalAddress(string? line, string? city, string? postal, string? country) =>
        new(CacNs + "PostalAddress",
            new XElement(CbcNs + "StreetName", line ?? string.Empty),
            new XElement(CbcNs + "CityName", city ?? string.Empty),
            new XElement(CbcNs + "PostalZone", postal ?? string.Empty),
            new XElement(CacNs + "Country",
                new XElement(CbcNs + "Name", country ?? "Türkiye")));

    private static XElement BuildTaxTotal(Invoice invoice)
    {
        var totalTax = invoice.TaxTotal;
        var subtotals = invoice.Lines
            .GroupBy(l => l.TaxRatePercent)
            .Select(g => new
            {
                Rate = g.Key,
                Base = Math.Round(g.Sum(l => l.LineNetAmount), 2),
                Amount = Math.Round(g.Sum(l => l.TaxAmount), 2),
            })
            .OrderBy(s => s.Rate)
            .ToList();

        var element = new XElement(CacNs + "TaxTotal",
            new XElement(CbcNs + "TaxAmount",
                new XAttribute("currencyID", invoice.Currency),
                FormatAmount(totalTax)));

        foreach (var s in subtotals)
        {
            element.Add(new XElement(CacNs + "TaxSubtotal",
                new XElement(CbcNs + "TaxableAmount",
                    new XAttribute("currencyID", invoice.Currency),
                    FormatAmount(s.Base)),
                new XElement(CbcNs + "TaxAmount",
                    new XAttribute("currencyID", invoice.Currency),
                    FormatAmount(s.Amount)),
                new XElement(CbcNs + "Percent", FormatAmount(s.Rate)),
                new XElement(CacNs + "TaxCategory",
                    new XElement(CacNs + "TaxScheme",
                        new XElement(CbcNs + "Name", "KDV"),
                        new XElement(CbcNs + "TaxTypeCode", "0015")))));
        }

        return element;
    }

    private static XElement? BuildWithholdingTaxTotal(Invoice invoice)
    {
        if (invoice.WithholdingTotal <= 0m)
        {
            return null;
        }

        var element = new XElement(CacNs + "WithholdingTaxTotal",
            new XElement(CbcNs + "TaxAmount",
                new XAttribute("currencyID", invoice.Currency),
                FormatAmount(invoice.WithholdingTotal)));

        var byCode = invoice.Lines
            .Where(l => l.WithholdingAmount > 0m)
            .GroupBy(l => l.WithholdingCode ?? string.Empty)
            .Select(g => new
            {
                Code = g.Key,
                Base = Math.Round(g.Sum(l => l.TaxAmount), 2),
                Amount = Math.Round(g.Sum(l => l.WithholdingAmount), 2),
            })
            .OrderBy(x => x.Code);

        foreach (var w in byCode)
        {
            var taxCategory = new XElement(CacNs + "TaxCategory");
            if (!string.IsNullOrEmpty(w.Code))
            {
                taxCategory.Add(new XElement(CbcNs + "TaxExemptionReasonCode", w.Code));
            }
            taxCategory.Add(new XElement(CacNs + "TaxScheme",
                new XElement(CbcNs + "Name", "KDV Tevkifatı"),
                new XElement(CbcNs + "TaxTypeCode", "0021")));

            element.Add(new XElement(CacNs + "TaxSubtotal",
                new XElement(CbcNs + "TaxableAmount",
                    new XAttribute("currencyID", invoice.Currency), FormatAmount(w.Base)),
                new XElement(CbcNs + "TaxAmount",
                    new XAttribute("currencyID", invoice.Currency), FormatAmount(w.Amount)),
                taxCategory));
        }

        return element;
    }

    private static XElement BuildLegalMonetaryTotal(Invoice invoice) =>
        new(CacNs + "LegalMonetaryTotal",
            new XElement(CbcNs + "LineExtensionAmount",
                new XAttribute("currencyID", invoice.Currency), FormatAmount(invoice.Subtotal)),
            new XElement(CbcNs + "TaxExclusiveAmount",
                new XAttribute("currencyID", invoice.Currency), FormatAmount(invoice.TaxableTotal)),
            new XElement(CbcNs + "TaxInclusiveAmount",
                new XAttribute("currencyID", invoice.Currency),
                FormatAmount(invoice.TaxableTotal + invoice.TaxTotal)),
            new XElement(CbcNs + "AllowanceTotalAmount",
                new XAttribute("currencyID", invoice.Currency),
                FormatAmount(invoice.LineDiscountTotal + invoice.HeaderDiscountAmount)),
            new XElement(CbcNs + "PayableAmount",
                new XAttribute("currencyID", invoice.Currency), FormatAmount(invoice.Total)));

    private static XElement BuildInvoiceLine(InvoiceLine line, string currency, string? exemptionCode, string? exemptionReason)
    {
        var element = new XElement(CacNs + "InvoiceLine",
            new XElement(CbcNs + "ID", line.LineNumber.ToString(CultureInfo.InvariantCulture)),
            new XElement(CbcNs + "InvoicedQuantity",
                new XAttribute("unitCode", line.UomCode ?? "C62"), FormatAmount(line.Quantity)),
            new XElement(CbcNs + "LineExtensionAmount",
                new XAttribute("currencyID", currency), FormatAmount(line.LineNetAmount)));

        if (line.LineDiscountAmount > 0m || line.LineDiscountPercent > 0m)
        {
            element.Add(new XElement(CacNs + "AllowanceCharge",
                new XElement(CbcNs + "ChargeIndicator", "false"),
                new XElement(CbcNs + "MultiplierFactorNumeric", FormatAmount(line.LineDiscountPercent / 100m)),
                new XElement(CbcNs + "Amount",
                    new XAttribute("currencyID", currency), FormatAmount(line.LineDiscountAmount))));
        }

        element.Add(BuildLineTaxTotal(line, currency, exemptionCode, exemptionReason));
        element.Add(new XElement(CacNs + "Item",
            new XElement(CbcNs + "Name", line.ProductName),
            new XElement(CbcNs + "SellersItemIdentification",
                new XElement(CbcNs + "ID", line.ProductSku))));
        element.Add(new XElement(CacNs + "Price",
            new XElement(CbcNs + "PriceAmount",
                new XAttribute("currencyID", currency), FormatAmount(line.UnitPrice))));
        return element;
    }

    private static XElement BuildCreditNoteLine(InvoiceLine line, string currency) =>
        new(CacNs + "CreditNoteLine",
            new XElement(CbcNs + "ID", line.LineNumber.ToString(CultureInfo.InvariantCulture)),
            new XElement(CbcNs + "CreditedQuantity",
                new XAttribute("unitCode", line.UomCode ?? "C62"), FormatAmount(line.Quantity)),
            new XElement(CbcNs + "LineExtensionAmount",
                new XAttribute("currencyID", currency), FormatAmount(line.LineNetAmount)),
            BuildLineTaxTotal(line, currency, exemptionCode: null, exemptionReason: null),
            new XElement(CacNs + "Item",
                new XElement(CbcNs + "Name", line.ProductName),
                new XElement(CbcNs + "SellersItemIdentification",
                    new XElement(CbcNs + "ID", line.ProductSku))),
            new XElement(CacNs + "Price",
                new XElement(CbcNs + "PriceAmount",
                    new XAttribute("currencyID", currency), FormatAmount(line.UnitPrice))));

    private static XElement BuildLineTaxTotal(InvoiceLine line, string currency, string? exemptionCode, string? exemptionReason)
    {
        var taxCategory = new XElement(CacNs + "TaxCategory");

        // WHY: KDV oranı 0 ve istisna kodu varsa GİB TaxExemptionReasonCode/Reason bekler.
        if (line.TaxRatePercent == 0m && !string.IsNullOrEmpty(exemptionCode))
        {
            taxCategory.Add(new XElement(CbcNs + "TaxExemptionReasonCode", exemptionCode));
            if (!string.IsNullOrEmpty(exemptionReason))
            {
                taxCategory.Add(new XElement(CbcNs + "TaxExemptionReason", exemptionReason));
            }
        }

        taxCategory.Add(new XElement(CacNs + "TaxScheme",
            new XElement(CbcNs + "Name", "KDV"),
            new XElement(CbcNs + "TaxTypeCode", "0015")));

        return new XElement(CacNs + "TaxTotal",
            new XElement(CbcNs + "TaxAmount",
                new XAttribute("currencyID", currency), FormatAmount(line.TaxAmount)),
            new XElement(CacNs + "TaxSubtotal",
                new XElement(CbcNs + "TaxableAmount",
                    new XAttribute("currencyID", currency), FormatAmount(line.LineNetAmount)),
                new XElement(CbcNs + "TaxAmount",
                    new XAttribute("currencyID", currency), FormatAmount(line.TaxAmount)),
                new XElement(CbcNs + "Percent", FormatAmount(line.TaxRatePercent)),
                taxCategory));
    }

    private static string ResolveInvoiceTypeCode(Invoice invoice)
    {
        if (invoice.Lines.Any(l => l.WithholdingAmount > 0m))
        {
            return "TEVKIFAT";
        }

        if (!string.IsNullOrEmpty(invoice.VatExemptionCode))
        {
            return "ISTISNA";
        }

        return MapInvoiceTypeCode(invoice.Type);
    }

    private static string MapInvoiceTypeCode(InvoiceType type) => type switch
    {
        InvoiceType.SalesInvoice => "SATIS",
        InvoiceType.ProForma => "ISTISNA",
        InvoiceType.CreditNote => "IADE",
        InvoiceType.DebitNote => "TEVKIFAT",
        InvoiceType.Advance => "AVANS",
        _ => "SATIS",
    };

    private static string FormatAmount(decimal value) =>
        Math.Round(value, 2).ToString("0.00", CultureInfo.InvariantCulture);

    private static string FormatDate(DateTime value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatTime(DateTime value) =>
        value.ToUniversalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture);
}

public record SellerParty(
    string Name,
    string? TaxNumber,
    string? NationalId,
    string? TaxOffice,
    string? AddressLine,
    string? City,
    string? PostalCode,
    string? Country);

public record BuyerParty(
    string Name,
    string? TaxNumber,
    string? NationalId,
    string? TaxOffice,
    string? AddressLine,
    string? City,
    string? PostalCode,
    string? Country);
