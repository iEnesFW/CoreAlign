using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Providers.Payment.Events;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Providers.Payment.Outbox;

/// <summary>
/// Drains <c>PaymentRefunded</c> outbox envelopes. F3 notification subsystem
/// emits the customer-facing refund confirmation; finance reconciliation reads
/// the same envelope to post the offsetting journal entry.
/// </summary>
public sealed class PaymentRefundedOutboxHandler : IOutboxMessageHandler
{
    public const string MessageTypeKey = "PaymentRefunded";

    public string MessageType => MessageTypeKey;

    private readonly ILogger<PaymentRefundedOutboxHandler> _logger;

    public PaymentRefundedOutboxHandler(ILogger<PaymentRefundedOutboxHandler> logger)
    {
        _logger = logger;
    }

    public Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return Task.FromResult(OutboxHandlerResult.Failed("Empty payload."));
        }

        PaymentRefundedEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<PaymentRefundedEvent>(
                payloadJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(OutboxHandlerResult.Failed($"Invalid payload JSON: {ex.Message}"));
        }

        if (payload is null)
        {
            return Task.FromResult(OutboxHandlerResult.Failed("Payload deserialized to null."));
        }

        _logger.LogInformation(
            "Payment refunded for tenant {TenantId} via {Provider} (external {ExternalId}, amount {Amount} {Currency}, fullyRefunded={FullyRefunded}, reason={Reason}).",
            payload.TenantId, payload.ProviderName, payload.ExternalTransactionId,
            payload.RefundedAmount, payload.Currency, payload.FullyRefunded, payload.Reason);

        return Task.FromResult(OutboxHandlerResult.Processed($"Refunded:{payload.ExternalTransactionId}"));
    }
}
