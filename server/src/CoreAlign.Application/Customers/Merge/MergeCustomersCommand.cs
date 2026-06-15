using MediatR;

namespace CoreAlign.Application.Customers.Merge;

public sealed record MergeCustomersCommand(
    Guid OperationId,
    Guid SourceCustomerId,
    Guid TargetCustomerId,
    DateTime SourceUpdatedAtUtc,
    DateTime TargetUpdatedAtUtc,
    string? Notes = null) : IRequest<MergeCustomersResult>;

public sealed class MergeCustomersResult
{
    public Guid OperationId { get; init; }
    public Guid SourceCustomerId { get; init; }
    public Guid TargetCustomerId { get; init; }
    public DateTime ExecutedAtUtc { get; init; }
    public int OrdersMoved { get; init; }
    public int InvoicesMoved { get; init; }
    public int PaymentsMoved { get; init; }
    public int AddressesMoved { get; init; }
    public int ContactsMoved { get; init; }
    public int CommentsMoved { get; init; }
    public int LedgerEntriesMoved { get; init; }
    public int TransactionsMoved { get; init; }
    public int TagLinksMoved { get; init; }
    public int DealerLinksMoved { get; init; }
    public int CustomerUsersMoved { get; init; }
    public int OtherRecordsMoved { get; init; }
    public bool ReplayedFromIdempotency { get; init; }
}
