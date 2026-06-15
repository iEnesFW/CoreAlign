using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Application.Notifications.Templates;
using CoreAlign.Application.Providers;
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
    private readonly IProviderRegistry<IEmailProvider> _emailRegistry;
    private readonly IProviderRegistry<ISmsProvider> _smsRegistry;
    private readonly IProviderRegistry<IPushNotificationProvider> _pushRegistry;
    private readonly IProviderRegistry<IWhatsAppProvider> _whatsAppRegistry;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        INotificationTemplateRenderer renderer,
        INotificationMessageRepository messages,
        INotificationPreferenceRepository preferences,
        IUserRepository users,
        ICustomerRepository customers,
        IUserDeviceTokenRepository deviceTokens,
        IProviderRegistry<IEmailProvider> emailRegistry,
        IProviderRegistry<ISmsProvider> smsRegistry,
        IProviderRegistry<IPushNotificationProvider> pushRegistry,
        IProviderRegistry<IWhatsAppProvider> whatsAppRegistry,
        IUnitOfWork unitOfWork,
        ILogger<NotificationDispatcher> logger)
    {
        _renderer = renderer;
        _messages = messages;
        _preferences = preferences;
        _users = users;
        _customers = customers;
        _deviceTokens = deviceTokens;
        _emailRegistry = emailRegistry;
        _smsRegistry = smsRegistry;
        _pushRegistry = pushRegistry;
        _whatsAppRegistry = whatsAppRegistry;
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
                var pushResults = await DispatchPushAsync(request, rendered, payloadJson, recipientDeviceToken, utcNow, ct).ConfigureAwait(false);
                results.AddRange(pushResults);
                continue;
            }

            var address = ResolveChannelAddress(channel, recipientEmail, recipientPhone);
            if (string.IsNullOrWhiteSpace(address) && channel != NotificationChannel.InApp)
            {
                _logger.LogWarning("No recipient address for channel {Channel}; skipping", channel);
                continue;
            }

            var resolvedAddress = address ?? string.Empty;
            var hash = ComputeIdempotencyHash(request, channel, resolvedAddress);

            var existing = await _messages.GetByHashAsync(request.TenantId, hash, ct).ConfigureAwait(false);
            if (existing is not null && (existing.Status == NotificationStatus.Sent || existing.Status == NotificationStatus.Delivered || existing.Status == NotificationStatus.Read))
            {
                _logger.LogInformation("Duplicate dispatch suppressed for hash {Hash} / channel {Channel}; existing status {Status}", hash, channel, existing.Status);
                results.Add(NotificationSendResult.Ok(existing.ProviderMessageId));
                continue;
            }

            var message = existing ?? new NotificationMessage(
                request.TenantId,
                request.UserId,
                request.CustomerId,
                channel,
                request.TemplateKey,
                request.Locale,
                resolvedAddress,
                request.CategoryKey,
                rendered.Subject,
                rendered.BodyHtml,
                payloadJson,
                hash);

            message.MarkQueued(utcNow);
            await _messages.UpsertAsync(message, ct).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            NotificationSendResult sendResult;
            try
            {
                sendResult = await DispatchToChannelAsync(request.TenantId, channel, resolvedAddress, rendered, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provider invocation threw for channel {Channel} / hash {Hash}", channel, hash);
                message.MarkFailed(ex.Message, utcNow);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                throw;
            }

            if (sendResult.Success)
            {
                message.MarkSent(InferProviderName(channel), sendResult.ProviderMessageId, utcNow);
            }
            else
            {
                message.MarkFailed(sendResult.FailureReason ?? "Unknown failure", utcNow);
            }
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            results.Add(sendResult);
        }

        return results;
    }

    private async Task<IReadOnlyList<NotificationSendResult>> DispatchPushAsync(
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
            _logger.LogInformation(
                "No active device tokens for user {UserId} / customer {CustomerId}; skipping push",
                request.UserId,
                request.CustomerId);
            return new[] { NotificationSendResult.Fail("NoDeviceTokens") };
        }

        var provider = await _pushRegistry.TryResolveForTenantAsync(request.TenantId, ct).ConfigureAwait(false);
        if (provider is null)
        {
            return new[] { NotificationSendResult.Fail("No push provider configured") };
        }

        var payloadData = BuildPushPayloadData(request.Payload);
        var sendResults = new List<NotificationSendResult>(tokens.Count);

        foreach (var tokenInfo in tokens)
        {
            var hash = ComputeIdempotencyHash(request, NotificationChannel.Push, tokenInfo.Token);
            var existing = await _messages.GetByHashAsync(request.TenantId, hash, ct).ConfigureAwait(false);
            if (existing is not null && (existing.Status == NotificationStatus.Sent || existing.Status == NotificationStatus.Delivered || existing.Status == NotificationStatus.Read))
            {
                _logger.LogInformation("Duplicate push suppressed for token hash {Hash}; existing status {Status}", hash, existing.Status);
                sendResults.Add(NotificationSendResult.Ok(existing.ProviderMessageId));
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

            var push = new PushMessage(tokenInfo.Token, rendered.Subject ?? "Notification", rendered.BodyText, payloadData);
            NotificationSendResult sendResult;
            try
            {
                sendResult = await provider.SendAsync(push, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Push provider invocation threw for token hash {Hash}", hash);
                message.MarkFailed(ex.Message, utcNow);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                throw;
            }

            if (sendResult.Success)
            {
                message.MarkSent(InferProviderName(NotificationChannel.Push), sendResult.ProviderMessageId, utcNow);
                if (tokenInfo.TokenId.HasValue)
                {
                    await _deviceTokens.MarkLastUsedAsync(request.TenantId, tokenInfo.TokenId.Value, utcNow, ct).ConfigureAwait(false);
                }
            }
            else
            {
                message.MarkFailed(sendResult.FailureReason ?? "Unknown failure", utcNow);
            }
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            sendResults.Add(sendResult);
        }

        return sendResults;
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

    private static string? ResolveChannelAddress(NotificationChannel channel, string? email, string? phone) => channel switch
    {
        NotificationChannel.Email => email,
        NotificationChannel.Sms => phone,
        NotificationChannel.WhatsApp => phone,
        NotificationChannel.InApp => string.Empty,
        _ => null
    };

    private async Task<NotificationSendResult> DispatchToChannelAsync(Guid tenantId, NotificationChannel channel, string address, RenderedTemplate rendered, CancellationToken ct)
    {
        switch (channel)
        {
            case NotificationChannel.Email:
            {
                var provider = await _emailRegistry.TryResolveForTenantAsync(tenantId, ct).ConfigureAwait(false);
                if (provider is null) return NotificationSendResult.Fail("No email provider configured");
                var msg = new EmailMessage("noreply@corealign.local", "CoreAlign", address, rendered.Subject ?? string.Empty, rendered.BodyHtml, rendered.BodyText, null);
                return await provider.SendAsync(msg, ct).ConfigureAwait(false);
            }
            case NotificationChannel.Sms:
            {
                var provider = await _smsRegistry.TryResolveForTenantAsync(tenantId, ct).ConfigureAwait(false);
                if (provider is null) return NotificationSendResult.Fail("No SMS provider configured");
                var msg = new SmsMessage(string.Empty, address, rendered.BodyText);
                return await provider.SendAsync(msg, ct).ConfigureAwait(false);
            }
            case NotificationChannel.WhatsApp:
            {
                var provider = await _whatsAppRegistry.TryResolveForTenantAsync(tenantId, ct).ConfigureAwait(false);
                if (provider is null) return NotificationSendResult.Fail("No WhatsApp provider configured");
                var msg = new WhatsAppMessage(string.Empty, address, "generic", "en", rendered.BodyText);
                return await provider.SendAsync(msg, ct).ConfigureAwait(false);
            }
            case NotificationChannel.InApp:
                return NotificationSendResult.Ok();
            default:
                return NotificationSendResult.Fail($"Unknown channel: {channel}");
        }
    }

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
