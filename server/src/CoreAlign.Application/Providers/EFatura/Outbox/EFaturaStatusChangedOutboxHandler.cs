using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Providers.EFatura.Events;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Providers.EFatura.Outbox;

/// <summary>
/// Drains <c>EFaturaStatusChanged</c> outbox envelopes emitted by the
/// reconciliation job. F3 notification subsystem listens to surface state
/// transitions (e.g. <c>Accepted</c>, <c>Rejected</c>) to finance ops.
/// </summary>
public sealed class EFaturaStatusChangedOutboxHandler : IOutboxMessageHandler
{
    public const string MessageTypeKey = "EFaturaStatusChanged";

    public string MessageType => MessageTypeKey;

    private readonly ILogger<EFaturaStatusChangedOutboxHandler> _logger;

    public EFaturaStatusChangedOutboxHandler(ILogger<EFaturaStatusChangedOutboxHandler> logger)
    {
        _logger = logger;
    }

    public Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return Task.FromResult(OutboxHandlerResult.Failed("Empty payload."));
        }

        EFaturaStatusChangedEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<EFaturaStatusChangedEvent>(
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
            "EFatura status changed for tenant {TenantId} via {Provider} (ettn {Ettn}, {Previous} -> {Current}).",
            payload.TenantId, payload.ProviderName, payload.Ettn,
            payload.PreviousStatus ?? "(none)", payload.CurrentStatus);

        return Task.FromResult(OutboxHandlerResult.Processed($"StatusChanged:{payload.Ettn}"));
    }
}
