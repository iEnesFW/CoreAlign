using CoreAlign.Application.Common;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Installation;

public record InstallationAcceptanceDto(
    Guid Id,
    Guid WorkOrderId,
    Guid ProjectId,
    Guid CustomerId,
    InstallationAcceptanceStatus Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    Guid InspectorUserId,
    Guid? CustomerSignatureFileId,
    DateTime? CustomerSignatureCapturedAtUtc,
    string? CustomerName,
    string ChecklistJson,
    string PhotoFileIds,
    string? NotesMd,
    string? RejectionReason);

public record PunchListItemDto(
    Guid Id,
    Guid AcceptanceId,
    string Description,
    PunchListSeverity Severity,
    PunchListItemStatus Status,
    Guid? AssignedToUserId,
    DateTime? ResolvedAtUtc,
    string? ResolutionNotes);

public record StartInstallationAcceptanceCommand(
    Guid WorkOrderId,
    Guid InspectorUserId)
    : IRequest<InstallationAcceptanceDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => WorkOrderId;
    public string AggregateType => nameof(Domain.Entities.Installation.InstallationAcceptance);
}

public record UpdateChecklistItemCommand(
    Guid AcceptanceId,
    string Category,
    string ItemKey,
    InstallationChecklistResult Result,
    string? Notes)
    : IRequest<InstallationAcceptanceDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => AcceptanceId;
    public string AggregateType => nameof(Domain.Entities.Installation.InstallationAcceptance);
}

public record UploadAcceptancePhotoCommand(
    Guid AcceptanceId,
    Guid FileId)
    : IRequest<InstallationAcceptanceDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => AcceptanceId;
    public string AggregateType => nameof(Domain.Entities.Installation.InstallationAcceptance);
}

public record CaptureCustomerSignatureCommand(
    Guid AcceptanceId,
    Guid FileId,
    string CustomerName)
    : IRequest<InstallationAcceptanceDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => AcceptanceId;
    public string AggregateType => nameof(Domain.Entities.Installation.InstallationAcceptance);
}

public record AcceptInstallationCommand(Guid AcceptanceId, string IdempotencyKey)
    : IRequest<InstallationAcceptanceDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => AcceptanceId;
    public string AggregateType => nameof(Domain.Entities.Installation.InstallationAcceptance);
}

public record RejectInstallationCommand(Guid AcceptanceId, string Reason)
    : IRequest<InstallationAcceptanceDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => AcceptanceId;
    public string AggregateType => nameof(Domain.Entities.Installation.InstallationAcceptance);
}

public record AddPunchListItemCommand(
    Guid AcceptanceId,
    string Description,
    PunchListSeverity Severity)
    : IRequest<PunchListItemDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => AcceptanceId;
    public string AggregateType => nameof(Domain.Entities.Installation.PunchListItem);
}

public record ResolvePunchListItemCommand(
    Guid PunchItemId,
    string? ResolutionNotes)
    : IRequest<PunchListItemDto>, ITransactionalRequest, IAuditableMutation
{
    public Guid AggregateId => PunchItemId;
    public string AggregateType => nameof(Domain.Entities.Installation.PunchListItem);
}

public record GetInstallationAcceptanceByWorkOrderIdQuery(Guid WorkOrderId) : IRequest<InstallationAcceptanceDto?>;

public record GetInstallationAcceptanceByIdQuery(Guid Id) : IRequest<InstallationAcceptanceDto?>;

public record ListPendingAcceptancesForInspectorQuery(Guid InspectorUserId, InstallationAcceptanceStatus? Status)
    : IRequest<IReadOnlyList<InstallationAcceptanceDto>>;

public record ListPunchListItemsQuery(PunchListItemStatus Status) : IRequest<IReadOnlyList<PunchListItemDto>>;

public record GetAcceptanceWithFullDetailsQuery(Guid Id)
    : IRequest<AcceptanceFullDetailsDto?>;

public record AcceptanceFullDetailsDto(
    InstallationAcceptanceDto Acceptance,
    IReadOnlyList<PunchListItemDto> PunchList);
