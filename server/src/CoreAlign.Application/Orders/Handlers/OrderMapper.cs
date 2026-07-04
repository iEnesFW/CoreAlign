using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Orders.Handlers;

public static class OrderMapper
{
    public static OrderDto ToDto(Order order, string? originDealerName, string? dealerApprovedByName)
    {
        var dto = ToDto(order);
        dto.OriginDealerName = originDealerName;
        dto.DealerApprovedByName = dealerApprovedByName;
        return dto;
    }

    public static OrderDto ToDto(Order order) => new()
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        Type = order.Type,
        Status = order.Status,
        Source = order.Source,
        CustomerId = order.CustomerId,
        CustomerName = order.Customer?.Name ?? string.Empty,
        BillingAddressId = order.BillingAddressId,
        ShippingAddressId = order.ShippingAddressId,
        CustomerSnapshot = order.CustomerSnapshot != null ? ToDto(order.CustomerSnapshot) : null,
        BillingAddressSnapshot = order.BillingAddressSnapshot != null ? ToDto(order.BillingAddressSnapshot) : null,
        ShippingAddressSnapshot = order.ShippingAddressSnapshot != null ? ToDto(order.ShippingAddressSnapshot) : null,
        OrderDate = order.OrderDate,
        RequestedDeliveryDate = order.RequestedDeliveryDate,
        PromisedDeliveryDate = order.PromisedDeliveryDate,
        ActualDeliveryDate = order.ActualDeliveryDate,
        SubmittedAtUtc = order.SubmittedAtUtc,
        ApprovedAtUtc = order.ApprovedAtUtc,
        CancelledAtUtc = order.CancelledAtUtc,
        Currency = order.Currency,
        ExchangeRate = order.ExchangeRate,
        PaymentTermsId = order.PaymentTermsId,
        PaymentTermsNetDaysSnapshot = order.PaymentTermsNetDaysSnapshot,
        DueDate = order.DueDate,
        PriceListId = order.PriceListId,
        Subtotal = order.Subtotal,
        LineDiscountTotal = order.LineDiscountTotal,
        HeaderDiscountAmount = order.HeaderDiscountAmount,
        HeaderDiscountPercent = order.HeaderDiscountPercent,
        TaxableTotal = order.TaxableTotal,
        TaxTotal = order.TaxTotal,
        WithholdingTotal = order.WithholdingTotal,
        ShippingCost = order.ShippingCost,
        RoundingAdjustment = order.RoundingAdjustment,
        Total = order.Total,
        SalesRepUserId = order.SalesRepUserId,
        Channel = order.Channel,
        ApprovedByUserId = order.ApprovedByUserId,
        OriginOrderId = order.OriginOrderId,
        CancelReason = order.CancelReason,
        InternalNotes = order.InternalNotes,
        CustomerNotes = order.CustomerNotes,
        Notes = order.Notes,
        Lines = order.Lines.OrderBy(l => l.LineNumber).Select(ToLineDto).ToList(),
        CreatedAtUtc = order.CreatedAtUtc,
        UpdatedAtUtc = order.UpdatedAtUtc,
        OriginPersona = order.OriginPersona,
        DealerApprovalStatus = order.DealerApprovalStatus,
        OriginDealerAccountId = order.OriginDealerAccountId,
        OriginDealerUserId = order.OriginDealerUserId,
        OriginCustomerUserId = order.OriginCustomerUserId,
    };

    public static OrderSummaryDto ToSummaryDto(Order order) => new()
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        CustomerId = order.CustomerId,
        CustomerName = order.Customer?.Name ?? string.Empty,
        OrderDate = order.OrderDate,
        Status = order.Status,
        Currency = order.Currency,
        Total = order.Total
    };

    public static OrderSummaryDto ToSummaryDto(OrderSearchRow row) => new()
    {
        Id = row.Id,
        OrderNumber = row.OrderNumber,
        CustomerId = row.CustomerId,
        CustomerName = row.CustomerName,
        OrderDate = row.OrderDate,
        Status = row.Status,
        Currency = row.Currency,
        Total = row.Total,
        InvoiceId = row.InvoiceId,
        InvoiceNumber = row.InvoiceNumber,
        ShipmentId = row.ShipmentId,
        ShipmentNumber = row.ShipmentNumber,
    };

    public static OrderLineDto ToLineDto(OrderLine line) => new()
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
        QuantityAllocated = line.QuantityAllocated,
        QuantityShipped = line.QuantityShipped,
        QuantityInvoiced = line.QuantityInvoiced,
        QuantityReturned = line.QuantityReturned,
        QuantityCancelled = line.QuantityCancelled,
        QuantityRemainingToShip = line.QuantityRemainingToShip,
        QuantityRemainingToInvoice = line.QuantityRemainingToInvoice,
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
        UnitCostSnapshot = line.UnitCostSnapshot,
        WarehouseId = line.WarehouseId,
        Status = line.Status,
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
