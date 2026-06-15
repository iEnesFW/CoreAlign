using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Events;

public record WarrantyActivatedEvent(
    Guid TenantId,
    Guid WarrantyContractId,
    Guid CustomerId,
    Guid OrderId,
    string Number,
    DateTime StartDate,
    DateTime EndDate,
    DateTime OccurredAtUtc) : IDomainEvent;

public record WarrantyExpiredEvent(
    Guid TenantId,
    Guid WarrantyContractId,
    Guid CustomerId,
    string Number,
    DateTime EndDate,
    DateTime OccurredAtUtc) : IDomainEvent;

public record WarrantyCancelledEvent(
    Guid TenantId,
    Guid WarrantyContractId,
    Guid CustomerId,
    string Number,
    string Reason,
    DateTime OccurredAtUtc) : IDomainEvent;

public record WarrantyExtendedEvent(
    Guid TenantId,
    Guid WarrantyContractId,
    int AddedMonths,
    DateTime NewEndDate,
    string? Reason,
    DateTime OccurredAtUtc) : IDomainEvent;

public record WarrantyExpiringSoonEvent(
    Guid TenantId,
    Guid WarrantyContractId,
    Guid CustomerId,
    string Number,
    DateTime EndDate,
    int DaysRemaining,
    DateTime OccurredAtUtc) : IDomainEvent;

public record ServiceTicketOpenedEvent(
    Guid TenantId,
    Guid ServiceTicketId,
    Guid CustomerId,
    Guid? WarrantyContractId,
    ServiceTicketType Type,
    ServiceTicketPriority Priority,
    string Title,
    bool IsUnderWarranty,
    DateTime OccurredAtUtc) : IDomainEvent;

public record ServiceTicketAssignedEvent(
    Guid TenantId,
    Guid ServiceTicketId,
    Guid AssignedToUserId,
    DateTime OccurredAtUtc) : IDomainEvent;

public record ServiceTicketResolvedEvent(
    Guid TenantId,
    Guid ServiceTicketId,
    Guid CustomerId,
    Guid? WorkOrderId,
    decimal? ChargeableAmount,
    DateTime OccurredAtUtc) : IDomainEvent;

public record GlassWorkOrderInstalledEvent(
    Guid TenantId,
    Guid WorkOrderId,
    Guid ProjectId,
    DateTime InstalledAtUtc,
    DateTime OccurredAtUtc) : IDomainEvent;
