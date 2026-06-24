using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CoreAlign.Application.Notifications.Delivery;
using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Application.Notifications.Templates;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Notifications;

public sealed class NotificationDispatcher : INotificationDispatcher
{
    private static readonly IReadOnlyList<NotificationChannel> DefaultChannels = new[]
    {
        NotificationChannel.InApp,
        NotificationChannel.Email
    };

    private readonly INotificationTemplateRenderer _renderer;
    private readonly INotificationMessageRepository _messages;
    private readonly INotificationPreferenceRepository _preferences;
    private readonly IUserRepository _users;
    private readonly ICustomerRepository _customers;
    private readonly IUserDeviceTokenRepository _deviceTokens;
    private readonly INotificationDeliveryQueue _deliveryQueue;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        INotificationTemplateRenderer renderer,
        INotificationMessageRepository messages,
        INotificationPreferenceRepository preferences,
        IUserRepository users,
        ICustomerRepository customers,
        IUserDeviceTokenRepository deviceTokens,
        INotificationDeliveryQueue deliveryQueue,
        IUnitOfWork unitOfWork,
        ILogger<NotificationDispatcher> logger)
    {
        _renderer = renderer;
        _messages = messages;
        _preferences = preferences;
        _users = users;
        _customers = customers;
        _deviceTokens = deviceTokens;
        _deliveryQueue = deliveryQueue;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NotificationSendResult>> DispatchAsync(NotificationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var channels = request.ChannelsOverride ?? DefaultChannels;
        var (recipientEmail, recipientPhone, recipientDeviceToken) = await ResolveRecipientAddressesAsync(request, ct).ConfigureAwait(false);
        var payloadJson = SerializePayload(request.Payload);
        var results = new List<NotificationSendResult>();
        var utcNow = DateTime.UtcNow;

        foreach (var channel in channels)
        {
            if (request.UserId.HasValue)
            {
                var pref = await _preferences.GetAsync(request.TenantId, request.UserId.Value, request.CategoryKey, channel, ct).ConfigureAwait(false);
                if (pref is not null && !pref.IsEnabled)
                {
                    _logger.LogInformation("Notification skipped for user {UserId} / category {Category} / channel {Channel} (opted out)", request.UserId, request.CategoryKey, channel);
                    continue;
                }
            }

            RenderedTemplate rendered;
            try
            {
                rendered = await _renderer.RenderAsync(request.TenantId, request.TemplateKey, channel, request.Locale, request.Payload, ct).ConfigureAwait(false);
            }
            catch (TemplateNotFoundException tnf)
            {
                _logger.LogWarning(tnf, "Template not found for {Key} / {Locale}", request.TemplateKey, request.Locale);
                results.Add(NotificationSendResult.Fail($"Template not found: {request.TemplateKey}"));
                continue;
            }

            if (channel == NotificationChannel.Push)
            {
                results.AddRange(await QueuePushAsync(request, rendered, payloadJson, recipientDeviceToken, utcNow, ct).ConfigureAwait(false));
                continue;
            }

            var address = channel == NotificationChannel.InApp
                ? ResolveInAppAddress(request)
                : ResolveChannelAddress(channel, recipientEmail, recipientPhone);
            if (string.IsNullOrWhiteSpace(address))
            {
                _logger.LogWarning("No recipient address for channel {Channel}; skipping", channel);
                continue;
            }

            results.Add(await QueueChannelAsync(request, channel, address, rendered, payloadJson, utcNow, ct).ConfigureAwait(false));
        }

        return results;
    }

    private async Task<NotificationSendResult> QueueChannelAsync(
        NotificationRequest request,
        NotificationChannel channel,
        string address,
        RenderedTemplate rendered,
        string payloadJson,
        DateTime utcNow,
        CancellationToken ct)
    {
        var hash = ComputeIdempotencyHash(request, channel, address);
        var existing = await _messages.GetByHashAsync(request.TenantId, hash, ct).ConfigureAwait(false);
        if (existing is not null && IsTerminal(existing.Status))
        {
            _logger.LogInformation("Duplicate dispatch suppressed for hash {Hash} / channel {Channel}; existing status {Status}", hash, channel, existing.Status);
            return NotificationSendResult.Ok(existing.ProviderMessageId);
        }

        if (existing is not null && existing.Status is NotificationStatus.Queued or NotificationStatus.Sending)
        {
            return NotificationSendResult.Ok(existing.ProviderMessageId);
        }

        var message = existing ?? new NotificationMessage(
            request.TenantId,
            request.UserId,
            request.CustomerId,
            channel,
            request.TemplateKey,
            request.Locale,
            address,
            request.CategoryKey,
            rendered.Subject,
            rendered.BodyHtml,
            payloadJson,
            hash);

        if (channel == NotificationChannel.InApp)
        {
            message.MarkSent(InferProviderName(channel), null, utcNow);
            await _messages.UpsertAsync(message, ct).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            return NotificationSendResult.Ok();
        }

        message.MarkQueued(utcNow);
        await _messages.UpsertAsync(message, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        await _deliveryQueue.EnqueueChannelSendAsync(
            new NotificationChannelSendPayload(
                request.TenantId,
                message.Id,
                channel,
                address,
                rendered.Subject,
                rendered.BodyHtml,
                rendered.BodyText,
                channel == NotificationChannel.Email ? request.ReplyToOverride : null,
                Cc: null,
                Bcc: null,
                Attachments: channel == NotificationChannel.Email ? MapAttachments(request.Attachments) : null),
            ct).ConfigureAwait(false);

        return NotificationSendResult.Ok();
    }

    private async Task<IReadOnlyList<NotificationSendResult>> QueuePushAsync(
        NotificationRequest request,
        RenderedTemplate rendered,
        string payloadJson,
        string? recipientDeviceTokenOverride,
        DateTime utcNow,
        CancellationToken ct)
    {
        var tokens = await ResolvePushTokensAsync(request, recipientDeviceTokenOverride, ct).ConfigureAwait(false);
        if (tokens.Count == 0)
        {
            _logger.LogInformation("No active device tokens for user {UserId} / customer {CustomerId}; skipping push", request.UserId, request.CustomerId);
            return new[] { NotificationSendResult.Fail("NoDeviceTokens") };
        }

        var pushData = BuildPushPayloadData(request.Payload);
        var results = new List<NotificationSendResult>(tokens.Count);

        foreach (var tokenInfo in tokens)
        {
            var hash = ComputeIdempotencyHash(request, NotificationChannel.Push, tokenInfo.Token);
            var existing = await _messages.GetByHashAsync(request.TenantId, hash, ct).ConfigureAwait(false);
            if (existing is not null && (IsTerminal(existing.Status) || existing.Status is NotificationStatus.Queued or NotificationStatus.Sending))
            {
                results.Add(NotificationSendResult.Ok(existing.ProviderMessageId));
                continue;
            }

            var message = existing ?? new NotificationMessage(
                request.TenantId,
                request.UserId,
                request.CustomerId,
                NotificationChannel.Push,
                request.TemplateKey,
                request.Locale,
                tokenInfo.Token,
                request.CategoryKey,
                rendered.Subject,
                rendered.BodyHtml,
                payloadJson,
                hash);

            message.MarkQueued(utcNow);
            await _messages.UpsertAsync(message, ct).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            await _deliveryQueue.EnqueueChannelSendAsync(
                new NotificationChannelSendPayload(
                    request.TenantId,
                    message.Id,
                    NotificationChannel.Push,
                    tokenInfo.Token,
                    rendered.Subject,
                    rendered.BodyHtml,
                    rendered.BodyText,
                    PushData: pushData,
                    DeviceTokenId: tokenInfo.TokenId),
                ct).ConfigureAwait(false);

            results.Add(NotificationSendResult.Ok());
        }

        return results;
    }

    private async Task<IReadOnlyList<PushTokenRef>> ResolvePushTokensAsync(
        NotificationRequest request,
        string? recipientDeviceTokenOverride,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(recipientDeviceTokenOverride))
        {
            return new[] { new PushTokenRef(recipientDeviceTokenOverride!, null) };
        }

        if (request.UserId.HasValue)
        {
            var userTokens = await _deviceTokens.ListActiveByUserAsync(request.TenantId, request.UserId.Value, ct).ConfigureAwait(false);
            return userTokens.Select(t => new PushTokenRef(t.Token, t.Id)).ToList();
        }

        if (request.CustomerId.HasValue)
        {
            var customerTokens = await _deviceTokens.ListActiveByCustomerAsync(request.TenantId, request.CustomerId.Value, ct).ConfigureAwait(false);
            return customerTokens.Select(t => new PushTokenRef(t.Token, t.Id)).ToList();
        }

        return Array.Empty<PushTokenRef>();
    }

    private static IReadOnlyList<EmailAttachmentPayload>? MapAttachments(IReadOnlyList<EmailAttachment>? attachments)
    {
        if (attachments is null || attachments.Count == 0) return null;
        return attachments
            .Select(a => new EmailAttachmentPayload(a.FileName, a.ContentType, Convert.ToBase64String(a.Content)))
            .ToList();
    }

    private static IReadOnlyDictionary<string, string>? BuildPushPayloadData(object payload)
    {
        if (payload is IReadOnlyDictionary<string, string> stringDict)
            return stringDict;

        if (payload is IDictionary<string, object?> objectDict)
        {
            var result = new Dictionary<string, string>(objectDict.Count, StringComparer.Ordinal);
            foreach (var kvp in objectDict)
            {
                if (kvp.Value is null) continue;
                result[kvp.Key] = kvp.Value.ToString() ?? string.Empty;
            }
            return result;
        }

        return null;
    }

    private readonly record struct PushTokenRef(string Token, Guid? TokenId);

    private async Task<(string? Email, string? Phone, string? DeviceToken)> ResolveRecipientAddressesAsync(NotificationRequest request, CancellationToken ct)
    {
        string? email = request.RecipientEmailOverride;
        string? phone = request.RecipientPhoneOverride;
        string? device = request.RecipientDeviceTokenOverride;

        if (request.UserId.HasValue && string.IsNullOrEmpty(email))
        {
            var user = await _users.GetByIdAsync(request.UserId.Value, ct).ConfigureAwait(false);
            if (user is not null)
            {
                email ??= user.Email;
                phone ??= user.PhoneNumber;
            }
        }

        if (request.CustomerId.HasValue && string.IsNullOrEmpty(email))
        {
            var customer = await _customers.GetByIdAsync(request.CustomerId.Value, ct).ConfigureAwait(false);
            if (customer is not null)
            {
                email ??= customer.Email;
                phone ??= customer.Phone;
            }
        }

        return (email, phone, device);
    }

    private static string ResolveInAppAddress(NotificationRequest request) =>
        request.UserId?.ToString("N") ?? request.CustomerId?.ToString("N") ?? "inapp";

    private static string? ResolveChannelAddress(NotificationChannel channel, string? email, string? phone) => channel switch
    {
        NotificationChannel.Email => email,
        NotificationChannel.Sms => phone,
        NotificationChannel.WhatsApp => phone,
        NotificationChannel.InApp => string.Empty,
        _ => null
    };

    private static bool IsTerminal(NotificationStatus status) =>
        status is NotificationStatus.Sent or NotificationStatus.Delivered or NotificationStatus.Read;

    private static string InferProviderName(NotificationChannel channel) => channel.ToString().ToLowerInvariant();

    private static string SerializePayload(object payload)
    {
        try
        {
            return JsonSerializer.Serialize(payload);
        }
        catch
        {
            return "{}";
        }
    }

    private static string ComputeIdempotencyHash(NotificationRequest request, NotificationChannel channel, string address)
    {
        var sb = new StringBuilder(256);
        sb.Append(request.TenantId.ToString("N"));
        sb.Append('|');
        sb.Append(request.UserId?.ToString("N") ?? "_");
        sb.Append('|');
        sb.Append(request.CustomerId?.ToString("N") ?? "_");
        sb.Append('|');
        sb.Append(channel);
        sb.Append('|');
        sb.Append(request.TemplateKey);
        sb.Append('|');
        sb.Append(request.Locale);
        sb.Append('|');
        sb.Append(request.CategoryKey);
        sb.Append('|');
        sb.Append(address);
        sb.Append('|');
        sb.Append(SerializePayload(request.Payload));

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var digest = SHA256.HashData(bytes);
        return Convert.ToHexString(digest);
    }
}
