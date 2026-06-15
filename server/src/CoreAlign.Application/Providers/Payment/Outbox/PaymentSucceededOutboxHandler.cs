using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Providers.Payment.Events;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Providers.Payment.Outbox;

/// <summary>
/// Drains <c>PaymentSucceeded</c> outbox envelopes. F3 notification subsystem
/// hooks the same envelope to send receipts and trigger order fulfilment hand-off.
/// </summary>
public sealed class PaymentSucceededOutboxHandler : IOutboxMessageHandler
{
    public const string MessageTypeKey = "PaymentSucceeded";

    public string MessageType => MessageTypeKey;

    private readonly ILogger<PaymentSucceededOutboxHandler> _logger;

    public PaymentSucceededOutboxHandler(ILogger<PaymentSucceededOutboxHandler> logger)
    {
        _logger = logger;
    }

    public Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return Task.FromResult(OutboxHandlerResult.Failed("Empty payload."));
        }

        PaymentSucceededEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<PaymentSucceededEvent>(
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
            "Payment succeeded for tenant {TenantId} via {Provider} (order {OrderReference}, external {ExternalId}, amount {Amount} {Currency}).",
            payload.TenantId, payload.ProviderName, payload.OrderReference, payload.ExternalTransactionId,
            payload.Amount, payload.Currency);

        return Task.FromResult(OutboxHandlerResult.Processed($"Succeeded:{payload.ExternalTransactionId}"));
    }
}
