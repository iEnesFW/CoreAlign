using CoreAlign.Application.Billing.Payments;
using CoreAlign.Application.CustomerPortal.Payments;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Billing.Handlers;

/// <summary>
/// Generic webhook entry-point handler. Idempotent by design: re-running with
/// the same payload on an already-Paid order is a no-op apart from recording
/// a fresh <see cref="PaymentAttempt"/> row (provider-side audit trail).
/// </summary>
public class ProcessPaymentWebhookHandler : IRequestHandler<ProcessPaymentWebhookCommand, PaymentWebhookResult>
{
    private readonly IPaymentGatewayRegistry _gateways;
    private readonly ISubscriptionOrderRepository _orders;
    private readonly IPaymentAttemptRepository _attempts;
    private readonly ISubscriptionActivatedOutbox _activatedOutbox;
    private readonly IInvoicePaymentSessionWebhookService _invoiceSessions;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProcessPaymentWebhookHandler> _logger;

    public ProcessPaymentWebhookHandler(
        IPaymentGatewayRegistry gateways,
        ISubscriptionOrderRepository orders,
        IPaymentAttemptRepository attempts,
        ISubscriptionActivatedOutbox activatedOutbox,
        IInvoicePaymentSessionWebhookService invoiceSessions,
        ITenantContext tenant,
        IUnitOfWork uow,
        ILogger<ProcessPaymentWebhookHandler> logger)
    {
        _gateways = gateways;
        _orders = orders;
        _attempts = attempts;
        _activatedOutbox = activatedOutbox;
        _invoiceSessions = invoiceSessions;
        _tenant = tenant;
        _uow = uow;
        _logger = logger;
    }

    public async Task<PaymentWebhookResult> Handle(ProcessPaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        var gateway = _gateways.Find(request.GatewayName);
        if (gateway is null)
        {
            return new PaymentWebhookResult(false, null, null, $"Unknown gateway '{request.GatewayName}'.");
        }

        WebhookProcessingResult webhook;
        try
        {
            webhook = await gateway.HandleWebhookAsync(request.Payload, request.Headers, cancellationToken);
        }
        catch (PaymentWebhookSignatureException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Webhook from {Gateway} could not be parsed/verified.", gateway.Name);
            return new PaymentWebhookResult(false, null, null, "Invalid webhook.");
        }

        if (string.IsNullOrWhiteSpace(webhook.IntentId))
        {
            return new PaymentWebhookResult(true, null, null, "Ack (no actionable intent).");
        }

        var order = await _orders.GetByGatewayIntentAsync(gateway.Name, webhook.IntentId, cancellationToken);
        if (order is null)
        {
            var invoiceOutcome = await _invoiceSessions.TryProcessAsync(gateway.Name, webhook, cancellationToken);
            if (invoiceOutcome is not null) return invoiceOutcome;
            return new PaymentWebhookResult(false, null, null, "No order for intent.");
        }

        using var tenantScope = _tenant.PushScope(order.TenantId);

        return webhook.Status switch
        {
            PaymentIntentStatus.Succeeded => await HandleSucceededAsync(order, gateway, webhook, cancellationToken),
            PaymentIntentStatus.Cancelled => await HandleCancelledAsync(order, gateway, webhook, cancellationToken),
            PaymentIntentStatus.Failed => await HandleFailedAsync(order, gateway, webhook, cancellationToken),
            _ => await HandlePendingOrUnknownAsync(order, gateway, webhook, cancellationToken),
        };
    }

    private async Task<PaymentWebhookResult> HandleSucceededAsync(SubscriptionOrder order, IPaymentGateway gateway, WebhookProcessingResult webhook, CancellationToken ct)
    {
        await _attempts.AddAsync(new PaymentAttempt(
            order.Id, gateway.Name, webhook.IntentId,
            PaymentAttemptStatus.Succeeded, order.TotalAmount, order.Currency, webhook.RawJson), ct);

        if (order.Status == SubscriptionOrderStatus.Paid)
        {
            await _uow.SaveChangesAsync(ct);
            return new PaymentWebhookResult(true, order.Id.ToString(), order.Status.ToString(), "Already paid; recorded duplicate attempt.");
        }

        order.MarkPaid(webhook.Reference, webhook.Reference);
        _orders.Update(order);
        await _activatedOutbox.EnqueueAsync(new SubscriptionActivatedPayload(order.Id, order.TenantId), ct);
        await _uow.SaveChangesAsync(ct);
        return new PaymentWebhookResult(true, order.Id.ToString(), order.Status.ToString(), null);
    }

    private async Task<PaymentWebhookResult> HandleCancelledAsync(SubscriptionOrder order, IPaymentGateway gateway, WebhookProcessingResult webhook, CancellationToken ct)
    {
        await _attempts.AddAsync(new PaymentAttempt(
            order.Id, gateway.Name, webhook.IntentId,
            PaymentAttemptStatus.Cancelled, order.TotalAmount, order.Currency, webhook.RawJson), ct);

        if (order.Status is SubscriptionOrderStatus.Cancelled or SubscriptionOrderStatus.Paid)
        {
            await _uow.SaveChangesAsync(ct);
            return new PaymentWebhookResult(true, order.Id.ToString(), order.Status.ToString(), "Terminal state; ignored.");
        }

        order.MarkCancelled(webhook.FailureReason ?? "Cancelled by gateway.");
        _orders.Update(order);
        await _uow.SaveChangesAsync(ct);
        return new PaymentWebhookResult(true, order.Id.ToString(), order.Status.ToString(), null);
    }

    private async Task<PaymentWebhookResult> HandleFailedAsync(SubscriptionOrder order, IPaymentGateway gateway, WebhookProcessingResult webhook, CancellationToken ct)
    {
        await _attempts.AddAsync(new PaymentAttempt(
            order.Id, gateway.Name, webhook.IntentId,
            PaymentAttemptStatus.Failed, order.TotalAmount, order.Currency, webhook.RawJson, webhook.FailureReason), ct);

        if (order.Status is SubscriptionOrderStatus.Failed or SubscriptionOrderStatus.Paid)
        {
            await _uow.SaveChangesAsync(ct);
            return new PaymentWebhookResult(true, order.Id.ToString(), order.Status.ToString(), "Terminal state; ignored.");
        }

        order.MarkFailed(webhook.FailureReason);
        _orders.Update(order);
        await _uow.SaveChangesAsync(ct);
        return new PaymentWebhookResult(true, order.Id.ToString(), order.Status.ToString(), null);
    }

    private async Task<PaymentWebhookResult> HandlePendingOrUnknownAsync(SubscriptionOrder order, IPaymentGateway gateway, WebhookProcessingResult webhook, CancellationToken ct)
    {
        await _attempts.AddAsync(new PaymentAttempt(
            order.Id, gateway.Name, webhook.IntentId,
            PaymentAttemptStatus.Initiated, order.TotalAmount, order.Currency, webhook.RawJson), ct);
        await _uow.SaveChangesAsync(ct);
        return new PaymentWebhookResult(true, order.Id.ToString(), order.Status.ToString(), "Non-terminal status; recorded attempt only.");
    }
}
