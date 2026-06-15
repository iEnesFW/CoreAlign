using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Providers.Payment.Events;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Providers.Payment.Outbox;

/// <summary>
/// Drains <c>Payment3DSecureRequired</c> outbox envelopes — emitted when a
/// charge is gated on issuer 3-D Secure. F3 notification subsystem will push
/// the cardholder a "complete verification" link from the same envelope.
/// </summary>
public sealed class Payment3DSecureRequiredOutboxHandler : IOutboxMessageHandler
{
    public const string MessageTypeKey = "Payment3DSecureRequired";

    public string MessageType => MessageTypeKey;

    private readonly ILogger<Payment3DSecureRequiredOutboxHandler> _logger;

    public Payment3DSecureRequiredOutboxHandler(ILogger<Payment3DSecureRequiredOutboxHandler> logger)
    {
        _logger = logger;
    }

    public Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return Task.FromResult(OutboxHandlerResult.Failed("Empty payload."));
        }

        Payment3DSecureRequiredEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<Payment3DSecureRequiredEvent>(
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
            "3DS required for tenant {TenantId} via {Provider} (transaction {TransactionId}, order {OrderReference}).",
            payload.TenantId, payload.ProviderName, payload.TransactionId, payload.OrderReference);

        return Task.FromResult(OutboxHandlerResult.Processed($"3DSRequired:{payload.TransactionId}"));
    }
}
