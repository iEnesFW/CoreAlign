using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Mrp;

public sealed class MrpSuggestionsCreatedOutboxHandler : IOutboxMessageHandler
{
    public const string MessageTypeKey = "MrpSuggestionsCreated";

    private readonly ILogger<MrpSuggestionsCreatedOutboxHandler> _logger;

    public MrpSuggestionsCreatedOutboxHandler(ILogger<MrpSuggestionsCreatedOutboxHandler> logger) => _logger = logger;

    public string MessageType => MessageTypeKey;

    public Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return Task.FromResult(OutboxHandlerResult.Failed("Empty payload."));
        }

        try
        {
            var evt = JsonSerializer.Deserialize<MrpSuggestionsCreatedEvent>(payloadJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
            if (evt is null)
            {
                return Task.FromResult(OutboxHandlerResult.Failed("Payload deserialized to null."));
            }

            _logger.LogInformation(
                "MRP suggestions created: {ReqCount} requisitions, {LineCount} lines, tenant {TenantId}, asOf {AsOfDate}.",
                evt.RequisitionCount, evt.LineCount, evt.TenantId, evt.AsOfDate);
            return Task.FromResult(OutboxHandlerResult.Processed($"MrpSuggestionsCreated:{evt.RequisitionCount}"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(OutboxHandlerResult.Failed($"Invalid payload JSON: {ex.Message}"));
        }
    }
}
