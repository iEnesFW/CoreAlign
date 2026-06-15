using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Providers.Payment.Handlers;

public sealed class PaymentWebhookEventHandler : IOutboxMessageHandler
{
    public const string MessageTypeKey = "provider.payment.webhook.replay";

    public string MessageType => MessageTypeKey;

    private readonly IProviderWebhookInboxRepository _inboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentWebhookEventHandler> _logger;

    public PaymentWebhookEventHandler(
        IProviderWebhookInboxRepository inboxRepository,
        IUnitOfWork unitOfWork,
        ILogger<PaymentWebhookEventHandler> logger)
    {
        _inboxRepository = inboxRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return OutboxHandlerResult.Failed("Empty payload.");
        }

        PaymentWebhookReplayEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<PaymentWebhookReplayEnvelope>(
                payloadJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException ex)
        {
            return OutboxHandlerResult.Failed($"Invalid envelope JSON: {ex.Message}");
        }

        if (envelope is null || envelope.InboxId == Guid.Empty)
        {
            return OutboxHandlerResult.Failed("Envelope missing inboxId.");
        }

        var entry = await _inboxRepository.GetByIdAsync(envelope.InboxId, cancellationToken);
        if (entry is null)
        {
            return OutboxHandlerResult.Failed($"Webhook inbox row {envelope.InboxId} not found.");
        }

        try
        {
            entry.MarkProcessed(DateTime.UtcNow);
            _inboxRepository.Update(entry);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Payment webhook {InboxId} replayed for provider {Provider} (event {Event}).",
                entry.Id, entry.ProviderName, entry.EventType);

            return OutboxHandlerResult.Processed($"Replayed:{entry.Id}");
        }
        catch (Exception ex)
        {
            entry.MarkFailed(ex.Message, DateTime.UtcNow);
            _inboxRepository.Update(entry);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "Failed to replay Payment webhook {InboxId}.", entry.Id);
            return OutboxHandlerResult.Failed(ex.Message);
        }
    }

    public sealed record PaymentWebhookReplayEnvelope(
        Guid InboxId,
        string? ProviderName,
        string? Category,
        string? EventType,
        string? RawPayload,
        DateTime? ReplayedAtUtc);
}
