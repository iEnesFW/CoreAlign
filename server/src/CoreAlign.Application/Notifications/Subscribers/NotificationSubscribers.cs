using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Notifications.Subscribers;

public sealed class WarrantyActivatedNotificationSubscriber : INotificationHandler<WarrantyActivatedEvent>
{
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<WarrantyActivatedNotificationSubscriber> _logger;

    public WarrantyActivatedNotificationSubscriber(INotificationDispatcher dispatcher, ILogger<WarrantyActivatedNotificationSubscriber> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public Task Handle(WarrantyActivatedEvent notification, CancellationToken cancellationToken)
    {
        var request = new NotificationRequest(
            notification.TenantId,
            null,
            notification.CustomerId,
            CategoryKey: "Warranty",
            TemplateKey: "Warranty.Activated",
            Locale: "tr",
            Payload: new Dictionary<string, object?>
            {
                ["warrantyNumber"] = notification.Number,
                ["startDate"] = notification.StartDate.ToString("yyyy-MM-dd"),
                ["endDate"] = notification.EndDate.ToString("yyyy-MM-dd"),
            });
        return _dispatcher.DispatchAsync(request, cancellationToken);
    }
}

public sealed class WarrantyExpiringNotificationSubscriber : INotificationHandler<WarrantyExpiringSoonEvent>
{
    private readonly INotificationDispatcher _dispatcher;

    public WarrantyExpiringNotificationSubscriber(INotificationDispatcher dispatcher) => _dispatcher = dispatcher;

    public Task Handle(WarrantyExpiringSoonEvent notification, CancellationToken cancellationToken)
    {
        var request = new NotificationRequest(
            notification.TenantId,
            null,
            notification.CustomerId,
            CategoryKey: "Warranty",
            TemplateKey: "Warranty.Expiring",
            Locale: "tr",
            Payload: new Dictionary<string, object?>
            {
                ["warrantyNumber"] = notification.Number,
                ["endDate"] = notification.EndDate.ToString("yyyy-MM-dd"),
                ["daysRemaining"] = notification.DaysRemaining
            });
        return _dispatcher.DispatchAsync(request, cancellationToken);
    }
}

public sealed class PaymentSucceededNotificationSubscriber : INotificationHandler<PaymentConfirmedEvent>
{
    private readonly INotificationDispatcher _dispatcher;

    public PaymentSucceededNotificationSubscriber(INotificationDispatcher dispatcher) => _dispatcher = dispatcher;

    public Task Handle(PaymentConfirmedEvent notification, CancellationToken cancellationToken)
    {
        var request = new NotificationRequest(
            notification.TenantId,
            null,
            notification.CustomerId,
            CategoryKey: "Payment",
            TemplateKey: "Payment.Succeeded",
            Locale: "tr",
            Payload: new Dictionary<string, object?>
            {
                ["paymentNumber"] = notification.PaymentNumber,
                ["amount"] = notification.Amount.ToString("0.00"),
                ["currency"] = notification.Currency
            });
        return _dispatcher.DispatchAsync(request, cancellationToken);
    }
}

public sealed class PaymentFailedNotificationSubscriber : INotificationHandler<PaymentVoidedEvent>
{
    private readonly INotificationDispatcher _dispatcher;

    public PaymentFailedNotificationSubscriber(INotificationDispatcher dispatcher) => _dispatcher = dispatcher;

    public Task Handle(PaymentVoidedEvent notification, CancellationToken cancellationToken)
    {
        var request = new NotificationRequest(
            notification.TenantId,
            null,
            notification.CustomerId,
            CategoryKey: "Payment",
            TemplateKey: "Payment.Failed",
            Locale: "tr",
            Payload: new Dictionary<string, object?>
            {
                ["paymentNumber"] = notification.PaymentNumber,
                ["amount"] = notification.Amount.ToString("0.00")
            });
        return _dispatcher.DispatchAsync(request, cancellationToken);
    }
}

public sealed class InstallationAcceptedNotificationSubscriber : INotificationHandler<InstallationAcceptedEvent>
{
    private readonly INotificationDispatcher _dispatcher;

