using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Events;

public record OrderLineSnapshot(Guid ProductId, decimal Quantity);

public record OrderConfirmedEvent(
    Guid TenantId,
    Guid OrderId,
    string OrderNumber,
    IReadOnlyList<OrderLineSnapshot> Lines,
    DateTime OccurredAtUtc) : IDomainEvent;

public record OrderCancelledFromActiveEvent(
    Guid TenantId,
    Guid OrderId,
    string OrderNumber,
    IReadOnlyList<OrderLineSnapshot> Lines,
    DateTime OccurredAtUtc) : IDomainEvent;

public record OrderSubmittedEvent(
    Guid TenantId,
    Guid OrderId,
    string OrderNumber,
    DateTime OccurredAtUtc) : IDomainEvent;

public record OrderApprovedEvent(
    Guid TenantId,
    Guid OrderId,
    string OrderNumber,
    Guid ApprovedByUserId,
    DateTime OccurredAtUtc) : IDomainEvent;

public record OrderShippedEvent(
    Guid TenantId,
    Guid OrderId,
    Guid ShipmentId,
    string OrderNumber,
    string ShipmentNumber,
    bool IsPartialShipment,
    DateTime OccurredAtUtc) : IDomainEvent;

public record OrderDeliveredEvent(
    Guid TenantId,
    Guid OrderId,
    string OrderNumber,
    DateTime OccurredAtUtc) : IDomainEvent;

public record OrderClosedEvent(
    Guid TenantId,
    Guid OrderId,
    string OrderNumber,
    DateTime OccurredAtUtc) : IDomainEvent;

public record OrderStatusChangedEvent(
    Guid TenantId,
    Guid OrderId,
    string OrderNumber,
    OrderStatus FromStatus,
    OrderStatus ToStatus,
    DateTime OccurredAtUtc) : IDomainEvent;

public record OrderRevisionRequestedEvent(
    Guid TenantId,
    Guid OrderId,
    Guid RevisionId,
    int RevisionNumber,
    string OrderNumber,
    Guid RequestedByUserId,
    string RequestedByPersona,
    DateTime OccurredAtUtc) : IDomainEvent;

public record OrderRevisionApprovedEvent(
    Guid TenantId,
    Guid OrderId,
    Guid RevisionId,
    int RevisionNumber,
    string OrderNumber,
    Guid ApprovedByUserId,
    decimal NewTotal,
    string Currency,
    DateTime OccurredAtUtc) : IDomainEvent;

public record OrderRevisionRejectedEvent(
    Guid TenantId,
    Guid OrderId,
    Guid RevisionId,
    int RevisionNumber,
    string OrderNumber,
    Guid RejectedByUserId,
    string Reason,
    DateTime OccurredAtUtc) : IDomainEvent;
