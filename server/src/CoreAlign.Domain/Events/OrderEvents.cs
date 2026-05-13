using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Events;

public record OrderLineSnapshot(Guid ProductId, decimal Quantity);

public record OrderLineDetailSnapshot(Guid OrderLineId, Guid ProductId, decimal Quantity);

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

public record OrderAllocationRequestedEvent(
    Guid TenantId,
    Guid OrderId,
    string OrderNumber,
    Guid? PreferredWarehouseId,
    IReadOnlyList<OrderLineDetailSnapshot> Lines,
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
