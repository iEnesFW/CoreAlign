using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Providers.Payment.Events;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Providers.Payment.Outbox;

/// <summary>
/// Drains <c>PaymentFailed</c> outbox envelopes. F3 notification subsystem
/// listens to surface decline reasons to the checkout UI and to finance ops.
/// </summary>
public sealed class PaymentFailedOutboxHandler : IOutboxMessageHandler
{
    public const string MessageTypeKey = "PaymentFailed";

    public string MessageType => MessageTypeKey;

    private readonly ILogger<PaymentFailedOutboxHandler> _logger;

    public PaymentFailedOutboxHandler(ILogger<PaymentFailedOutboxHandler> logger)
    {
        _logger = logger;
    }

    public Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return Task.FromResult(OutboxHandlerResult.Failed("Empty payload."));
        }

        PaymentFailedEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<PaymentFailedEvent>(
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

        _logger.LogWarning(
            "Payment failed for tenant {TenantId} via {Provider} (order {OrderReference}, code {Code}, message {Message}).",
            payload.TenantId, payload.ProviderName, payload.OrderReference,
            payload.FailureCode, payload.FailureMessage);

        return Task.FromResult(OutboxHandlerResult.Processed($"Failed:{payload.OrderReference}"));
    }
}
