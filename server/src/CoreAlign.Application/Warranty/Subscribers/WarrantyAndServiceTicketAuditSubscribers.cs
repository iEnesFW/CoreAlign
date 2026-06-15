using System.Text.Json;
using CoreAlign.Application.Common.Audit;
using CoreAlign.Application.Notifications;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Warranty.Subscribers;

public sealed class WarrantyExtendedAuditSubscriber : INotificationHandler<WarrantyExtendedEvent>
{
    public const string OutboxMessageTypeKey = "WarrantyExtended";
    private const string AggregateTypeName = "WarrantyContract";

    private readonly IAuditContext _auditContext;
    private readonly IOutboxRepository _outbox;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<WarrantyExtendedAuditSubscriber> _logger;

    public WarrantyExtendedAuditSubscriber(
        IAuditContext auditContext,
        IOutboxRepository outbox,
        INotificationDispatcher dispatcher,
        ILogger<WarrantyExtendedAuditSubscriber> logger)
    {
        _auditContext = auditContext;
        _outbox = outbox;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task Handle(WarrantyExtendedEvent notification, CancellationToken cancellationToken)
    {
        _auditContext.CaptureCustom(
            notification.WarrantyContractId,
            AggregateTypeName,
            "WarrantyExtended",
            JsonSerializer.Serialize(new
            {
                notification.WarrantyContractId,
                notification.AddedMonths,
                notification.NewEndDate,
                notification.Reason,
                notification.OccurredAtUtc,
            }));

        var payload = JsonSerializer.Serialize(notification);
        await _outbox.AddAsync(new OutboxMessage(OutboxMessageTypeKey, payload), cancellationToken);

        var notificationRequest = new NotificationRequest(
            notification.TenantId,
            UserId: null,
            CustomerId: null,
            CategoryKey: "Warranty",
            TemplateKey: "Warranty.Extended",
            Locale: "tr",
            Payload: new Dictionary<string, object?>
            {
                ["warrantyContractId"] = notification.WarrantyContractId,
                ["addedMonths"] = notification.AddedMonths,
                ["newEndDate"] = notification.NewEndDate.ToString("yyyy-MM-dd"),
                ["reason"] = notification.Reason
            });
        await _dispatcher.DispatchAsync(notificationRequest, cancellationToken);

        _logger.LogInformation(
            "Warranty contract {WarrantyContractId} extended by {AddedMonths} months; new end {NewEndDate:o}.",
            notification.WarrantyContractId, notification.AddedMonths, notification.NewEndDate);
    }
}

public sealed class ServiceTicketAssignedAuditSubscriber : INotificationHandler<ServiceTicketAssignedEvent>
{
    public const string OutboxMessageTypeKey = "ServiceTicketAssigned";
    private const string AggregateTypeName = "ServiceTicket";

    private readonly IAuditContext _auditContext;
    private readonly IOutboxRepository _outbox;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<ServiceTicketAssignedAuditSubscriber> _logger;

    public ServiceTicketAssignedAuditSubscriber(
        IAuditContext auditContext,
        IOutboxRepository outbox,
        INotificationDispatcher dispatcher,
        ILogger<ServiceTicketAssignedAuditSubscriber> logger)
    {
        _auditContext = auditContext;
        _outbox = outbox;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task Handle(ServiceTicketAssignedEvent notification, CancellationToken cancellationToken)
    {
        _auditContext.CaptureCustom(
            notification.ServiceTicketId,
            AggregateTypeName,
            "ServiceTicketAssigned",
            JsonSerializer.Serialize(new
            {
                notification.ServiceTicketId,
                notification.AssignedToUserId,
                notification.OccurredAtUtc,
            }));

        var payload = JsonSerializer.Serialize(notification);
        await _outbox.AddAsync(new OutboxMessage(OutboxMessageTypeKey, payload), cancellationToken);

        var notificationRequest = new NotificationRequest(
            notification.TenantId,
            UserId: notification.AssignedToUserId,
            CustomerId: null,
            CategoryKey: "ServiceTicket",
            TemplateKey: "ServiceTicket.Assigned",
            Locale: "tr",
            Payload: new Dictionary<string, object?>
            {
                ["serviceTicketId"] = notification.ServiceTicketId,
                ["assignedToUserId"] = notification.AssignedToUserId
            });
        await _dispatcher.DispatchAsync(notificationRequest, cancellationToken);

        _logger.LogInformation(
            "Service ticket {ServiceTicketId} assigned to user {AssignedToUserId}.",
            notification.ServiceTicketId, notification.AssignedToUserId);
    }
}
