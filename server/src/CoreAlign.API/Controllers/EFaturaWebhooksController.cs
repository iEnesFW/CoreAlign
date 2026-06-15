using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
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
[Route("api/v{version:apiVersion}/webhooks/efatura")]
public class EFaturaWebhooksController : ControllerBase
{
    private const string TenantHeaderName = "X-CoreAlign-Tenant-Id";
    private const int MaxBodySize = 256 * 1024;

    private readonly IWebhookSignatureVerifier _verifier;
    private readonly IProviderWebhookInboxRepository _inbox;
    private readonly ILogger<EFaturaWebhooksController> _logger;

    public EFaturaWebhooksController(
        IWebhookSignatureVerifier verifier,
        IProviderWebhookInboxRepository inbox,
        ILogger<EFaturaWebhooksController> logger)
    {
        _verifier = verifier;
        _inbox = inbox;
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
                "EFatura webhook verification threw for provider {Provider} tenant {TenantId}.",
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
            : "efatura.unknown";

        var entry = new ProviderWebhookInbox(
            tenantId,
            ProviderCategory.EFatura,
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
                "Failed to persist EFatura webhook inbox row for provider {Provider} tenant {TenantId}.",
                providerName,
                tenantId);
            return StatusCode(500, ApiResponse<object>.Failure("Webhook inbox persistence failed.", 500));
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
