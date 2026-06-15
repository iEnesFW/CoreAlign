using CoreAlign.Application.Common;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Warranty;

public record WarrantyContractDto(
    Guid Id,
    Guid OrderId,
    Guid? InvoiceId,
    Guid CustomerId,
    Guid? ProductId,
    Guid? WorkOrderId,
    string Number,
    WarrantyCoverageType CoverageType,
    DateTime StartDate,
    DateTime EndDate,
    int WarrantyMonths,
    WarrantyContractStatus Status,
    string TermsJson,
    string? Notes,
    string? CancellationReason,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record MaintenanceScheduleDto(
    Guid Id,
    Guid WarrantyContractId,
    MaintenanceScheduleType Type,
    DateTime NextDueDate,
    DateTime? LastCompletedAtUtc,
    string RecurrencePattern,
    bool IsActive,
    string? Notes);

public record ServiceTicketDto(
    Guid Id,
    Guid? WarrantyContractId,
    Guid CustomerId,
    Guid? WorkOrderId,
    ServiceTicketType Type,
    ServiceTicketStatus Status,
    ServiceTicketPriority Priority,
    string Title,
    string DescriptionMd,
    DateTime ReportedAtUtc,
    Guid? AssignedToUserId,
    DateTime? ResolvedAtUtc,
    string? ResolutionNotesMd,
    bool IsUnderWarranty,
    decimal? ChargeableAmount);

public record WarrantyExpiryAlertDto(
    Guid WarrantyContractId,
    Guid CustomerId,
    string Number,
    DateTime EndDate,
    int DaysRemaining);

public record CreateWarrantyContractCommand(
    Guid OrderId,
    Guid CustomerId,
    WarrantyCoverageType CoverageType,
    int WarrantyMonths,
    string TermsJson,
    Guid? ProductId = null,
    Guid? WorkOrderId = null,
    Guid? InvoiceId = null,
    string? Notes = null)
    : IRequest<WarrantyContractDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => Guid.Empty;
    public string AggregateType => nameof(Domain.Entities.Warranty.WarrantyContract);
}

public record ActivateWarrantyContractCommand(Guid Id, DateTime StartDate)
    : IRequest<WarrantyContractDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => Id;
    public string AggregateType => nameof(Domain.Entities.Warranty.WarrantyContract);
}

public record ExtendWarrantyContractCommand(Guid Id, int MonthsAdded, string? Reason)
    : IRequest<WarrantyContractDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => Id;
    public string AggregateType => nameof(Domain.Entities.Warranty.WarrantyContract);
}

public record CancelWarrantyContractCommand(Guid Id, string Reason)
    : IRequest<WarrantyContractDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => Id;
    public string AggregateType => nameof(Domain.Entities.Warranty.WarrantyContract);
}

public record SuspendWarrantyContractCommand(Guid Id, string? Reason)
    : IRequest<WarrantyContractDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => Id;
    public string AggregateType => nameof(Domain.Entities.Warranty.WarrantyContract);
}

public record ResumeWarrantyContractCommand(Guid Id)
    : IRequest<WarrantyContractDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => Id;
    public string AggregateType => nameof(Domain.Entities.Warranty.WarrantyContract);
}

public record CreateServiceTicketCommand(
    Guid CustomerId,
    ServiceTicketType Type,
    ServiceTicketPriority Priority,
    string Title,
    string DescriptionMd,
    Guid? WarrantyContractId = null)
    : IRequest<ServiceTicketDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => Guid.Empty;
    public string AggregateType => nameof(Domain.Entities.Warranty.ServiceTicket);
}

public record AssignServiceTicketCommand(Guid Id, Guid UserId)
    : IRequest<ServiceTicketDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => Id;
    public string AggregateType => nameof(Domain.Entities.Warranty.ServiceTicket);
}

public record ResolveServiceTicketCommand(
    Guid Id,
    string ResolutionNotesMd,
    Guid? WorkOrderId,
    decimal? ChargeableAmount)
    : IRequest<ServiceTicketDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => Id;
    public string AggregateType => nameof(Domain.Entities.Warranty.ServiceTicket);
}

public record CreateMaintenanceScheduleCommand(
    Guid WarrantyContractId,
    MaintenanceScheduleType Type,
    DateTime NextDueDate,
    string? RecurrencePattern,
    string? Notes)
    : IRequest<MaintenanceScheduleDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => WarrantyContractId;
    public string AggregateType => nameof(Domain.Entities.Warranty.MaintenanceSchedule);
}

public record CompleteScheduledMaintenanceCommand(Guid Id, DateTime CompletedAtUtc)
    : IRequest<MaintenanceScheduleDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => Id;
    public string AggregateType => nameof(Domain.Entities.Warranty.MaintenanceSchedule);
}

public record ListWarrantyContractsForCustomerQuery(Guid CustomerId)
    : IRequest<IReadOnlyList<WarrantyContractDto>>;

public record ListWarrantyContractsQuery(WarrantyContractStatus? Status, Guid? CustomerId)
    : IRequest<IReadOnlyList<WarrantyContractDto>>;

public record GetWarrantyContractByIdQuery(Guid Id) : IRequest<WarrantyContractDto?>;

public record GetWarrantyContractByOrderIdQuery(Guid OrderId) : IRequest<WarrantyContractDto?>;

public record ListExpiringWarrantyAlertsQuery(int WithinDays = 30)
    : IRequest<IReadOnlyList<WarrantyExpiryAlertDto>>;

public record ListServiceTicketsQuery(
    ServiceTicketStatus? Status,
    ServiceTicketType? Type,
    ServiceTicketPriority? Priority,
    Guid? CustomerId)
    : IRequest<IReadOnlyList<ServiceTicketDto>>;

public record ListMyServiceTicketsQuery(Guid CustomerId)
    : IRequest<IReadOnlyList<ServiceTicketDto>>;

public record ListMaintenanceSchedulesDueQuery(DateTime AsOfDate)
    : IRequest<IReadOnlyList<MaintenanceScheduleDto>>;
