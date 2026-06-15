using System.Collections.Specialized;
using System.Globalization;
using System.Web;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Providers.Payment.PayTR;

/// <summary>
/// Verifies inbound PayTR callbacks. PayTR posts callbacks as
/// <c>application/x-www-form-urlencoded</c> with the fields
/// <c>merchant_oid</c>, <c>status</c>, <c>total_amount</c>, and <c>hash</c>.
/// The hash is rebuilt with <see cref="PayTRHashBuilder.VerifyCallback"/> and
/// compared constant-time using the tenant-specific salt + key.
/// </summary>
public sealed class PayTRWebhookVerifier : IProviderWebhookVerifier
{
    public const string CallbackMerchantOidField = "merchant_oid";
    public const string CallbackStatusField = "status";
    public const string CallbackTotalAmountField = "total_amount";
    public const string CallbackHashField = "hash";

    private readonly ITenantProviderConfigResolver _configResolver;
    private readonly IProviderCredentialProtector _credentialProtector;
    private readonly ILogger<PayTRWebhookVerifier> _logger;

    public PayTRWebhookVerifier(
        ITenantProviderConfigResolver configResolver,
        IProviderCredentialProtector credentialProtector,
        ILogger<PayTRWebhookVerifier> logger)
    {
        _configResolver = configResolver;
        _credentialProtector = credentialProtector;
        _logger = logger;
    }

    public string ProviderName => PayTRPaymentProvider.ProviderKey;

    public async Task<bool> VerifyAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return false;
        }

        var encrypted = await _configResolver
            .GetEncryptedCredentialsAsync(tenantId, ProviderCategory.Payment, ProviderName, cancellationToken)
            .ConfigureAwait(false);

        var credentials = _credentialProtector.UnprotectAs<PayTRCredentials>(tenantId, ProviderCategory.Payment, encrypted);
        if (credentials is null
            || string.IsNullOrWhiteSpace(credentials.MerchantKey)
            || string.IsNullOrWhiteSpace(credentials.MerchantSalt))
        {
            _logger.LogWarning("PayTR webhook secrets missing for tenant {TenantId}.", tenantId);
            return false;
        }

        if (!TryParseCallback(rawBody, out var merchantOid, out var status, out var totalAmount, out var receivedHash))
        {
            _logger.LogWarning("PayTR callback missing required fields for tenant {TenantId}.", tenantId);
            return false;
        }

        return PayTRHashBuilder.VerifyCallback(
            merchantOid,
            status,
            totalAmount,
            receivedHash,
            credentials.MerchantKey,
            credentials.MerchantSalt);
    }

    public static bool TryParseCallback(
        string rawBody,
        out string merchantOid,
        out string status,
        out decimal totalAmount,
        out string hash)
    {
        merchantOid = string.Empty;
        status = string.Empty;
        totalAmount = 0m;
        hash = string.Empty;

        NameValueCollection parsed;
        try
        {
            parsed = HttpUtility.ParseQueryString(rawBody);
        }
        catch (Exception)
        {
            return false;
        }

        merchantOid = parsed[CallbackMerchantOidField] ?? string.Empty;
        status = parsed[CallbackStatusField] ?? string.Empty;
        hash = parsed[CallbackHashField] ?? string.Empty;

        var rawAmount = parsed[CallbackTotalAmountField];
        if (!decimal.TryParse(rawAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amountInCents))
        {
            return false;
        }
        totalAmount = amountInCents / 100m;

        return !string.IsNullOrWhiteSpace(merchantOid)
            && !string.IsNullOrWhiteSpace(status)
            && !string.IsNullOrWhiteSpace(hash);
    }
}
