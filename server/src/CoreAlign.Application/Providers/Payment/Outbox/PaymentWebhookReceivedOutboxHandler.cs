using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Providers.Payment.Events;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Providers.Payment.Outbox;

/// <summary>
/// Drains <c>PaymentWebhookReceived</c> outbox envelopes enqueued by the
/// <c>PaymentWebhooksController</c> at the moment of receipt. Distinct from
/// <c>PaymentWebhookEventHandler</c> which replays the inbox row; this handler
/// is the audit/telemetry breadcrumb for the raw inbound signature.
/// </summary>
public sealed class PaymentWebhookReceivedOutboxHandler : IOutboxMessageHandler
{
    public const string MessageTypeKey = "PaymentWebhookReceived";

    public string MessageType => MessageTypeKey;

    private readonly ILogger<PaymentWebhookReceivedOutboxHandler> _logger;

    public PaymentWebhookReceivedOutboxHandler(ILogger<PaymentWebhookReceivedOutboxHandler> logger)
    {
        _logger = logger;
    }

    public Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return Task.FromResult(OutboxHandlerResult.Failed("Empty payload."));
        }

        PaymentWebhookReceivedEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<PaymentWebhookReceivedEvent>(
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
            "Payment webhook received for tenant {TenantId} via {Provider} (event {EventType}, signatureHash {SignatureHash}).",
            payload.TenantId, payload.ProviderName, payload.EventType, payload.SignatureHash);

        return Task.FromResult(OutboxHandlerResult.Processed($"WebhookReceived:{payload.EventType}"));
    }
}
