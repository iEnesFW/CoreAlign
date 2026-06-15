using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Documents;

public static class DocumentMappers
{
    public static DocumentTenantHeader ToHeader(this Tenant tenant) => new(
        LegalName: string.IsNullOrWhiteSpace(tenant.LegalName) ? tenant.Name : tenant.LegalName!,
        TradeName: tenant.TradeName,
        TaxNumber: tenant.TaxNumber,
        TaxOffice: tenant.TaxOffice,
        AddressLine1: tenant.AddressLine1,
        AddressLine2: tenant.AddressLine2,
        City: tenant.City,
        StateProvince: tenant.StateProvince,
        PostalCode: tenant.PostalCode,
        Country: tenant.Country,
        Phone: tenant.Phone,
        Email: tenant.Email,
        Website: tenant.Website,
        LogoUrl: tenant.LogoUrl,
        TenantSlug: tenant.Slug);

    public static DocumentParty ToSellerParty(this Tenant tenant) => new(
        LegalName: string.IsNullOrWhiteSpace(tenant.LegalName) ? tenant.Name : tenant.LegalName!,
        TradeName: tenant.TradeName,
        TaxNumber: tenant.TaxNumber,
        TaxOffice: tenant.TaxOffice,
        Email: tenant.Email,
        Phone: tenant.Phone,
        AddressLine1: tenant.AddressLine1,
        AddressLine2: tenant.AddressLine2,
        City: tenant.City,
        StateProvince: tenant.StateProvince,
        PostalCode: tenant.PostalCode,
        Country: tenant.Country);

    public static DocumentParty ToBuyerParty(
        CustomerSnapshot? customerSnapshot,
        AddressSnapshot? billingAddressSnapshot,
        Customer fallbackCustomer)
    {
        var legalName = customerSnapshot?.LegalName
            ?? (string.IsNullOrWhiteSpace(fallbackCustomer.LegalName) ? fallbackCustomer.Name : fallbackCustomer.LegalName!);
        var tradeName = customerSnapshot?.TradeName ?? fallbackCustomer.TradeName;
        var taxNumber = customerSnapshot?.TaxNumber ?? fallbackCustomer.TaxNumber;
        var taxOffice = customerSnapshot?.TaxOffice ?? fallbackCustomer.TaxOffice;
        var email = customerSnapshot?.Email ?? fallbackCustomer.Email;
        var phone = customerSnapshot?.Phone ?? fallbackCustomer.Phone;

        return new DocumentParty(
            LegalName: legalName,
            TradeName: tradeName,
            TaxNumber: taxNumber,
            TaxOffice: taxOffice,
            Email: email,
            Phone: phone,
            AddressLine1: billingAddressSnapshot?.Line1,
            AddressLine2: billingAddressSnapshot?.Line2,
            City: billingAddressSnapshot?.City,
            StateProvince: billingAddressSnapshot?.State,
            PostalCode: billingAddressSnapshot?.PostalCode,
            Country: billingAddressSnapshot?.Country);
    }

