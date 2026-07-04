using System.Text.Json;
using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Invoices.Handlers;

public static class InvoiceMapper
{
    public static InvoiceDto ToDto(Invoice invoice)
    {
        var nowUtc = DateTime.UtcNow;
        var isOverdue = invoice.Status != InvoiceStatus.Paid
                        && invoice.Status != InvoiceStatus.Cancelled
                        && invoice.Status != InvoiceStatus.Void
                        && invoice.DueDate < nowUtc
                        && invoice.AmountPaid < invoice.Total;

        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Type = invoice.Type,
            Status = invoice.Status,
            OrderId = invoice.OrderId,
            OriginInvoiceId = invoice.OriginInvoiceId,
            CreditNoteId = invoice.CreditNoteId,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.Customer?.Name ?? invoice.CustomerNameSnapshot,
            CustomerSnapshot = invoice.CustomerSnapshot != null ? ToDto(invoice.CustomerSnapshot) : null,
            BillingAddressSnapshot = invoice.BillingAddressSnapshot != null ? ToDto(invoice.BillingAddressSnapshot) : null,
            ShippingAddressSnapshot = invoice.ShippingAddressSnapshot != null ? ToDto(invoice.ShippingAddressSnapshot) : null,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            PostingDate = invoice.PostingDate,
            IssuedAtUtc = invoice.IssuedAtUtc,
            SentAtUtc = invoice.SentAtUtc,
            PaidAtUtc = invoice.PaidAtUtc,
            CancelledAtUtc = invoice.CancelledAtUtc,
            VoidedAtUtc = invoice.VoidedAtUtc,
            Currency = invoice.Currency,
            ExchangeRate = invoice.ExchangeRate,
            PaymentTermsId = invoice.PaymentTermsId,
            PaymentTermsNetDaysSnapshot = invoice.PaymentTermsNetDaysSnapshot,
            Subtotal = invoice.Subtotal,
            LineDiscountTotal = invoice.LineDiscountTotal,
            HeaderDiscountAmount = invoice.HeaderDiscountAmount,
            HeaderDiscountPercent = invoice.HeaderDiscountPercent,
            TaxableTotal = invoice.TaxableTotal,
            TaxTotal = invoice.TaxTotal,
            WithholdingTotal = invoice.WithholdingTotal,
            ShippingCost = invoice.ShippingCost,
            RoundingAdjustment = invoice.RoundingAdjustment,
            Total = invoice.Total,
            AmountPaid = invoice.AmountPaid,
            AmountDue = invoice.AmountDue,
            TaxBreakdown = ParseTaxBreakdown(invoice.TaxBreakdownJson),
            CancelReason = invoice.CancelReason,
            VoidReason = invoice.VoidReason,
            InternalNotes = invoice.InternalNotes,
            PublicNotes = invoice.PublicNotes,
            TermsAndConditions = invoice.TermsAndConditions,
            Notes = invoice.Notes,
            EInvoiceUuid = invoice.EInvoiceUuid,
            EInvoiceStatus = invoice.EInvoiceStatus,
            IsPostedToLedger = invoice.IsPostedToLedger,
            IsOverdue = isOverdue,
            Lines = invoice.Lines.OrderBy(l => l.LineNumber).Select(ToLineDto).ToList(),
            CreatedAtUtc = invoice.CreatedAtUtc,
            UpdatedAtUtc = invoice.UpdatedAtUtc,
        };
    }

    public static InvoiceSummaryDto ToSummaryDto(Invoice invoice)
    {
        var nowUtc = DateTime.UtcNow;
        var isOverdue = invoice.Status != InvoiceStatus.Paid
                        && invoice.Status != InvoiceStatus.Cancelled
                        && invoice.Status != InvoiceStatus.Void
                        && invoice.DueDate < nowUtc
                        && invoice.AmountPaid < invoice.Total;

        return new InvoiceSummaryDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Type = invoice.Type,
            OrderId = invoice.OrderId,
            CustomerName = invoice.Customer?.Name ?? invoice.CustomerNameSnapshot,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            Status = invoice.Status,
            Currency = invoice.Currency,
            Total = invoice.Total,
            AmountPaid = invoice.AmountPaid,
            AmountDue = invoice.AmountDue,
            IsOverdue = isOverdue,
        };
    }

    public static InvoiceSummaryDto ToSummaryDto(InvoiceSearchRow row)
    {
        var nowUtc = DateTime.UtcNow;
        var isOverdue = row.Status != InvoiceStatus.Paid
                        && row.Status != InvoiceStatus.Cancelled
                        && row.Status != InvoiceStatus.Void
                        && row.DueDate < nowUtc
                        && row.AmountPaid < row.Total;

        return new InvoiceSummaryDto
        {
            Id = row.Id,
            InvoiceNumber = row.InvoiceNumber,
            Type = row.Type,
            OrderId = row.OrderId,
            CustomerName = row.CustomerName,
            IssueDate = row.IssueDate,
            DueDate = row.DueDate,
            Status = row.Status,
            Currency = row.Currency,
            Total = row.Total,
            AmountPaid = row.AmountPaid,
            AmountDue = row.Total - row.AmountPaid,
            IsOverdue = isOverdue,
            OrderNumber = row.OrderNumber,
        };
    }

    public static InvoiceLineDto ToLineDto(InvoiceLine line) => new()
    {
        Id = line.Id,
        LineNumber = line.LineNumber,
        ProductId = line.ProductId,
        ProductSku = line.ProductSku,
        ProductName = line.ProductName,
        Description = line.Description,
        UomId = line.UomId,
        UomCode = line.UomCode,
        Quantity = line.Quantity,
        UnitPrice = line.UnitPrice,
        LineDiscountPercent = line.LineDiscountPercent,
        LineDiscountAmount = line.LineDiscountAmount,
        TaxRateId = line.TaxRateId,
        TaxRatePercent = line.TaxRatePercent,
        TaxAmount = line.TaxAmount,
        IsTaxInclusive = line.IsTaxInclusive,
        WithholdingRatePercent = line.WithholdingRatePercent,
        WithholdingAmount = line.WithholdingAmount,
        LineSubtotal = line.LineSubtotal,
        LineNetAmount = line.LineNetAmount,
        LineTotal = line.LineTotal,
        RevenueAccountCode = line.RevenueAccountCode,
        OriginOrderLineId = line.OriginOrderLineId,
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

    private static List<TaxBreakdownItem> ParseTaxBreakdown(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<TaxBreakdownItem>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var list = new List<TaxBreakdownItem>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                list.Add(new TaxBreakdownItem
                {
                    Rate = element.TryGetProperty("rate", out var rate) && rate.TryGetDecimal(out var r) ? r : 0m,
                    Base = element.TryGetProperty("base", out var b) && b.TryGetDecimal(out var bv) ? bv : 0m,
                    Amount = element.TryGetProperty("amount", out var a) && a.TryGetDecimal(out var av) ? av : 0m,
                });
            }
            return list;
        }
        catch
        {
            return new List<TaxBreakdownItem>();
        }
    }

    public static string GenerateInvoiceNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return $"INV-{timestamp}-{random}";
    }
}
