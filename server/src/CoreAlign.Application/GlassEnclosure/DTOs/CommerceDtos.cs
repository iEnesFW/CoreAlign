using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.DTOs;

public record GenerateShareTokenDto(int? OverrideTtlDays);

public record ShareTokenInfoDto(
    Guid Id,
    string Token,
    string PublicUrl,
    int SceneVersion,
    DateTime ExpiresAtUtc,
    int ViewCount,
    DateTime? LastViewedAtUtc,
    DateTime? AcceptedAtUtc,
    DateTime? RejectedAtUtc,
    string? RejectionReason);

public record ShareViewerProjectDto(
    Guid ProjectId,
    string Code,
    string ProjectName,
    string? CustomerName,
    string Status,
    string Currency,
    decimal GrandTotal,
    int Version,
    string SceneJson,
    DateTime ValidUntilUtc,
    bool AlreadyDecided);

public record ShareViewerActionDto(
    bool Accept,
    string? Reason,
    string? SignatureDataUrl);

public record ShareViewerActionResultDto(
    bool Accepted,
    bool Rejected,
    DateTime DecidedAtUtc);

public record ConvertProjectToOrderResultDto(
    Guid ProjectId,
    Guid OrderId,
    string OrderNumber,
    DateTime LinkedAtUtc);

public record ReleaseToProductionDto(DateTime? RequestedStartDateUtc, Guid? AssignedTeamId);

public record GlassWorkOrderDto(
    Guid Id,
    Guid ProjectId,
    DateTime ScheduledStartDate,
    DateTime ScheduledEndDate,
    Guid? AssignedTeamId,
    Guid? AssignedInstallerUserId,
    decimal WorkloadM2,
    string Status,
    int RecutCount,
    string? DefectNotes,
    string? BomSnapshotJson,
    decimal? BomSnapshotTotal,
    int RevisionCount,
    bool HasOutstandingBlockingRevision,
    WorkOrderRevisionStatus? LatestRevisionStatus,
    int? LatestRevisionNumber,
    decimal? LatestRevisionDeltaPercent);

public record NotificationLogDto(
    Guid Id,
    Guid ProjectId,
    string EventCode,
    string Channel,
    string RecipientKind,
    string RecipientAddress,
    string Status,
    string? ProviderMessageId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? DeliveredAtUtc,
    DateTime? ReadAtUtc,
    string? ErrorMessage,
    int RetryCount);
