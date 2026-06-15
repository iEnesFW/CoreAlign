using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Application.Quotes.DTOs;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Quotes.Handlers;

public static class QuoteMapper
{
    public static QuoteDto ToDto(Quote quote) => new()
    {
        Id = quote.Id,
        QuoteNumber = quote.QuoteNumber,
        Status = quote.Status,
        CustomerId = quote.CustomerId,
        CustomerName = quote.Customer?.Name ?? quote.CustomerSnapshot?.LegalName ?? string.Empty,
        BillingAddressId = quote.BillingAddressId,
        ShippingAddressId = quote.ShippingAddressId,
        CustomerSnapshot = quote.CustomerSnapshot != null ? ToDto(quote.CustomerSnapshot) : null,
        BillingAddressSnapshot = quote.BillingAddressSnapshot != null ? ToDto(quote.BillingAddressSnapshot) : null,
        ShippingAddressSnapshot = quote.ShippingAddressSnapshot != null ? ToDto(quote.ShippingAddressSnapshot) : null,
        QuoteDate = quote.QuoteDate,
        ValidUntilUtc = quote.ValidUntilUtc,
        SentAtUtc = quote.SentAtUtc,
        AcceptedAtUtc = quote.AcceptedAtUtc,
        RejectedAtUtc = quote.RejectedAtUtc,
        ExpiredAtUtc = quote.ExpiredAtUtc,
        ConvertedAtUtc = quote.ConvertedAtUtc,
        Currency = quote.Currency,
        ExchangeRate = quote.ExchangeRate,
        PaymentTermsId = quote.PaymentTermsId,
        PaymentTermsNetDaysSnapshot = quote.PaymentTermsNetDaysSnapshot,
        PriceListId = quote.PriceListId,
        SalesRepUserId = quote.SalesRepUserId,
        Subtotal = quote.Subtotal,
        LineDiscountTotal = quote.LineDiscountTotal,
        HeaderDiscountAmount = quote.HeaderDiscountAmount,
        HeaderDiscountPercent = quote.HeaderDiscountPercent,
        TaxableTotal = quote.TaxableTotal,
        TaxTotal = quote.TaxTotal,
        WithholdingTotal = quote.WithholdingTotal,
        ShippingCost = quote.ShippingCost,
        RoundingAdjustment = quote.RoundingAdjustment,
        Total = quote.Total,
        ConvertedOrderId = quote.ConvertedOrderId,
        RejectionReason = quote.RejectionReason,
        InternalNotes = quote.InternalNotes,
        CustomerNotes = quote.CustomerNotes,
        PublicNotes = quote.PublicNotes,
        TermsAndConditions = quote.TermsAndConditions,
        Notes = quote.Notes,
        Lines = quote.Lines.OrderBy(l => l.LineNumber).Select(ToLineDto).ToList(),
        CreatedAtUtc = quote.CreatedAtUtc,
        UpdatedAtUtc = quote.UpdatedAtUtc,
    };

    public static QuoteSummaryDto ToSummaryDto(Quote quote) => new()
    {
        Id = quote.Id,
        QuoteNumber = quote.QuoteNumber,
        CustomerId = quote.CustomerId,
        CustomerName = quote.Customer?.Name ?? quote.CustomerSnapshot?.LegalName ?? string.Empty,
        QuoteDate = quote.QuoteDate,
        ValidUntilUtc = quote.ValidUntilUtc,
        Status = quote.Status,
        Currency = quote.Currency,
        Total = quote.Total,
        ConvertedOrderId = quote.ConvertedOrderId,
    };

    public static QuoteSummaryDto ToSummaryDto(QuoteSearchRow row) => new()
    {
        Id = row.Id,
        QuoteNumber = row.QuoteNumber,
        CustomerId = row.CustomerId ?? Guid.Empty,
        CustomerName = row.CustomerName,
        QuoteDate = row.QuoteDate,
        ValidUntilUtc = row.ValidUntilUtc,
        Status = row.Status,
        Currency = row.Currency,
        Total = row.Total,
        ConvertedOrderId = row.ConvertedOrderId,
    };

    public static QuoteLineDto ToLineDto(QuoteLine line) => new()
    {
        Id = line.Id,
        LineNumber = line.LineNumber,
        ProductId = line.ProductId,
        ProductSku = line.ProductSku,
        ProductName = line.ProductName,
        ProductDescription = line.ProductDescriptionSnapshot,
        UomId = line.UomId,
        UomCode = line.UomCode,
        UomConversionFactor = line.UomConversionFactor,
        Quantity = line.Quantity,
        ListPriceSnapshot = line.ListPriceSnapshot,
        UnitPrice = line.UnitPrice,
        LineDiscountPercent = line.LineDiscountPercent,
        LineDiscountAmount = line.LineDiscountAmount,
        IsManualPriceOverride = line.IsManualPriceOverride,
        TaxRateId = line.TaxRateId,
        TaxRatePercent = line.TaxRatePercent,
        TaxAmount = line.TaxAmount,
        IsTaxInclusive = line.IsTaxInclusive,
        WithholdingRatePercent = line.WithholdingRatePercent,
        WithholdingAmount = line.WithholdingAmount,
        LineSubtotal = line.LineSubtotal,
        LineNetAmount = line.LineNetAmount,
        LineTotal = line.LineTotal,
        LineNotes = line.LineNotes,
    };

    private static CustomerSnapshotDto ToDto(CustomerSnapshot s) => new()
    {
        Code = s.Code,
        LegalName = s.LegalName,
        TradeName = s.TradeName,
        TaxNumber = s.TaxNumber,
        TaxOffice = s.TaxOffice,
        NationalId = s.NationalId,
        Email = s.Email,
        Phone = s.Phone,
    };

    private static AddressSnapshotDto ToDto(AddressSnapshot a) => new()
    {
        Label = a.Label,
        RecipientName = a.RecipientName,
        Phone = a.Phone,
        Line1 = a.Line1,
        Line2 = a.Line2,
        City = a.City,
        State = a.State,
        PostalCode = a.PostalCode,
        Country = a.Country,
    };
}
