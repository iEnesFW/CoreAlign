using CoreAlign.Application.Returns.DTOs;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Returns.Mapping;

public static class ReturnRequestMapper
{
    public static ReturnRequestDto ToDto(
        ReturnRequest entity,
        string? orderNumber = null,
        string? sourceInvoiceNumber = null,
        string? creditNoteNumber = null) =>
        new()
        {
            Id = entity.Id,
            ReturnNumber = entity.ReturnNumber,
            Status = entity.Status,
            Reason = entity.Reason,
            ReasonText = entity.ReasonText,
            OrderId = entity.OrderId,
            OrderNumber = orderNumber ?? entity.Order?.OrderNumber ?? string.Empty,
            CustomerId = entity.CustomerId,
            CustomerName = entity.Customer?.Name ?? entity.CustomerNameSnapshot,
            Currency = entity.Currency,
            SourceInvoiceId = entity.SourceInvoiceId,
            SourceInvoiceNumber = sourceInvoiceNumber,
            CreditNoteId = entity.CreditNoteId,
            CreditNoteNumber = creditNoteNumber,
            RefundPaymentId = entity.RefundPaymentId,
            RequestedAtUtc = entity.RequestedAtUtc,
            RequestedByUserId = entity.RequestedByUserId,
            ApprovedAtUtc = entity.ApprovedAtUtc,
            ApprovedByUserId = entity.ApprovedByUserId,
            RejectedAtUtc = entity.RejectedAtUtc,
            RejectedByUserId = entity.RejectedByUserId,
            RejectionReason = entity.RejectionReason,
            ReceivedAtUtc = entity.ReceivedAtUtc,
            ReceivedByUserId = entity.ReceivedByUserId,
            ReceivedAtWarehouseId = entity.ReceivedAtWarehouseId,
            CreditNoteIssuedAtUtc = entity.CreditNoteIssuedAtUtc,
            RefundedAtUtc = entity.RefundedAtUtc,
            CancelledAtUtc = entity.CancelledAtUtc,
            InternalNotes = entity.InternalNotes,
            CustomerNotes = entity.CustomerNotes,
            LineSubtotal = entity.LineSubtotal,
            TaxTotal = entity.TaxTotal,
            Total = entity.Total,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            Lines = entity.Lines.OrderBy(l => l.LineNumber).Select(ToLineDto).ToList(),
        };

    public static ReturnRequestLineDto ToLineDto(ReturnRequestLine line) => new()
    {
        Id = line.Id,
        LineNumber = line.LineNumber,
        OrderLineId = line.OrderLineId,
        ProductId = line.ProductId,
        ProductSku = line.ProductSku,
        ProductName = line.ProductName,
        UomId = line.UomId,
        UomCode = line.UomCode,
        QuantityReturned = line.QuantityReturned,
        UnitPrice = line.UnitPrice,
        TaxRatePercent = line.TaxRatePercent,
        TaxRateId = line.TaxRateId,
        IsTaxInclusive = line.IsTaxInclusive,
        LineSubtotal = line.LineSubtotal,
        TaxAmount = line.TaxAmount,
        LineTotal = line.LineTotal,
        Restockable = line.Restockable,
        LineNotes = line.LineNotes,
    };

    public static ReturnRequestSummaryDto ToSummary(ReturnRequest entity) => new()
    {
        Id = entity.Id,
        ReturnNumber = entity.ReturnNumber,
        Status = entity.Status,
        Reason = entity.Reason,
        OrderId = entity.OrderId,
        OrderNumber = entity.Order?.OrderNumber ?? string.Empty,
        CustomerId = entity.CustomerId,
        CustomerName = entity.Customer?.Name ?? entity.CustomerNameSnapshot,
        Currency = entity.Currency,
        Total = entity.Total,
        RequestedAtUtc = entity.RequestedAtUtc,
        ReceivedAtUtc = entity.ReceivedAtUtc,
        CreditNoteId = entity.CreditNoteId,
    };

    public static ReturnRequestSummaryDto ToSummary(ReturnRequestSearchRow row) => new()
    {
        Id = row.Id,
        ReturnNumber = row.ReturnNumber,
        Status = row.Status,
        Reason = Enum.TryParse<ReturnReasonCode>(row.Reason, ignoreCase: true, out var parsedReason)
            ? parsedReason
            : ReturnReasonCode.Other,
        OrderId = row.OrderId,
        OrderNumber = row.OrderNumber,
        CustomerId = row.CustomerId,
        CustomerName = row.CustomerName,
        Currency = row.Currency,
        Total = row.LineTotal,
        RequestedAtUtc = row.RequestedAtUtc,
        ReceivedAtUtc = row.ReceivedAtUtc,
        CreditNoteId = row.CreditNoteId,
    };
}
