using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Events;

public sealed record InstallationAcceptanceStartedEvent(
    Guid TenantId,
    Guid AcceptanceId,
    Guid WorkOrderId,
    Guid ProjectId,
    Guid InspectorUserId,
    DateTime StartedAtUtc,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record InstallationAcceptedEvent(
    Guid TenantId,
    Guid AcceptanceId,
    Guid WorkOrderId,
    Guid ProjectId,
    Guid CustomerId,
    DateTime AcceptedAtUtc,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record InstallationRejectedEvent(
    Guid TenantId,
    Guid AcceptanceId,
    Guid WorkOrderId,
    Guid ProjectId,
    Guid CustomerId,
    string Reason,
    DateTime RejectedAtUtc,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record InstallationAcceptanceSignatureCapturedEvent(
    Guid TenantId,
    Guid AcceptanceId,
    Guid SignatureFileId,
    DateTime OccurredAtUtc) : IDomainEvent;
