using CoreAlign.Application.Common;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Orders.Revisions;

public record RevisionLineInput(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    int LineNumber = 0,
    decimal LineDiscountPercent = 0m,
    decimal LineDiscountAmount = 0m,
    decimal TaxRatePercent = 0m,
    bool IsTaxInclusive = false,
    decimal WithholdingRatePercent = 0m,
    string? LineNotes = null);

public record RequestOrderRevisionCommand(
    Guid OrderId,
    IReadOnlyList<RevisionLineInput> ProposedLines,
    string? RequestNotes = null) : IRequest<OrderRevisionDto>, ITransactionalRequest;

public record ApproveOrderRevisionCommand(Guid OrderId, Guid RevisionId)
    : IRequest<OrderRevisionDto>, ITransactionalRequest;

public record RejectOrderRevisionCommand(Guid OrderId, Guid RevisionId, string Reason)
    : IRequest<OrderRevisionDto>, ITransactionalRequest;

public record CancelOrderRevisionCommand(Guid OrderId, Guid RevisionId)
    : IRequest<OrderRevisionDto>, ITransactionalRequest;

public record GetOrderRevisionsQuery(Guid OrderId) : IRequest<OrderRevisionTimelineDto>;

public class OrderRevisionLineDto
{
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineDiscountPercent { get; set; }
    public decimal LineDiscountAmount { get; set; }
    public decimal TaxRatePercent { get; set; }
    public bool IsTaxInclusive { get; set; }
    public decimal WithholdingRatePercent { get; set; }
    public string? LineNotes { get; set; }
}

public class OrderRevisionDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public int RevisionNumber { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string RequestedByPersona { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
    public RevisionStatus Status { get; set; }
    public Guid? CounterpartyDecisionByUserId { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public string? RequestNotes { get; set; }
    public List<OrderRevisionLineDto> ProposedLines { get; set; } = new();
}

public class OrderRevisionTimelineDto
{
    public Guid OrderId { get; set; }
    public Guid? CurrentRevisionId { get; set; }
    public int AppliedRevisionCount { get; set; }
    public List<OrderRevisionDto> Revisions { get; set; } = new();
}
