using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Providers.Payment.Events;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Providers.Payment.Outbox;

/// <summary>
/// Drains <c>PaymentInitiated</c> outbox envelopes produced by
/// <see cref="IPaymentDispatcher"/>. Today it logs structured telemetry so the
/// payment ledger has an auditable initiation breadcrumb; the F3 notification
/// subsystem will subscribe to the same envelope for fan-out (receipt e-mail,
/// in-app toast). Idempotent: replaying a row never mutates state.
/// </summary>
public sealed class PaymentInitiatedOutboxHandler : IOutboxMessageHandler
{
    public const string MessageTypeKey = "PaymentInitiated";

    public string MessageType => MessageTypeKey;

    private readonly ILogger<PaymentInitiatedOutboxHandler> _logger;

    public PaymentInitiatedOutboxHandler(ILogger<PaymentInitiatedOutboxHandler> logger)
    {
        _logger = logger;
    }

    public Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return Task.FromResult(OutboxHandlerResult.Failed("Empty payload."));
        }

        PaymentInitiatedEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<PaymentInitiatedEvent>(
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
            "Payment initiated for tenant {TenantId} via {Provider} (order {OrderReference}, amount {Amount} {Currency}).",
            payload.TenantId, payload.ProviderName, payload.OrderReference, payload.Amount, payload.Currency);

        return Task.FromResult(OutboxHandlerResult.Processed($"Initiated:{payload.OrderReference}"));
    }
}
