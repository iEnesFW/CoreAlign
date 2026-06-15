using System.Text.Json;
using CoreAlign.Application.Common.Audit;
using CoreAlign.Application.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Installation.Subscribers;

public sealed class InstallationAcceptanceStartedAuditSubscriber
    : INotificationHandler<InstallationAcceptanceStartedEvent>
{
    private const string AggregateTypeName = "InstallationAcceptance";

    private readonly IAuditContext _auditContext;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<InstallationAcceptanceStartedAuditSubscriber> _logger;

    public InstallationAcceptanceStartedAuditSubscriber(
        IAuditContext auditContext,
        INotificationDispatcher dispatcher,
        ILogger<InstallationAcceptanceStartedAuditSubscriber> logger)
    {
        _auditContext = auditContext;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task Handle(InstallationAcceptanceStartedEvent notification, CancellationToken cancellationToken)
    {
        _auditContext.CaptureCustom(
            notification.AcceptanceId,
            AggregateTypeName,
            "InstallationAcceptanceStarted",
            JsonSerializer.Serialize(new
            {
                notification.AcceptanceId,
                notification.WorkOrderId,
                notification.ProjectId,
                notification.InspectorUserId,
                notification.StartedAtUtc,
            }));

        var request = new NotificationRequest(
            notification.TenantId,
            UserId: null,
            CustomerId: null,
            CategoryKey: "Installation",
            TemplateKey: "Installation.AcceptanceStarted",
            Locale: "tr",
            Payload: new Dictionary<string, object?>
            {
                ["acceptanceId"] = notification.AcceptanceId,
                ["workOrderId"] = notification.WorkOrderId,
                ["projectId"] = notification.ProjectId,
                ["startedAt"] = notification.StartedAtUtc.ToString("yyyy-MM-dd HH:mm")
            },
            ChannelsOverride: new[] { NotificationChannel.Email });
        await _dispatcher.DispatchAsync(request, cancellationToken);

        _logger.LogInformation(
            "Installation acceptance {AcceptanceId} started for work order {WorkOrderId}, inspector {InspectorUserId}.",
            notification.AcceptanceId, notification.WorkOrderId, notification.InspectorUserId);
    }
}

public sealed class InstallationAcceptanceSignatureCapturedAuditSubscriber
    : INotificationHandler<InstallationAcceptanceSignatureCapturedEvent>
{
    private const string AggregateTypeName = "InstallationAcceptance";

    private readonly IAuditContext _auditContext;
    private readonly ILogger<InstallationAcceptanceSignatureCapturedAuditSubscriber> _logger;

    public InstallationAcceptanceSignatureCapturedAuditSubscriber(
        IAuditContext auditContext,
        ILogger<InstallationAcceptanceSignatureCapturedAuditSubscriber> logger)
    {
        _auditContext = auditContext;
        _logger = logger;
    }

    public Task Handle(InstallationAcceptanceSignatureCapturedEvent notification, CancellationToken cancellationToken)
    {
        _auditContext.CaptureCustom(
            notification.AcceptanceId,
            AggregateTypeName,
            "SignatureCaptured",
            JsonSerializer.Serialize(new
            {
                notification.AcceptanceId,
                notification.SignatureFileId,
                notification.OccurredAtUtc,
            }));

        _logger.LogInformation(
            "Customer signature captured for acceptance {AcceptanceId}, file {SignatureFileId}.",
            notification.AcceptanceId, notification.SignatureFileId);
        return Task.CompletedTask;
    }
}

public sealed class InstallationRejectedAuditSubscriber
    : INotificationHandler<InstallationRejectedEvent>
{
    private const string AggregateTypeName = "InstallationAcceptance";

    private readonly IAuditContext _auditContext;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<InstallationRejectedAuditSubscriber> _logger;

    public InstallationRejectedAuditSubscriber(
        IAuditContext auditContext,
        INotificationDispatcher dispatcher,
        ILogger<InstallationRejectedAuditSubscriber> logger)
    {
        _auditContext = auditContext;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task Handle(InstallationRejectedEvent notification, CancellationToken cancellationToken)
    {
        _auditContext.CaptureCustom(
            notification.AcceptanceId,
            AggregateTypeName,
            "InstallationRejected",
            JsonSerializer.Serialize(new
            {
                notification.AcceptanceId,
                notification.WorkOrderId,
                notification.ProjectId,
                notification.CustomerId,
                notification.Reason,
                notification.RejectedAtUtc,
            }));

        var request = new NotificationRequest(
            notification.TenantId,
            UserId: null,
            CustomerId: null,
            CategoryKey: "Installation",
            TemplateKey: "Installation.Rejected",
            Locale: "tr",
            Payload: new Dictionary<string, object?>
            {
                ["acceptanceId"] = notification.AcceptanceId,
                ["workOrderId"] = notification.WorkOrderId,
                ["projectId"] = notification.ProjectId,
                ["reason"] = notification.Reason,
                ["rejectedAt"] = notification.RejectedAtUtc.ToString("yyyy-MM-dd HH:mm")
            });
        await _dispatcher.DispatchAsync(request, cancellationToken);

        _logger.LogWarning(
            "Installation rejected for acceptance {AcceptanceId} (work order {WorkOrderId}): {Reason}.",
            notification.AcceptanceId, notification.WorkOrderId, notification.Reason);
    }
}