    public static InvoiceDocumentModel ToInvoiceDocumentModel(
        this Invoice invoice,
        Tenant tenant,
        Customer customer,
        PaymentTerm? paymentTerms)
    {
        var lines = invoice.Lines
            .OrderBy(l => l.LineNumber)
            .Select((l, idx) => new DocumentLine(
                LineNumber: l.LineNumber > 0 ? l.LineNumber : idx + 1,
                Sku: l.ProductSku,
                Name: l.ProductName,
                Description: l.Description,
                Quantity: l.Quantity,
                UnitCode: l.UomCode,
                UnitPrice: l.UnitPrice,
                DiscountAmount: l.LineDiscountAmount + (l.LineSubtotal * (l.LineDiscountPercent / 100m)),
                TaxRatePercent: l.TaxRatePercent,
                TaxAmount: l.TaxAmount,
                LineNetAmount: l.LineNetAmount,
                LineTotal: l.LineTotal))
            .ToList();

        var breakdown = invoice.Lines
            .GroupBy(l => l.TaxRatePercent)
            .Select(g => new DocumentTaxBreakdown(
                RatePercent: g.Key,
                TaxableBase: Math.Round(g.Sum(l => l.LineNetAmount), 4),
                TaxAmount: Math.Round(g.Sum(l => l.TaxAmount), 4)))
            .OrderBy(b => b.RatePercent)
            .ToList();

        var title = invoice.Type switch
        {
            InvoiceType.CreditNote => "Credit Note / İade Faturası",
            InvoiceType.DebitNote => "Debit Note / Borç Dekontu",
            InvoiceType.ProForma => "Proforma Invoice / Proforma Fatura",
            _ => "Invoice / Fatura"
        };

        return new InvoiceDocumentModel(
            DocumentTitle: title,
            DocumentNumber: invoice.InvoiceNumber,
            IssueDate: invoice.IssueDate,
            DueDate: invoice.DueDate,
            Currency: invoice.Currency,
            Tenant: tenant.ToHeader(),
            Seller: tenant.ToSellerParty(),
            Buyer: ToBuyerParty(invoice.CustomerSnapshot, invoice.BillingAddressSnapshot, customer),
            Lines: lines,
            TaxBreakdown: breakdown,
            Subtotal: invoice.Subtotal,
            DiscountTotal: invoice.LineDiscountTotal + invoice.HeaderDiscountAmount,
            TaxTotal: invoice.TaxTotal,
            WithholdingTotal: invoice.WithholdingTotal,
            ShippingCost: invoice.ShippingCost,
            RoundingAdjustment: invoice.RoundingAdjustment,
            GrandTotal: invoice.Total,
            PaymentTerms: FormatPaymentTerms(paymentTerms, invoice.PaymentTermsNetDaysSnapshot),
            PublicNotes: invoice.PublicNotes ?? invoice.Notes,
            TermsAndConditions: invoice.TermsAndConditions);
    }

    public static OrderDocumentModel ToOrderDocumentModel(
        this Order order,
        Tenant tenant,
        Customer customer,
        PaymentTerm? paymentTerms)
    {
        var lines = order.Lines
            .OrderBy(l => l.LineNumber)
            .Select((l, idx) => new DocumentLine(
                LineNumber: l.LineNumber > 0 ? l.LineNumber : idx + 1,
                Sku: l.ProductSku,
                Name: l.ProductName,
                Description: l.ProductDescriptionSnapshot,
                Quantity: l.Quantity,
                UnitCode: l.UomCode,
                UnitPrice: l.UnitPrice,
                DiscountAmount: l.LineDiscountAmount + (l.LineSubtotal * (l.LineDiscountPercent / 100m)),
                TaxRatePercent: l.TaxRatePercent,
                TaxAmount: l.TaxAmount,
                LineNetAmount: l.LineNetAmount,
                LineTotal: l.LineTotal))
            .ToList();

        var breakdown = order.Lines
            .GroupBy(l => l.TaxRatePercent)
            .Select(g => new DocumentTaxBreakdown(
                RatePercent: g.Key,
                TaxableBase: Math.Round(g.Sum(l => l.LineNetAmount), 4),
                TaxAmount: Math.Round(g.Sum(l => l.LineTaxAmount), 4)))
            .OrderBy(b => b.RatePercent)
            .ToList();

        return new OrderDocumentModel(
            DocumentTitle: "Order Confirmation / Sipariş Onayı",
            OrderNumber: order.OrderNumber,
            OrderDate: order.OrderDate,
            RequestedDeliveryDate: order.RequestedDeliveryDate,
            Currency: order.Currency,
            Tenant: tenant.ToHeader(),
            Seller: tenant.ToSellerParty(),
            Buyer: ToBuyerParty(order.CustomerSnapshot, order.BillingAddressSnapshot, customer),
            Lines: lines,
            TaxBreakdown: breakdown,
            Subtotal: order.Subtotal,
            DiscountTotal: order.LineDiscountTotal + order.HeaderDiscountAmount,
            TaxTotal: order.TaxTotal,
            ShippingCost: order.ShippingCost,
            GrandTotal: order.Total,
            PaymentTerms: FormatPaymentTerms(paymentTerms, order.PaymentTermsNetDaysSnapshot),
            CustomerNotes: order.CustomerNotes ?? order.Notes);
    }

