using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Application.Providers;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Notifications.Delivery;

public sealed class NotificationChannelSendOutboxHandler : IOutboxMessageHandler
{
    public string MessageType => NotificationDeliveryQueue.MessageType;

    private readonly INotificationMessageRepository _messages;
    private readonly IProviderRegistry<IEmailProvider> _emailRegistry;
    private readonly IProviderRegistry<ISmsProvider> _smsRegistry;
    private readonly IProviderRegistry<IPushNotificationProvider> _pushRegistry;
    private readonly IProviderRegistry<IWhatsAppProvider> _whatsAppRegistry;
    private readonly IUserDeviceTokenRepository _deviceTokens;
    private readonly INotificationRateLimiter _rateLimiter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationChannelSendOutboxHandler> _logger;

    public NotificationChannelSendOutboxHandler(
        INotificationMessageRepository messages,
        IProviderRegistry<IEmailProvider> emailRegistry,
        IProviderRegistry<ISmsProvider> smsRegistry,
        IProviderRegistry<IPushNotificationProvider> pushRegistry,
        IProviderRegistry<IWhatsAppProvider> whatsAppRegistry,
        IUserDeviceTokenRepository deviceTokens,
        INotificationRateLimiter rateLimiter,
        IUnitOfWork unitOfWork,
        ILogger<NotificationChannelSendOutboxHandler> logger)
    {
        _messages = messages;
        _emailRegistry = emailRegistry;
        _smsRegistry = smsRegistry;
        _pushRegistry = pushRegistry;
        _whatsAppRegistry = whatsAppRegistry;
        _deviceTokens = deviceTokens;
        _rateLimiter = rateLimiter;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<OutboxHandlerResult> HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var payload = NotificationDeliveryQueue.Deserialize(payloadJson);
        if (payload is null) return OutboxHandlerResult.Failed("Payload deserialized to null.");

        var message = await _messages.GetByIdAsync(payload.TenantId, payload.NotificationMessageId, cancellationToken).ConfigureAwait(false);
        if (message is null) return OutboxHandlerResult.Processed("Notification message not found.");

        if (message.Status is NotificationStatus.Sent or NotificationStatus.Delivered or NotificationStatus.Read)
        {
            return OutboxHandlerResult.Processed("Already sent.");
        }

        var providerName = await ResolveProviderNameAsync(payload, cancellationToken).ConfigureAwait(false);
        if (providerName is null)
        {
            var reason = $"No provider configured for channel {payload.Channel}";
            message.MarkFailed(reason, DateTime.UtcNow);
            await _messages.UpsertAsync(message, cancellationToken).ConfigureAwait(false);
            return OutboxHandlerResult.Failed(reason);
        }

        var decision = await _rateLimiter.TryAcquireAsync(payload.TenantId, providerName, payload.Address, cancellationToken).ConfigureAwait(false);
        if (!decision.Allowed)
        {
            return OutboxHandlerResult.Deferred(decision.Reason ?? "Rate limited", decision.WindowEndUtc);
        }

        NotificationSendResult sendResult;
        try
        {
            sendResult = await SendAsync(payload, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Channel provider threw for message {MessageId} / channel {Channel}", payload.NotificationMessageId, payload.Channel);
            sendResult = NotificationSendResult.Fail(ex.Message);
        }

        var utcNow = DateTime.UtcNow;
        if (sendResult.Success)
        {
            message.MarkSent(providerName, sendResult.ProviderMessageId, utcNow);
            await _messages.UpsertAsync(message, cancellationToken).ConfigureAwait(false);
            if (payload.Channel == NotificationChannel.Push && payload.DeviceTokenId.HasValue)
            {
                await _deviceTokens.MarkLastUsedAsync(payload.TenantId, payload.DeviceTokenId.Value, utcNow, cancellationToken).ConfigureAwait(false);
            }
            return OutboxHandlerResult.Processed(sendResult.ProviderMessageId ?? "Sent");
        }

        message.MarkFailed(sendResult.FailureReason ?? "Unknown failure", utcNow);
        await _messages.UpsertAsync(message, cancellationToken).ConfigureAwait(false);
        return OutboxHandlerResult.Failed(sendResult.FailureReason ?? "Unknown failure");
    }

    private async Task<string?> ResolveProviderNameAsync(NotificationChannelSendPayload payload, CancellationToken ct) => payload.Channel switch
    {
        NotificationChannel.Email => (await _emailRegistry.TryResolveForTenantAsync(payload.TenantId, ct).ConfigureAwait(false))?.Name,
        NotificationChannel.Sms => (await _smsRegistry.TryResolveForTenantAsync(payload.TenantId, ct).ConfigureAwait(false))?.Name,
        NotificationChannel.WhatsApp => (await _whatsAppRegistry.TryResolveForTenantAsync(payload.TenantId, ct).ConfigureAwait(false))?.Name,
        NotificationChannel.Push => (await _pushRegistry.TryResolveForTenantAsync(payload.TenantId, ct).ConfigureAwait(false))?.Name,
        _ => null,
    };

    private async Task<NotificationSendResult> SendAsync(NotificationChannelSendPayload payload, CancellationToken ct)
    {
        switch (payload.Channel)
        {
            case NotificationChannel.Email:
            {
                var provider = await _emailRegistry.TryResolveForTenantAsync(payload.TenantId, ct).ConfigureAwait(false);
                if (provider is null) return NotificationSendResult.Fail("No email provider configured");
                var email = new EmailMessage(
                    string.Empty,
                    string.Empty,
                    payload.Address,
                    payload.Subject ?? string.Empty,
                    payload.BodyHtml,
                    payload.BodyText,
                    payload.ReplyTo,
                    payload.Cc,
                    payload.Bcc,
                    DecodeAttachments(payload.Attachments));
                return await provider.SendAsync(email, ct).ConfigureAwait(false);
            }
            case NotificationChannel.Sms:
            {
                var provider = await _smsRegistry.TryResolveForTenantAsync(payload.TenantId, ct).ConfigureAwait(false);
                if (provider is null) return NotificationSendResult.Fail("No SMS provider configured");
                return await provider.SendAsync(new SmsMessage(string.Empty, payload.Address, payload.BodyText), ct).ConfigureAwait(false);
            }
            case NotificationChannel.WhatsApp:
            {
                var provider = await _whatsAppRegistry.TryResolveForTenantAsync(payload.TenantId, ct).ConfigureAwait(false);
                if (provider is null) return NotificationSendResult.Fail("No WhatsApp provider configured");
                return await provider.SendAsync(new WhatsAppMessage(string.Empty, payload.Address, "generic", "en", payload.BodyText), ct).ConfigureAwait(false);
            }
            case NotificationChannel.Push:
            {
                var provider = await _pushRegistry.TryResolveForTenantAsync(payload.TenantId, ct).ConfigureAwait(false);
                if (provider is null) return NotificationSendResult.Fail("No push provider configured");
                return await provider.SendAsync(new PushMessage(payload.Address, payload.Subject ?? "Notification", payload.BodyText, payload.PushData), ct).ConfigureAwait(false);
            }
            default:
                return NotificationSendResult.Fail($"Unsupported channel: {payload.Channel}");
        }
    }

    private static IReadOnlyList<EmailAttachment>? DecodeAttachments(IReadOnlyList<EmailAttachmentPayload>? attachments)
    {
        if (attachments is null || attachments.Count == 0) return null;
        var decoded = new List<EmailAttachment>(attachments.Count);
        foreach (var attachment in attachments)
        {
            decoded.Add(new EmailAttachment(attachment.FileName, attachment.ContentType, Convert.FromBase64String(attachment.ContentBase64)));
        }
        return decoded;
    }
}
