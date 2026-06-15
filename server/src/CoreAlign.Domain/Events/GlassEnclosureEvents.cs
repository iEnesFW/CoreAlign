using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Events;

public record GlassStockShortageLineSnapshot(Guid ProfileOrGlassOrHardwareId, GlassBOMLineKind Kind, decimal RequiredQuantity, decimal AvailableQuantity);

public record GlassProjectCreatedEvent(
    Guid TenantId,
    Guid ProjectId,
    Guid CustomerId,
    Guid CreatedByUserId,
    DateTime OccurredAtUtc) : IDomainEvent;

public record GlassProjectStatusChangedEvent(
    Guid TenantId,
    Guid ProjectId,
    GlassProjectStatus FromStatus,
    GlassProjectStatus ToStatus,
    Guid ChangedByUserId,
    DateTime OccurredAtUtc) : IDomainEvent;

public record GlassProjectQuotedEvent(
    Guid TenantId,
    Guid ProjectId,
    Guid QuoteSnapshotId,
    string ShareToken,
    DateTime OccurredAtUtc) : IDomainEvent;

public record GlassProjectQuoteViewedEvent(
    Guid TenantId,
    Guid ProjectId,
    string ShareToken,
    string IpHash,
    DateTime OccurredAtUtc) : IDomainEvent;

public record GlassProjectQuoteAcceptedEvent(
    Guid TenantId,
    Guid ProjectId,
    string ShareToken,
    string? SignatureUrl,
    DateTime OccurredAtUtc) : IDomainEvent;

public record GlassProjectQuoteRejectedEvent(
    Guid TenantId,
    Guid ProjectId,
    string ShareToken,
    string? Reason,
    DateTime OccurredAtUtc) : IDomainEvent;

public record GlassProjectConfirmedEvent(
    Guid TenantId,
    Guid ProjectId,
    Guid OrderId,
    Guid ConfirmedByUserId,
    DateTime OccurredAtUtc) : IDomainEvent;

public record GlassWorkOrderReleasedEvent(
    Guid TenantId,
    Guid WorkOrderId,
    Guid ProjectId,
    DateTime ScheduledStartDate,
    Guid? AssignedTeamId,
    DateTime OccurredAtUtc) : IDomainEvent;

public record GlassWorkOrderStatusChangedEvent(
    Guid TenantId,
    Guid WorkOrderId,
    Guid ProjectId,
    GlassWorkOrderStatus FromStatus,
    GlassWorkOrderStatus ToStatus,
    DateTime OccurredAtUtc) : IDomainEvent;

public record GlassWorkOrderDefectReportedEvent(
    Guid TenantId,
    Guid WorkOrderId,
    Guid ProjectId,
    string DefectNotes,
    DateTime OccurredAtUtc) : IDomainEvent;

public record GlassFieldSurveySubmittedEvent(
    Guid TenantId,
    Guid FieldSurveyId,
    Guid ProjectId,
    Guid SurveyedByUserId,
    DateTime OccurredAtUtc) : IDomainEvent;

public record GlassFieldSurveyAppliedEvent(
    Guid TenantId,
    Guid FieldSurveyId,
    Guid ProjectId,
    DateTime OccurredAtUtc) : IDomainEvent;

public record GlassSceneVersionSavedEvent(
    Guid TenantId,
    Guid ProjectId,
    int Version,
    Guid SavedByUserId,
    DateTime OccurredAtUtc) : IDomainEvent;

public record GlassStockShortageDetectedEvent(
    Guid TenantId,
    Guid ProjectId,
    IReadOnlyList<GlassStockShortageLineSnapshot> Shortages,
    DateTime OccurredAtUtc) : IDomainEvent;

public record GlassInstallationCompletedEvent(
    Guid TenantId,
    Guid ProjectId,
    Guid InstalledByUserId,
    DateTime OccurredAtUtc) : IDomainEvent;

public record GlassWorkOrderRevisionCreatedEvent(
    Guid TenantId,
    Guid RevisionId,
    Guid WorkOrderId,
    int RevisionNumber,
    WorkOrderRevisionStatus Status,
    decimal DeltaPercent,
    DateTime OccurredAtUtc) : IDomainEvent;

public record GlassWorkOrderRevisionApprovedEvent(
    Guid TenantId,
    Guid RevisionId,
    Guid WorkOrderId,
    int RevisionNumber,
    DateTime OccurredAtUtc) : IDomainEvent;

public record GlassWorkOrderRevisionRejectedEvent(
    Guid TenantId,
    Guid RevisionId,
    Guid WorkOrderId,
    int RevisionNumber,
    string Reason,
    DateTime OccurredAtUtc) : IDomainEvent;

public sealed record GlassWorkOrderRevisionBlockOverriddenEvent(
    Guid TenantId,
    Guid RevisionId,
    Guid WorkOrderId,
    Guid OverriddenByUserId,
    string OverrideReason,
    DateTime OccurredAtUtc) : IDomainEvent;
