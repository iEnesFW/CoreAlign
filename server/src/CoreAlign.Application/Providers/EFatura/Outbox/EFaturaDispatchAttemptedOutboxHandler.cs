using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Providers.EFatura.Events;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Providers.EFatura.Outbox;

/// <summary>
/// Drains <c>EFaturaDispatchAttempted</c> outbox envelopes emitted by
/// <see cref="IEFaturaDispatcher"/>. Records the attempt outcome for ops
/// dashboards and feeds the F3 notification subsystem when provider failures
/// require finance escalation.
/// </summary>
public sealed class EFaturaDispatchAttemptedOutboxHandler : IOutboxMessageHandler
{
    public const string MessageTypeKey = "EFaturaDispatchAttempted";

    public string MessageType => MessageTypeKey;

    private readonly ILogger<EFaturaDispatchAttemptedOutboxHandler> _logger;

    public EFaturaDispatchAttemptedOutboxHandler(ILogger<EFaturaDispatchAttemptedOutboxHandler> logger)
    {
        _logger = logger;
    }

    public Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return Task.FromResult(OutboxHandlerResult.Failed("Empty payload."));
        }

        EFaturaDispatchAttemptedEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<EFaturaDispatchAttemptedEvent>(
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

        if (payload.Succeeded)
        {
            _logger.LogInformation(
                "EFatura dispatch succeeded for tenant {TenantId} via {Provider} (document {Document}, duration {DurationMs}ms).",
                payload.TenantId, payload.ProviderName, payload.DocumentNumber, payload.Duration.TotalMilliseconds);
        }
        else
        {
            _logger.LogWarning(
                "EFatura dispatch failed for tenant {TenantId} via {Provider} (document {Document}, error {Error}).",
                payload.TenantId, payload.ProviderName, payload.DocumentNumber, payload.ErrorMessage);
        }

        return Task.FromResult(OutboxHandlerResult.Processed($"DispatchAttempted:{payload.DocumentNumber}"));
    }
}
