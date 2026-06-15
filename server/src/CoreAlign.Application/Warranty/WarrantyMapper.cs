using CoreAlign.Domain.Entities.Warranty;

namespace CoreAlign.Application.Warranty;

internal static class WarrantyMapper
{
    public static WarrantyContractDto ToDto(WarrantyContract c) => new(
        c.Id,
        c.OrderId,
        c.InvoiceId,
        c.CustomerId,
        c.ProductId,
        c.WorkOrderId,
        c.Number,
        c.CoverageType,
        c.StartDate,
        c.EndDate,
        c.WarrantyMonths,
        c.Status,
        c.TermsJson,
        c.Notes,
        c.CancellationReason,
        c.CreatedAtUtc,
        c.UpdatedAtUtc);

    public static MaintenanceScheduleDto ToDto(MaintenanceSchedule s) => new(
        s.Id,
        s.WarrantyContractId,
        s.Type,
        s.NextDueDate,
        s.LastCompletedAtUtc,
        s.RecurrencePattern,
        s.IsActive,
        s.Notes);

    public static ServiceTicketDto ToDto(ServiceTicket t) => new(
        t.Id,
        t.WarrantyContractId,
        t.CustomerId,
        t.WorkOrderId,
        t.Type,
        t.Status,
        t.Priority,
        t.Title,
        t.DescriptionMd,
        t.ReportedAtUtc,
        t.AssignedToUserId,
        t.ResolvedAtUtc,
        t.ResolutionNotesMd,
        t.IsUnderWarranty,
        t.ChargeableAmount);

    public static WarrantyExpiryAlertDto ToExpiryAlert(WarrantyContract c, DateTime now)
    {
        var daysRemaining = (int)Math.Max(0, Math.Ceiling((c.EndDate - now).TotalDays));
        return new WarrantyExpiryAlertDto(c.Id, c.CustomerId, c.Number, c.EndDate, daysRemaining);
    }
}
