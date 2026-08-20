using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Events;

public record ReturnRequestLineSnapshot(
    Guid ReturnRequestLineId,
    Guid ProductId,
    decimal QuantityReturned,
    decimal UnitPrice,
    decimal UnitCostSnapshot);

public record ReturnRequestApprovedEvent(
    Guid TenantId,
    Guid ReturnRequestId,
    string ReturnNumber,
    Guid OrderId,
    Guid CustomerId,
    DateTime OccurredAtUtc) : IDomainEvent;

public record ReturnRequestRejectedEvent(
    Guid TenantId,
    Guid ReturnRequestId,
    string ReturnNumber,
    Guid OrderId,
    Guid CustomerId,
    string? Reason,
    DateTime OccurredAtUtc) : IDomainEvent;

public record ReturnRequestReceivedEvent(
    Guid TenantId,
    Guid ReturnRequestId,
    string ReturnNumber,
    Guid OrderId,
    Guid CustomerId,
    Guid WarehouseId,
    IReadOnlyList<ReturnRequestLineSnapshot> Lines,
    decimal ReturnedLineNet,
    DateTime OccurredAtUtc) : IDomainEvent;

public record ReturnRequestCancelledEvent(
    Guid TenantId,
    Guid ReturnRequestId,
    string ReturnNumber,
    Guid OrderId,
    Guid CustomerId,
    DateTime OccurredAtUtc) : IDomainEvent;

public record ReturnRequestCreditNotedEvent(
    Guid TenantId,
    Guid ReturnRequestId,
    string ReturnNumber,
    Guid OrderId,
    Guid CustomerId,
    Guid CreditNoteId,
    DateTime OccurredAtUtc) : IDomainEvent;
