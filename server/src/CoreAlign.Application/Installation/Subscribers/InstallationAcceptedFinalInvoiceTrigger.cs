using System.Text.Json;
using CoreAlign.Application.Common.Audit;
using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Installation.Subscribers;

public sealed class InstallationAcceptedFinalInvoiceTrigger : INotificationHandler<InstallationAcceptedEvent>
{
    public const string MessageTypeKey = "InstallationAcceptedFinalInvoice";
    private const string AggregateTypeName = "InstallationAcceptance";

    private readonly IOutboxRepository _outbox;
    private readonly IOrderRepository _orders;
    private readonly IMediator _mediator;
    private readonly IAuditContext _auditContext;
    private readonly ILogger<InstallationAcceptedFinalInvoiceTrigger> _logger;

    public InstallationAcceptedFinalInvoiceTrigger(
        IOutboxRepository outbox,
        IOrderRepository orders,
        IMediator mediator,
        IAuditContext auditContext,
        ILogger<InstallationAcceptedFinalInvoiceTrigger> logger)
    {
        _outbox = outbox;
        _orders = orders;
        _mediator = mediator;
        _auditContext = auditContext;
        _logger = logger;
    }

    public async Task Handle(InstallationAcceptedEvent notification, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            notification.TenantId,
            notification.AcceptanceId,
            notification.WorkOrderId,
            notification.ProjectId,
            notification.CustomerId,
            notification.AcceptedAtUtc,
        });

        var message = new OutboxMessage(MessageTypeKey, payload);
        await _outbox.AddAsync(message, cancellationToken);

        var order = await _orders.GetByGlassProjectIdAsync(notification.ProjectId, cancellationToken);
        if (order is null)
        {
            _logger.LogWarning(
                "InstallationAccepted for acceptance {AcceptanceId}: no order found for project {ProjectId}; final invoice skipped.",
                notification.AcceptanceId, notification.ProjectId);
            return;
        }

        try
        {
            var invoice = await _mediator.Send(
                new GenerateInvoiceFromOrderCommand(order.Id),
                cancellationToken);

            _auditContext.CaptureCustom(
                notification.AcceptanceId,
                AggregateTypeName,
                "FinalInvoiceGenerated",
                JsonSerializer.Serialize(new
                {
                    notification.AcceptanceId,
                    OrderId = order.Id,
                    InvoiceId = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber,
                }));

            _logger.LogInformation(
                "Final invoice {InvoiceId} ({InvoiceNumber}) generated for order {OrderId} after acceptance {AcceptanceId}.",
                invoice.Id, invoice.InvoiceNumber, order.Id, notification.AcceptanceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Final invoice generation failed for acceptance {AcceptanceId}, order {OrderId}; outbox row {OutboxId} will retry.",
                notification.AcceptanceId, order.Id, message.Id);
        }
    }
}
