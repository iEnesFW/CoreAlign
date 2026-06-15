using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Installation.Subscribers;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Installation.Outbox;

public sealed class InstallationAcceptedOutboxHandler : IOutboxMessageHandler
{
    public string MessageType => InstallationAcceptedFinalInvoiceTrigger.MessageTypeKey;

    private readonly ILogger<InstallationAcceptedOutboxHandler> _logger;
    public InstallationAcceptedOutboxHandler(ILogger<InstallationAcceptedOutboxHandler> logger) => _logger = logger;

    public Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return Task.FromResult(OutboxHandlerResult.Failed("Empty payload."));

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var acceptanceId = doc.RootElement.GetProperty("AcceptanceId").GetGuid();
            _logger.LogInformation(
                "InstallationAccepted outbox processed for acceptance {AcceptanceId}; downstream final-invoice generation hook deferred to F2.1 wiring.",
                acceptanceId);
            return Task.FromResult(OutboxHandlerResult.Processed($"InstallationAccepted:{acceptanceId}"));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(OutboxHandlerResult.Failed($"Invalid payload JSON: {ex.Message}"));
        }
    }
}
