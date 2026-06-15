using System.Text.Json;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Notifications.Webhooks;

public sealed class NotificationStatusUpdater : INotificationStatusUpdater
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly INotificationMessageRepository _messages;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<NotificationStatusUpdater> _logger;

    public NotificationStatusUpdater(
        INotificationMessageRepository messages,
        IUnitOfWork uow,
        ILogger<NotificationStatusUpdater> logger)
    {
        _messages = messages;
        _uow = uow;
        _logger = logger;
    }

    public async Task<NotificationStatusUpdateResult> UpdateFromWebhookAsync(
        Guid tenantId,
        string providerName,
        string rawBody,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ArgumentException("ProviderName is required.", nameof(providerName));
        }

        var payload = TryParse(rawBody);
        if (payload is null || string.IsNullOrWhiteSpace(payload.ProviderMessageId) || string.IsNullOrWhiteSpace(payload.EventType))
        {
            _logger.LogWarning(
                "Notification webhook payload for provider {Provider} tenant {TenantId} missing ProviderMessageId or EventType; skipping status update.",
                providerName,
                tenantId);
            return new NotificationStatusUpdateResult(false, false, payload?.EventType, payload?.ProviderMessageId);
        }

        var message = await _messages
            .GetByProviderMessageIdAsync(tenantId, providerName, payload.ProviderMessageId, cancellationToken)
            .ConfigureAwait(false);

        if (message is null)
        {
            _logger.LogInformation(
                "Notification message not found for provider {Provider} providerMessageId {ProviderMessageId} tenant {TenantId}; webhook persisted in inbox only.",
                providerName,
                payload.ProviderMessageId,
                tenantId);
            return new NotificationStatusUpdateResult(false, false, payload.EventType, payload.ProviderMessageId);
        }

        var occurred = payload.OccurredAtUtc ?? DateTime.UtcNow;
        var statusChanged = ApplyEvent(message, payload.EventType, occurred);

        if (statusChanged)
        {
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new NotificationStatusUpdateResult(true, statusChanged, payload.EventType, payload.ProviderMessageId);
    }

    private static bool ApplyEvent(NotificationMessage message, string eventType, DateTime occurred)
    {
        switch (eventType.Trim().ToLowerInvariant())
        {
            case "delivered":
                if (message.Status == NotificationStatus.Delivered || message.Status == NotificationStatus.Read)
                {
                    return false;
                }
                message.MarkDelivered(occurred);
                return true;
            case "bounce":
            case "bounced":
                message.MarkBounced(eventType, occurred);
                return true;
            case "opened":
            case "read":
                if (message.Status == NotificationStatus.Read)
                {
                    return false;
                }
                message.MarkRead(occurred);
                return true;
            case "failed":
                message.MarkFailed(eventType, occurred);
                return true;
            default:
                return false;
        }
    }

    private static WebhookStatusPayload? TryParse(string rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<WebhookStatusPayload>(rawBody, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record WebhookStatusPayload(string? ProviderMessageId, string? EventType, DateTime? OccurredAtUtc);
}