    public static QuoteDocumentModel ToQuoteDocumentModel(
        this Quote quote,
        Tenant tenant,
        Customer customer,
        PaymentTerm? paymentTerms)
    {
        var lines = quote.Lines
            .OrderBy(l => l.LineNumber)
            .Select((l, idx) => new DocumentLine(
                LineNumber: l.LineNumber > 0 ? l.LineNumber : idx + 1,
                Sku: l.ProductSku,
                Name: l.ProductName,
                Description: l.ProductDescriptionSnapshot,
                Quantity: l.Quantity,
                UnitCode: l.UomCode,
                UnitPrice: l.UnitPrice,
                DiscountAmount: l.LineDiscountAmount + (l.LineSubtotal * (l.LineDiscountPercent / 100m)),
                TaxRatePercent: l.TaxRatePercent,
                TaxAmount: l.TaxAmount,
                LineNetAmount: l.LineNetAmount,
                LineTotal: l.LineTotal))
            .ToList();

        var breakdown = quote.Lines
            .GroupBy(l => l.TaxRatePercent)
            .Select(g => new DocumentTaxBreakdown(
                RatePercent: g.Key,
                TaxableBase: Math.Round(g.Sum(l => l.LineNetAmount), 4),
                TaxAmount: Math.Round(g.Sum(l => l.LineTaxAmount), 4)))
            .OrderBy(b => b.RatePercent)
            .ToList();

        return new QuoteDocumentModel(
            DocumentTitle: "Quote / Teklif",
            QuoteNumber: quote.QuoteNumber,
            QuoteDate: quote.QuoteDate,
            ValidUntilUtc: quote.ValidUntilUtc,
            Currency: quote.Currency,
            Tenant: tenant.ToHeader(),
            Seller: tenant.ToSellerParty(),
            Buyer: ToBuyerParty(quote.CustomerSnapshot, quote.BillingAddressSnapshot, customer),
            Lines: lines,
            TaxBreakdown: breakdown,
            Subtotal: quote.Subtotal,
            DiscountTotal: quote.LineDiscountTotal + quote.HeaderDiscountAmount,
            TaxTotal: quote.TaxTotal,
            WithholdingTotal: quote.WithholdingTotal,
            ShippingCost: quote.ShippingCost,
            RoundingAdjustment: quote.RoundingAdjustment,
            GrandTotal: quote.Total,
            PaymentTerms: FormatPaymentTerms(paymentTerms, quote.PaymentTermsNetDaysSnapshot),
            CustomerNotes: quote.CustomerNotes ?? quote.Notes,
            PublicNotes: quote.PublicNotes,
            TermsAndConditions: quote.TermsAndConditions);
    }

    public static ShipmentDocumentModel ToShipmentDocumentModel(
        this Shipment shipment,
        Order order,
        Tenant tenant,
        Customer customer,
        Warehouse? warehouse)
    {
        var lines = shipment.Lines
            .Select((l, idx) => new ShipmentDocumentLine(
                LineNumber: idx + 1,
                Sku: l.ProductSku,
                Name: l.ProductName,
                Quantity: l.Quantity,
                LotNumber: l.Lot?.LotNumber,
                SerialNumber: l.SerialNumber,
                Notes: l.Notes))
            .ToList();

        return new ShipmentDocumentModel(
            DocumentTitle: "Packing Slip / İrsaliye",
            ShipmentNumber: shipment.ShipmentNumber,
            OrderNumber: order.OrderNumber,
            CreatedDate: shipment.CreatedDate,
            DispatchedAt: shipment.DispatchedAtUtc,
            Tenant: tenant.ToHeader(),
            Seller: tenant.ToSellerParty(),
            Buyer: ToBuyerParty(order.CustomerSnapshot, shipment.ShippingAddressSnapshot ?? order.ShippingAddressSnapshot, customer),
            WarehouseName: warehouse?.Name,
            CarrierName: shipment.CarrierName,
            TrackingNumber: shipment.TrackingNumber,
            TrackingUrl: shipment.TrackingUrl,
            Lines: lines,
            Notes: shipment.Notes);
    }

    private static string? FormatPaymentTerms(PaymentTerm? term, int? netDaysSnapshot)
    {
        if (term is not null)
        {
            return $"{term.Name} (Net {term.NetDays})";
        }
        return netDaysSnapshot.HasValue ? $"Net {netDaysSnapshot.Value}" : null;
    }
}
