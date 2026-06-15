using CoreAlign.Application.Billing;
using CoreAlign.Application.Billing.Payments;

namespace CoreAlign.Application.CustomerPortal.Payments;

public interface IInvoicePaymentSessionWebhookService
{
    Task<PaymentWebhookResult?> TryProcessAsync(string gatewayName, WebhookProcessingResult webhook, CancellationToken cancellationToken);
}
