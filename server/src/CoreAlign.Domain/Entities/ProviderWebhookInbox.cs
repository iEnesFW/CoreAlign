using System.Security.Cryptography;
using System.Text;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

/// <summary>
/// Inbound webhook envelope for an external provider. Stored per tenant — the unique index is
/// (TenantId, SignatureHash) so the same provider event cannot be replay-clobbered across tenants.
/// Construction requires TenantId; the dispatch pipeline MUST resolve the tenant from the verified
/// signature/secret before persisting so we never accept anonymous cross-tenant writes.
/// </summary>
public class ProviderWebhookInbox : TenantEntity
{
    public ProviderCategory Category { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;
    public string SignatureHash { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; private set; }
    public string? ProcessingError { get; private set; }
    public int RetryCount { get; private set; }

    protected ProviderWebhookInbox() { }

    public ProviderWebhookInbox(
        Guid tenantId,
        ProviderCategory category,
        string providerName,
        string signatureHash,
        string eventType,
        string payloadJson)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required for webhook inbox.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ArgumentException("Provider name is required.", nameof(providerName));
        }

        if (string.IsNullOrWhiteSpace(signatureHash))
        {
            throw new ArgumentException("Signature hash is required.", nameof(signatureHash));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Event type is required.", nameof(eventType));
        }

        TenantId = tenantId;
        Category = category;
        ProviderName = providerName.Trim();
        SignatureHash = signatureHash.Trim();
        EventType = eventType.Trim();
        PayloadJson = payloadJson ?? string.Empty;
    }

    public void MarkProcessed(DateTime utcNow)
    {
        ProcessedAtUtc = utcNow;
        ProcessingError = null;
        UpdatedAtUtc = utcNow;
    }

    public void MarkFailed(string error, DateTime utcNow)
    {
        ProcessingError = error;
        RetryCount++;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Verifies an inbound webhook request body against the tenant-specific provider secret using HMAC-SHA256.
    /// Returns the lowercase hex digest the caller can persist as <see cref="SignatureHash"/>, or null when
    /// the supplied <paramref name="receivedSignatureHex"/> does not match (constant-time compared).
    /// </summary>
    public static string? VerifyHmacSignature(string rawPayload, string sharedSecret, string receivedSignatureHex)
    {
        if (string.IsNullOrEmpty(rawPayload) || string.IsNullOrEmpty(sharedSecret) || string.IsNullOrEmpty(receivedSignatureHex))
        {
            return null;
        }

        var key = Encoding.UTF8.GetBytes(sharedSecret);
        var payload = Encoding.UTF8.GetBytes(rawPayload);
        using var hmac = new HMACSHA256(key);
        var computed = hmac.ComputeHash(payload);
        var computedHex = Convert.ToHexString(computed).ToLowerInvariant();
        var received = receivedSignatureHex.Trim();
        if (received.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            received = received.Substring("sha256=".Length);
        }
        received = received.ToLowerInvariant();
        var receivedBytes = Encoding.UTF8.GetBytes(received);
        var computedBytes = Encoding.UTF8.GetBytes(computedHex);
        return CryptographicOperations.FixedTimeEquals(receivedBytes, computedBytes) ? computedHex : null;
    }
}