    public InstallationAcceptedNotificationSubscriber(INotificationDispatcher dispatcher) => _dispatcher = dispatcher;

    public Task Handle(InstallationAcceptedEvent notification, CancellationToken cancellationToken)
    {
        var request = new NotificationRequest(
            notification.TenantId,
            null,
            notification.CustomerId,
            CategoryKey: "Installation",
            TemplateKey: "Installation.Accepted",
            Locale: "tr",
            Payload: new Dictionary<string, object?>
            {
                ["acceptanceId"] = notification.AcceptanceId,
                ["workOrderId"] = notification.WorkOrderId,
                ["acceptedAt"] = notification.AcceptedAtUtc.ToString("yyyy-MM-dd HH:mm")
            });
        return _dispatcher.DispatchAsync(request, cancellationToken);
    }
}

public sealed class MrpSuggestionsCreatedNotificationSubscriber : INotificationHandler<MrpSuggestionsCreatedEvent>
{
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<MrpSuggestionsCreatedNotificationSubscriber> _logger;

    public MrpSuggestionsCreatedNotificationSubscriber(INotificationDispatcher dispatcher, ILogger<MrpSuggestionsCreatedNotificationSubscriber> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public Task Handle(MrpSuggestionsCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("MRP suggestions created for tenant {TenantId}: {LineCount} lines", notification.TenantId, notification.LineCount);
        var request = new NotificationRequest(
            notification.TenantId,
            null,
            null,
            CategoryKey: "Mrp",
            TemplateKey: "Mrp.SuggestionsCreated",
            Locale: "tr",
            Payload: new Dictionary<string, object?>
            {
                ["lineCount"] = notification.LineCount,
                ["requisitionCount"] = notification.RequisitionCount,
                ["asOfDate"] = notification.AsOfDate.ToString("yyyy-MM-dd")
            },
            ChannelsOverride: new[] { NotificationChannel.InApp });
        return _dispatcher.DispatchAsync(request, cancellationToken);
    }
}

public sealed class ServiceTicketResolvedNotificationSubscriber : INotificationHandler<ServiceTicketResolvedEvent>
{
    private readonly INotificationDispatcher _dispatcher;

    public ServiceTicketResolvedNotificationSubscriber(INotificationDispatcher dispatcher) => _dispatcher = dispatcher;

    public Task Handle(ServiceTicketResolvedEvent notification, CancellationToken cancellationToken)
    {
        var request = new NotificationRequest(
            notification.TenantId,
            null,
            notification.CustomerId,
            CategoryKey: "ServiceTicket",
            TemplateKey: "ServiceTicket.Resolved",
            Locale: "tr",
            Payload: new Dictionary<string, object?>
            {
                ["serviceTicketId"] = notification.ServiceTicketId,
                ["chargeableAmount"] = notification.ChargeableAmount?.ToString("0.00") ?? "0.00"
            });
        return _dispatcher.DispatchAsync(request, cancellationToken);
    }
}

public sealed class ServiceTicketAssignedNotificationSubscriber : INotificationHandler<ServiceTicketAssignedEvent>
{
    private readonly INotificationDispatcher _dispatcher;

    public ServiceTicketAssignedNotificationSubscriber(INotificationDispatcher dispatcher) => _dispatcher = dispatcher;

    public Task Handle(ServiceTicketAssignedEvent notification, CancellationToken cancellationToken)
    {
        var request = new NotificationRequest(
            notification.TenantId,
            notification.AssignedToUserId,
            null,
            CategoryKey: "ServiceTicket",
            TemplateKey: "ServiceTicket.Assigned",
            Locale: "tr",
            Payload: new Dictionary<string, object?>
            {
                ["serviceTicketId"] = notification.ServiceTicketId
            },
            ChannelsOverride: new[] { NotificationChannel.InApp, NotificationChannel.Email });
        return _dispatcher.DispatchAsync(request, cancellationToken);
    }
}
