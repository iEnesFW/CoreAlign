using CoreAlign.Application.Billing;
using CoreAlign.Application.Billing.Payments;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.CustomerPortal.Payments;

public class InvoicePaymentSessionWebhookService : IInvoicePaymentSessionWebhookService
{
    private readonly IPaymentSessionRepository _sessions;
    private readonly IInvoiceRepository _invoices;
    private readonly IPaymentRepository _payments;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;
    private readonly IPublisher _publisher;
    private readonly ILogger<InvoicePaymentSessionWebhookService> _logger;

    public InvoicePaymentSessionWebhookService(
        IPaymentSessionRepository sessions,
        IInvoiceRepository invoices,
        IPaymentRepository payments,
        IDocumentSequenceRepository sequences,
        ITenantContext tenant,
        IUnitOfWork uow,
        IPublisher publisher,
        ILogger<InvoicePaymentSessionWebhookService> logger)
    {
        _sessions = sessions;
        _invoices = invoices;
        _payments = payments;
        _sequences = sequences;
        _tenant = tenant;
        _uow = uow;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<PaymentWebhookResult?> TryProcessAsync(string gatewayName, WebhookProcessingResult webhook, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(webhook.IntentId)) return null;

        var session = await _sessions.GetByIntentAsync(gatewayName, webhook.IntentId, cancellationToken);
        if (session is null) return null;

        using var tenantScope = _tenant.PushScope(session.TenantId);

        return webhook.Status switch
        {
            PaymentIntentStatus.Succeeded => await HandleSucceededAsync(session, webhook, cancellationToken),
            PaymentIntentStatus.Cancelled => await HandleCancelledAsync(session, webhook, cancellationToken),
            PaymentIntentStatus.Failed => await HandleFailedAsync(session, webhook, cancellationToken),
            _ => new PaymentWebhookResult(true, session.Id.ToString(), session.Status.ToString(), "Non-terminal status; ack only."),
        };
    }

    private async Task<PaymentWebhookResult> HandleSucceededAsync(Domain.Entities.Payments.PaymentSession session, WebhookProcessingResult webhook, CancellationToken ct)
    {
        if (session.Status == PaymentSessionStatus.Succeeded)
        {
            return new PaymentWebhookResult(true, session.Id.ToString(), session.Status.ToString(), "Already succeeded; idempotent ack.");
        }

        var invoice = await _invoices.GetByIdAsync(session.InvoiceId, ct);
        if (invoice is null)
        {
            _logger.LogWarning("PaymentSession {SessionId} references missing invoice {InvoiceId}.", session.Id, session.InvoiceId);
            return new PaymentWebhookResult(false, session.Id.ToString(), session.Status.ToString(), "Invoice not found.");
        }

        var remaining = Math.Max(0m, Math.Round(invoice.Total - invoice.AmountPaid, 4));
        var amountToApply = Math.Min(session.Amount, remaining);
        if (amountToApply <= 0m)
        {
            _logger.LogWarning(
                "PaymentSession {SessionId} succeeded for {Charged} {Currency} but invoice {InvoiceId} is already fully paid. Manual refund required.",
                session.Id, session.Amount, session.Currency, invoice.Id);
            session.MarkSucceeded(webhook.Reference);
            _sessions.Update(session);
            await _uow.SaveChangesAsync(ct);
            return new PaymentWebhookResult(true, session.Id.ToString(), session.Status.ToString(), "Invoice already paid; session closed.");
        }

        var overpaid = Math.Max(0m, Math.Round(session.Amount - amountToApply, 4));
        if (overpaid > 0m)
        {
            _logger.LogWarning(
                "PaymentSession {SessionId} charged {Charged} {Currency} but invoice {InvoiceId} only required {Applied}. Overpaid {Overpaid} — manual refund required.",
                session.Id, session.Amount, session.Currency, invoice.Id, amountToApply, overpaid);
        }

        var paymentNumber = await _sequences.ConsumeAsync(DocumentSequenceType.PaymentNumber, DateTime.UtcNow, ct);

        var payment = new Payment(
            paymentNumber,
            invoice.CustomerId,
            invoice.CustomerNameSnapshot,
            PaymentDirection.CustomerReceipt,
            DateTime.UtcNow,
            PaymentMethod.CreditCard,
            amountToApply,
            invoice.Currency);

        payment.UpdateDetails(
            paymentDate: DateTime.UtcNow,
            postingDate: DateTime.UtcNow.Date,
            method: PaymentMethod.CreditCard,
            amount: amountToApply,
            exchangeRate: invoice.ExchangeRate,
            bankAccountInfo: null,
            referenceNumber: webhook.Reference,
            checkNumber: null,
            checkDueDate: null,
            notes: $"Customer portal online payment via {webhook.IntentId}");

        payment.Confirm(session.InitiatedByUserId);
        payment.Apply(invoice.Id, amountToApply, remaining);

        await _payments.AddAsync(payment, ct);

        invoice.RecordPayment(amountToApply, DateTime.UtcNow);
        _invoices.Update(invoice);

        session.MarkSucceeded(webhook.Reference);
        _sessions.Update(session);

        await _uow.SaveChangesAsync(ct);

        var message = overpaid > 0m
            ? $"Applied {amountToApply} {invoice.Currency}; overpaid {overpaid} requires manual refund."
            : null;
        return new PaymentWebhookResult(true, session.Id.ToString(), session.Status.ToString(), message);
    }

    private async Task<PaymentWebhookResult> HandleCancelledAsync(Domain.Entities.Payments.PaymentSession session, WebhookProcessingResult webhook, CancellationToken ct)
    {
        if (session.Status is PaymentSessionStatus.Succeeded or PaymentSessionStatus.Cancelled)
        {
            return new PaymentWebhookResult(true, session.Id.ToString(), session.Status.ToString(), "Terminal state; ignored.");
        }
        session.MarkCancelled(webhook.FailureReason ?? "Cancelled by gateway.");
        _sessions.Update(session);
        await _uow.SaveChangesAsync(ct);
        return new PaymentWebhookResult(true, session.Id.ToString(), session.Status.ToString(), null);
    }

    private async Task<PaymentWebhookResult> HandleFailedAsync(Domain.Entities.Payments.PaymentSession session, WebhookProcessingResult webhook, CancellationToken ct)
    {
        if (session.Status is PaymentSessionStatus.Succeeded or PaymentSessionStatus.Failed)
        {
            return new PaymentWebhookResult(true, session.Id.ToString(), session.Status.ToString(), "Terminal state; ignored.");
        }
        session.MarkFailed(webhook.FailureReason);
        _sessions.Update(session);
        await _uow.SaveChangesAsync(ct);
        return new PaymentWebhookResult(true, session.Id.ToString(), session.Status.ToString(), null);
    }
}
