using CoreAlign.Application.Invoices.Recurring.DTOs;
using CoreAlign.Domain.Entities.Invoices;

namespace CoreAlign.Application.Invoices.Recurring.Handlers;

public static class RecurringInvoiceMapper
{
    public static RecurringInvoiceTemplateDto ToDto(RecurringInvoiceTemplate t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        CustomerId = t.CustomerId,
        Currency = t.Currency,
        Frequency = t.Frequency,
        IntervalCount = t.IntervalCount,
        AnchorDayOfMonth = t.AnchorDayOfMonth,
        AnchorDayOfWeek = t.AnchorDayOfWeek,
        StartDate = t.StartDate,
        EndDate = t.EndDate,
        MaxOccurrences = t.MaxOccurrences,
        NextRunDate = t.NextRunDate,
        LastRunDate = t.LastRunDate,
        OccurrencesGenerated = t.OccurrencesGenerated,
        DueDays = t.DueDays,
        PaymentTermsId = t.PaymentTermsId,
        HeaderDiscountPercent = t.HeaderDiscountPercent,
        HeaderDiscountAmount = t.HeaderDiscountAmount,
        ShippingCost = t.ShippingCost,
        RoundingAdjustment = t.RoundingAdjustment,
        Status = t.Status,
        AutoConfirm = t.AutoConfirm,
        PublicNotes = t.PublicNotes,
        InternalNotes = t.InternalNotes,
        CreatedByUserId = t.CreatedByUserId,
        CreatedAtUtc = t.CreatedAtUtc,
        UpdatedAtUtc = t.UpdatedAtUtc,
        Lines = t.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l => new RecurringInvoiceTemplateLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                ProductId = l.ProductId,
                ProductSku = l.ProductSku,
                ProductName = l.ProductName,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                TaxRatePercent = l.TaxRatePercent,
                TaxRateId = l.TaxRateId,
                LineDiscountPercent = l.LineDiscountPercent,
                LineDiscountAmount = l.LineDiscountAmount,
                WithholdingRatePercent = l.WithholdingRatePercent,
                IsTaxInclusive = l.IsTaxInclusive,
                UomId = l.UomId,
                UomCode = l.UomCode
            })
            .ToList()
    };
}
