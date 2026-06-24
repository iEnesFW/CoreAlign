using CoreAlign.Application.Common;
using CoreAlign.Domain.Common;
using MediatR;

namespace CoreAlign.Application.Documents.Forwarding;

public enum ForwardableDocumentType
{
    Invoice = 0,
    Order = 1,
}

public sealed record ForwardDocumentResult(bool Queued, string Status);

public sealed record ForwardCustomerDocumentCommand(
    ForwardableDocumentType DocumentType,
    Guid DocumentId,
    string RecipientEmail,
    Guid IdempotencyKey) : IRequest<ForwardDocumentResult>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => DocumentId;
    public string AggregateType => DocumentType.ToString();
}

public sealed record ForwardDealerDocumentCommand(
    ForwardableDocumentType DocumentType,
    Guid DocumentId,
    string RecipientEmail,
    Guid IdempotencyKey) : IRequest<ForwardDocumentResult>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => DocumentId;
    public string AggregateType => DocumentType.ToString();
}
