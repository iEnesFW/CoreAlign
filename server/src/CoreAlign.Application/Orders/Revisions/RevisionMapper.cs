using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Sales;

namespace CoreAlign.Application.Orders.Revisions;

public static class RevisionMapper
{
    public static OrderRevisionDto ToDto(OrderRevision revision) => new()
    {
        Id = revision.Id,
        OrderId = revision.OrderId,
        RevisionNumber = revision.RevisionNumber,
        RequestedByUserId = revision.RequestedByUserId,
        RequestedByPersona = revision.RequestedByPersona,
        RequestedAtUtc = revision.RequestedAtUtc,
        Status = revision.Status,
        CounterpartyDecisionByUserId = revision.CounterpartyDecisionByUserId,
        DecidedAtUtc = revision.DecidedAtUtc,
        RejectionReason = revision.RejectionReason,
        RequestNotes = revision.RequestNotes,
        ProposedLines = revision.ProposedLines.Select(ToLineDto).ToList(),
    };

    public static OrderRevisionLineDto ToLineDto(RevisionLineSnapshot s) => new()
    {
        ProductId = s.ProductId,
        ProductSku = s.ProductSku,
        ProductName = s.ProductName,
        LineNumber = s.LineNumber,
        Quantity = s.Quantity,
        UnitPrice = s.UnitPrice,
        LineDiscountPercent = s.LineDiscountPercent,
        LineDiscountAmount = s.LineDiscountAmount,
        TaxRatePercent = s.TaxRatePercent,
        IsTaxInclusive = s.IsTaxInclusive,
        WithholdingRatePercent = s.WithholdingRatePercent,
        LineNotes = s.LineNotes,
    };

    public static OrderRevisionTimelineDto ToTimelineDto(Order order) => new()
    {
        OrderId = order.Id,
        CurrentRevisionId = order.CurrentRevisionId,
        AppliedRevisionCount = order.AppliedRevisionCount,
        Revisions = order.Revisions
            .OrderByDescending(r => r.RevisionNumber)
            .Select(ToDto)
            .ToList(),
    };
}
