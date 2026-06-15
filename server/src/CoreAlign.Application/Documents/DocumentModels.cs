namespace CoreAlign.Application.Documents;

public sealed record DocumentTenantHeader(
    string LegalName,
    string? TradeName,
    string? TaxNumber,
    string? TaxOffice,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateProvince,
    string? PostalCode,
    string? Country,
    string? Phone,
    string? Email,
    string? Website,
    string? LogoUrl,
    string TenantSlug);

public sealed record DocumentParty(
    string LegalName,
    string? TradeName,
    string? TaxNumber,
    string? TaxOffice,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateProvince,
    string? PostalCode,
    string? Country);

public sealed record DocumentLine(
    int LineNumber,
    string Sku,
    string Name,
    string? Description,
    decimal Quantity,
    string? UnitCode,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal TaxRatePercent,
    decimal TaxAmount,
    decimal LineNetAmount,
    decimal LineTotal);

public sealed record DocumentTaxBreakdown(decimal RatePercent, decimal TaxableBase, decimal TaxAmount);

public sealed record InvoiceDocumentModel(
    string DocumentTitle,
    string DocumentNumber,
    DateTime IssueDate,
    DateTime DueDate,
    string Currency,
    DocumentTenantHeader Tenant,
    DocumentParty Seller,
    DocumentParty Buyer,
    IReadOnlyList<DocumentLine> Lines,
    IReadOnlyList<DocumentTaxBreakdown> TaxBreakdown,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal WithholdingTotal,
    decimal ShippingCost,
    decimal RoundingAdjustment,
    decimal GrandTotal,
    string? PaymentTerms,
    string? PublicNotes,
    string? TermsAndConditions);

public sealed record OrderDocumentModel(
    string DocumentTitle,
    string OrderNumber,
    DateTime OrderDate,
    DateTime? RequestedDeliveryDate,
    string Currency,
    DocumentTenantHeader Tenant,
    DocumentParty Seller,
    DocumentParty Buyer,
    IReadOnlyList<DocumentLine> Lines,
    IReadOnlyList<DocumentTaxBreakdown> TaxBreakdown,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal ShippingCost,
    decimal GrandTotal,
    string? PaymentTerms,
    string? CustomerNotes);

public sealed record QuoteDocumentModel(
    string DocumentTitle,
    string QuoteNumber,
    DateTime QuoteDate,
    DateTime ValidUntilUtc,
    string Currency,
    DocumentTenantHeader Tenant,
    DocumentParty Seller,
    DocumentParty Buyer,
    IReadOnlyList<DocumentLine> Lines,
    IReadOnlyList<DocumentTaxBreakdown> TaxBreakdown,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal WithholdingTotal,
    decimal ShippingCost,
    decimal RoundingAdjustment,
    decimal GrandTotal,
    string? PaymentTerms,
    string? CustomerNotes,
    string? PublicNotes,
    string? TermsAndConditions);

public sealed record ShipmentDocumentLine(
    int LineNumber,
    string Sku,
    string Name,
    decimal Quantity,
    string? LotNumber,
    string? SerialNumber,
    string? Notes);

public sealed record DealerCommissionStatementLine(
    DateTime AccruedAtUtc,
    string OrderNumber,
    string? ShipmentNumber,
    string CustomerName,
    decimal OrderTotal,
    decimal CommissionPercent,
    decimal CommissionAmount,
    string Status);

public sealed record DealerCommissionStatementModel(
    string DocumentTitle,
    string DealerName,
    string? DealerCode,
    DateTime FromUtc,
    DateTime ToUtc,
    string Currency,
    DocumentTenantHeader Tenant,
    IReadOnlyList<DealerCommissionStatementLine> Lines,
    decimal TotalAccrued,
    decimal TotalPaid,
    decimal TotalOutstanding);

public sealed record ShipmentDocumentModel(
    string DocumentTitle,
    string ShipmentNumber,
    string OrderNumber,
    DateTime CreatedDate,
    DateTime? DispatchedAt,
    DocumentTenantHeader Tenant,
    DocumentParty Seller,
    DocumentParty Buyer,
    string? WarehouseName,
    string? CarrierName,
    string? TrackingNumber,
    string? TrackingUrl,
    IReadOnlyList<ShipmentDocumentLine> Lines,
    string? Notes);
