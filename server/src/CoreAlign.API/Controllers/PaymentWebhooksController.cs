using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Asp.Versioning;
using CoreAlign.Application.Common;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Application.Providers.Payment.Events;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

/// <summary>
/// F2.2 payment provider webhook inbound endpoint. Mirrors
/// <see cref="EFaturaWebhooksController"/> wiring exactly — verifier
/// composer, tenant-id header, ProviderWebhookInbox persistence, outbox event.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/webhooks/payment")]
public class PaymentWebhooksController : ControllerBase
{
    private const string TenantHeaderName = "X-CoreAlign-Tenant-Id";
    private const int MaxBodySize = 256 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IWebhookSignatureVerifier _verifier;
    private readonly IProviderWebhookInboxRepository _inbox;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IOutboxSignal _outboxSignal;
    private readonly ILogger<PaymentWebhooksController> _logger;

    public PaymentWebhooksController(
        IWebhookSignatureVerifier verifier,
        IProviderWebhookInboxRepository inbox,
        IOutboxRepository outboxRepository,
        IOutboxSignal outboxSignal,
        ILogger<PaymentWebhooksController> logger)
    {
        _verifier = verifier;
        _inbox = inbox;
        _outboxRepository = outboxRepository;
        _outboxSignal = outboxSignal;
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
                "Payment webhook verification threw for provider {Provider} tenant {TenantId}.",
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
            : "payment.unknown";

        var entry = new ProviderWebhookInbox(
            tenantId,
            ProviderCategory.Payment,
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
                "Failed to persist payment webhook inbox row for provider {Provider} tenant {TenantId}.",
                providerName,
                tenantId);
            return StatusCode(500, ApiResponse<object>.Failure("Webhook inbox persistence failed.", 500));
        }

        try
        {
            var evt = new PaymentWebhookReceivedEvent(tenantId, providerName, signatureHash, eventType, DateTime.UtcNow);
            var payload = JsonSerializer.Serialize(evt, JsonOptions);
            await _outboxRepository.AddAsync(new OutboxMessage("PaymentWebhookReceived", payload), cancellationToken);
            _outboxSignal.MarkPending();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to enqueue PaymentWebhookReceived event for provider {Provider} tenant {TenantId}; continuing.",
                providerName,
                tenantId);
        }

        return Ok(ApiResponse<object>.Success(new { providerName, tenantId, signatureHash }));
    }

    private static string ComputeSignatureHash(string rawBody)
    {
        var bytes = Encoding.UTF8.GetBytes(rawBody ?? string.Empty);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
