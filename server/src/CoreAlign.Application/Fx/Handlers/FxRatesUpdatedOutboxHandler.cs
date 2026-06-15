using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Fx.Events;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Fx.Handlers;

public sealed class FxRatesUpdatedOutboxHandler : IOutboxMessageHandler
{
    public const string MessageTypeKey = "FxRatesUpdated";

    public string MessageType => MessageTypeKey;

    private readonly ILogger<FxRatesUpdatedOutboxHandler> _logger;

    public FxRatesUpdatedOutboxHandler(ILogger<FxRatesUpdatedOutboxHandler> logger)
    {
        _logger = logger;
    }

    public Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return Task.FromResult(OutboxHandlerResult.Failed("Empty payload."));
        }

        FxRatesUpdatedEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<FxRatesUpdatedEvent>(
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
            "FX rates updated: {Count} rates for {Date:yyyy-MM-dd} from {Source} (fetched {FetchedAt:o}).",
            payload.RateCount,
            payload.EffectiveDate,
            payload.Source,
            payload.FetchedAtUtc);

        return Task.FromResult(OutboxHandlerResult.Processed($"FxRatesUpdated:{payload.Source}:{payload.EffectiveDate:yyyy-MM-dd}:{payload.RateCount}"));
    }
}
