using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using CoreAlign.Application.Common;
using CoreAlign.Application.Notifications.Webhooks;
using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/webhooks/notifications")]
public class NotificationWebhooksController : ControllerBase
{
    private const string TenantHeaderName = "X-CoreAlign-Tenant-Id";
    private const int MaxBodySize = 256 * 1024;

    private readonly IWebhookSignatureVerifier _verifier;
    private readonly IProviderWebhookInboxRepository _inbox;
    private readonly INotificationStatusUpdater _statusUpdater;
    private readonly ILogger<NotificationWebhooksController> _logger;

    public NotificationWebhooksController(
        IWebhookSignatureVerifier verifier,
        IProviderWebhookInboxRepository inbox,
        INotificationStatusUpdater statusUpdater,
        ILogger<NotificationWebhooksController> logger)
    {
        _verifier = verifier;
        _inbox = inbox;
        _statusUpdater = statusUpdater;
        _logger = logger;
    }

    [HttpPost("{providerName}")]
    public async Task<IActionResult> Handle(string providerName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return BadRequest(ApiResponse<object>.Failure("Provider name is required.", 400));
        }

        if (Request.ContentLength is null || Request.ContentLength > MaxBodySize)
        {
            return BadRequest(ApiResponse<object>.Failure("Webhook payload missing or too large.", 400));
        }

        Request.EnableBuffering();
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true, detectEncodingFromByteOrderMarks: false))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken);
        }
        Request.Body.Position = 0;

        var headers = Request.Headers
            .ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        if (!headers.TryGetValue(TenantHeaderName, out var tenantHeader) || !Guid.TryParse(tenantHeader, out var tenantId))
        {
            return BadRequest(ApiResponse<object>.Failure($"Header {TenantHeaderName} missing or invalid.", 400));
        }

        bool verified;
        try
        {
            verified = await _verifier.VerifyAsync(providerName, rawBody, headers, tenantId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Notification webhook verification threw for provider {Provider} tenant {TenantId}.",
                providerName,
                tenantId);
            return Unauthorized(ApiResponse<object>.Failure("Webhook signature verification failed.", 401));
        }

        if (!verified)
        {
            return Unauthorized(ApiResponse<object>.Failure("Invalid webhook signature.", 401));
        }

        var signatureHash = ComputeSignatureHash(rawBody);
        var eventType = headers.TryGetValue("X-Event-Type", out var eventTypeHeader) && !string.IsNullOrWhiteSpace(eventTypeHeader)
            ? eventTypeHeader
            : "notification.unknown";

        var category = ResolveCategory(providerName);
        var entry = new ProviderWebhookInbox(
            tenantId,
            category,
            providerName,
            signatureHash,
            eventType,
            rawBody);

        try
        {
            await _inbox.AddAsync(entry, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to persist notification webhook inbox row for provider {Provider} tenant {TenantId}.",
                providerName,
                tenantId);
            return StatusCode(500, ApiResponse<object>.Failure("Webhook inbox persistence failed.", 500));
        }

        try
        {
            await _statusUpdater.UpdateFromWebhookAsync(tenantId, providerName, rawBody, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Notification status update failed for provider {Provider} tenant {TenantId}; inbox row persisted, status change deferred to replay.",
                providerName,
                tenantId);
        }

        return Ok(ApiResponse<object>.Success(new { providerName, tenantId, signatureHash }));
    }

    private static ProviderCategory ResolveCategory(string providerName)
    {
        var key = providerName.Trim().ToLowerInvariant();
        return key switch
        {
            var p when p.Contains("sms", StringComparison.Ordinal) => ProviderCategory.Sms,
            var p when p.Contains("whatsapp", StringComparison.Ordinal) => ProviderCategory.WhatsApp,
            var p when p.Contains("push", StringComparison.Ordinal) || p.Contains("fcm", StringComparison.Ordinal) || p.Contains("apns", StringComparison.Ordinal) => ProviderCategory.Push,
            _ => ProviderCategory.Email,
        };
    }

    private static string ComputeSignatureHash(string rawBody)
    {
        var bytes = Encoding.UTF8.GetBytes(rawBody ?? string.Empty);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
