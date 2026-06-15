using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CoreAlign.Application.Billing.Payments;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.Payment;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using PaymentProviderIntentRequest = CoreAlign.Application.Providers.Payment.PaymentIntentRequest;
using BillingPaymentIntentRequest = CoreAlign.Application.Billing.Payments.PaymentIntentRequest;

namespace CoreAlign.Infrastructure.Providers.Payment.Iyzico;

public sealed class IyzicoPaymentProvider : IPaymentProvider
{
    public const string ProviderKey = "iyzico";
    public const string HttpClientName = "IyzicoPayment";

    public const string ChargePath = "/payment/auth";
    public const string ThreeDSInitPath = "/payment/3dsecure/initialize";
    public const string ThreeDSVerifyPath = "/payment/3dsecure/auth";
    public const string RefundPath = "/payment/refund";
    public const string TransactionLookupPath = "/payment/detail";
    public const string CardStoragePath = "/cardstorage/card";

    private const string SandboxBaseUrl = "https://sandbox-api.iyzipay.com";
    private const string ProductionBaseUrl = "https://api.iyzipay.com";

    private const string DefaultLocale = "tr";
    private const string PaymentChannelWeb = "WEB";
    private const string PaymentGroupProduct = "PRODUCT";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITenantProviderConfigResolver _configResolver;
    private readonly IProviderCredentialProtector _credentialProtector;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<IyzicoPaymentProvider> _logger;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public IyzicoPaymentProvider(
        IHttpClientFactory httpClientFactory,
        ITenantProviderConfigResolver configResolver,
        IProviderCredentialProtector credentialProtector,
        ITenantContext tenantContext,
        ILogger<IyzicoPaymentProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configResolver = configResolver;
        _credentialProtector = credentialProtector;
        _tenantContext = tenantContext;
        _logger = logger;
        _retryPolicy = BuildRetryPolicy(logger);
    }

    public string Name => ProviderKey;

    public string DisplayName => "Iyzico";

    public ProviderCapabilities Capabilities => new(
        ProviderCapability.WebhookCallback
            | ProviderCapability.SignatureValidation
            | ProviderCapability.Refund
            | ProviderCapability.RealTimeStatus,
        new Dictionary<string, string>
        {
            ["transport"] = "rest",
            ["auth"] = "iyzws-pki",
            ["region"] = "tr",
        });

    public async Task<ProviderHealthCheckResult> CheckHealthAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        const string endpoint = "/payment/test";
        var started = DateTime.UtcNow;
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, SandboxBaseUrl + endpoint)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            };
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var elapsed = DateTime.UtcNow - started;
            return (int)response.StatusCode < 500
                ? ProviderHealthCheckResult.Healthy(Name, elapsed, endpoint, (int)response.StatusCode)
                : ProviderHealthCheckResult.Unhealthy(Name, $"HTTP {(int)response.StatusCode}", elapsed, endpoint, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Iyzico health check failed for tenant {TenantId}.", tenantId);
            return ProviderHealthCheckResult.Unhealthy(Name, ex.Message, DateTime.UtcNow - started, endpoint);
        }
    }

    public Task<IReadOnlyList<PaymentMethodDescriptor>> ListMethodsAsync(Guid tenantId, CancellationToken ct)
    {
        IReadOnlyList<PaymentMethodDescriptor> list = new[]
        {
            new PaymentMethodDescriptor(
                PaymentMethodKind.ThreeDS,
                "Iyzico 3D Secure Card",
                MinAmount: 1m,
                MaxAmount: 1_000_000m,
                SupportedCurrencies: new[] { "TRY", "USD", "EUR", "GBP" }),
            new PaymentMethodDescriptor(
                PaymentMethodKind.CardOnFile,
                "Iyzico Stored Card",
                MinAmount: 1m,
                MaxAmount: 1_000_000m,
                SupportedCurrencies: new[] { "TRY", "USD", "EUR", "GBP" }),
            new PaymentMethodDescriptor(
                PaymentMethodKind.Installment,
                "Iyzico Installment",
                MinAmount: 1m,
                MaxAmount: 1_000_000m,
                SupportedCurrencies: new[] { "TRY" }),
        };
        return Task.FromResult(list);
    }

    public Task<PaymentLinkResult> CreateLinkAsync(PaymentProviderIntentRequest req, PaymentLinkOptions opts, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(opts);

        throw new IyzicoProviderException(
            "LINK_FLOW_UNSUPPORTED",
            "Iyzico provider charges occur via the 3DS or Charge flow; use Initiate3DSecureAsync or ChargeAsync.");
    }

    public Task<PaymentIntentResult> CreateIntentAsync(BillingPaymentIntentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        throw new IyzicoProviderException(
            "INTENT_FLOW_VIA_3DS",
            "Iyzico provider intents are created through Initiate3DSecureAsync with an iyzico.js card token.");
    }

    public Task<WebhookProcessingResult> HandleWebhookAsync(string payload, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(headers);

        string? paymentId = null;
        string? status = null;
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("paymentId", out var pid)) paymentId = pid.GetString();
            if (doc.RootElement.TryGetProperty("status", out var st)) status = st.GetString();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Iyzico webhook body is not valid JSON.");
            throw new ArgumentException("Iyzico webhook body is not valid JSON.", nameof(payload));
        }

        var mapped = MapPaymentStatus(status);
        var result = new WebhookProcessingResult(
            IntentId: paymentId ?? string.Empty,
            Status: mapped,
            Reference: paymentId,
            FailureReason: mapped == PaymentIntentStatus.Failed ? status : null,
            RawJson: payload);
        return Task.FromResult(result);
    }

    public Task<CaptureResult> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(new CaptureResult(true, request.IntentId, null, null));
    }

    public async Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var paymentTxnId = string.IsNullOrWhiteSpace(request.PaymentTransactionId)
            ? request.IntentId
            : request.PaymentTransactionId;
        if (string.IsNullOrWhiteSpace(paymentTxnId))
        {
            return new RefundResult(false, null, null, "PaymentTransactionId is required for Iyzico refunds.");
        }
        if (request.Amount is null || request.Amount.Value <= 0m)
        {
            return new RefundResult(false, null, null, "Refund amount must be greater than zero.");
        }

        var refundRequest = new IyzicoRefundRequest(
            Locale: DefaultLocale,
            ConversationId: request.IntentId,
            PaymentTransactionId: paymentTxnId,
            Price: FormatAmount(request.Amount.Value),
            Ip: "127.0.0.1",
            Currency: NormalizeCurrency(request.Currency));

        var result = await RefundAsync(refundRequest, cancellationToken).ConfigureAwait(false);
        if (!IsSuccess(result.Status))
        {
            return new RefundResult(false, null, null, result.ErrorMessage ?? "Iyzico refund failed.");
        }
        var raw = JsonSerializer.Serialize(result, JsonOptions);
        return new RefundResult(true, result.PaymentId ?? result.PaymentTransactionId, raw, null);
    }

    public async Task<IyzicoChargeResult> ChargeAsync(IyzicoChargeRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<IyzicoChargeRequest, IyzicoChargeResult>(ChargePath, request, ct).ConfigureAwait(false);
    }

    public async Task<Iyzico3DSecureInitResult> Initiate3DSecureAsync(Iyzico3DSecureInitRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<Iyzico3DSecureInitRequest, Iyzico3DSecureInitResult>(ThreeDSInitPath, request, ct).ConfigureAwait(false);
    }

    public async Task<IyzicoChargeResult> Verify3DSecureAsync(Iyzico3DSecureVerifyRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<Iyzico3DSecureVerifyRequest, IyzicoChargeResult>(ThreeDSVerifyPath, request, ct).ConfigureAwait(false);
    }

    public async Task<IyzicoRefundResult> RefundAsync(IyzicoRefundRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<IyzicoRefundRequest, IyzicoRefundResult>(RefundPath, request, ct).ConfigureAwait(false);
    }

    public async Task<IyzicoTransactionLookupResult> GetTransactionAsync(IyzicoTransactionLookupRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<IyzicoTransactionLookupRequest, IyzicoTransactionLookupResult>(TransactionLookupPath, request, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Stores a card in the Iyzico vault using the ephemeral
    /// <see cref="IyzicoVaultCardReference.CardToken"/> the cardholder's
    /// browser obtained from iyzico.js. The backend never sees raw PAN /
    /// CVC; only the opaque token traverses this method.
    /// </summary>
    public async Task<IyzicoTokenizeResult> TokenizeCardAsync(IyzicoTokenizeRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Card?.CardToken))
        {
            throw new IyzicoProviderException("CARD_TOKEN_MISSING", "An ephemeral card token from iyzico.js is required; raw PAN is not accepted.");
        }
        return await PostAsync<IyzicoTokenizeRequest, IyzicoTokenizeResult>(CardStoragePath, request, ct).ConfigureAwait(false);
    }

    public static string FormatAmount(decimal amount)
    {
        var rounded = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        return rounded.ToString("F2", CultureInfo.InvariantCulture);
    }

    public static string NormalizeCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency)) return "TRY";
        return currency.Trim().ToUpperInvariant() switch
        {
            "TRY" => "TRY",
            "USD" => "USD",
            "EUR" => "EUR",
            "GBP" => "GBP",
            _ => throw new IyzicoProviderException("CURRENCY_UNSUPPORTED", $"Currency '{currency}' is not supported by Iyzico."),
        };
    }

    public static PaymentIntentStatus MapPaymentStatus(string? iyzicoStatus)
    {
        if (string.IsNullOrWhiteSpace(iyzicoStatus)) return PaymentIntentStatus.Pending;
        return iyzicoStatus.Trim().ToUpperInvariant() switch
        {
            "SUCCESS" => PaymentIntentStatus.Succeeded,
            "FAILURE" => PaymentIntentStatus.Failed,
            "INIT_THREEDS" => PaymentIntentStatus.RequiresAction,
            "CALLBACK_THREEDS" => PaymentIntentStatus.RequiresAction,
            "BKM_POS_SELECTED" => PaymentIntentStatus.Pending,
            "CALLBACK_PECCO" => PaymentIntentStatus.Pending,
            _ => PaymentIntentStatus.Pending,
        };
    }

    private async Task<TResult> PostAsync<TRequest, TResult>(string path, TRequest request, CancellationToken ct)
        where TRequest : class
        where TResult : class
    {
        var ctx = await ResolveContextAsync(ct).ConfigureAwait(false);
        var body = JsonSerializer.Serialize(request, JsonOptions);

        var response = await _retryPolicy.ExecuteAsync(async pollyCt =>
        {
            using var httpRequest = BuildSignedRequest(ctx, path, body);
            return await ctx.HttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, pollyCt).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

        try
        {
            var raw = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var err = TryParseError(raw);
                throw new IyzicoProviderException(
                    err?.ErrorCode ?? $"HTTP_{(int)response.StatusCode}",
                    err?.ErrorMessage ?? $"Iyzico request failed with status {(int)response.StatusCode}.");
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new IyzicoProviderException("RESPONSE_EMPTY", "Iyzico response body was empty.");
            }

            var parsed = JsonSerializer.Deserialize<TResult>(raw, JsonOptions)
                ?? throw new IyzicoProviderException("RESPONSE_PARSE_FAILED", "Iyzico response could not be parsed.");
            return parsed;
        }
        finally
        {
            response.Dispose();
        }
    }

    private async Task<IyzicoInvocationContext> ResolveContextAsync(CancellationToken ct)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var encrypted = await _configResolver
            .GetEncryptedCredentialsAsync(tenantId, ProviderCategory.Payment, ProviderKey, ct)
            .ConfigureAwait(false);

        var credentials = _credentialProtector.UnprotectAs<IyzicoCredentials>(tenantId, ProviderCategory.Payment, encrypted)
            ?? throw new IyzicoProviderException("CREDENTIALS_MISSING", "Iyzico credentials are not configured for the current tenant.");

        var baseUrl = credentials.IsSandbox ? SandboxBaseUrl : ProductionBaseUrl;
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        if (httpClient.BaseAddress is null)
        {
            httpClient.BaseAddress = new Uri(baseUrl);
        }

        return new IyzicoInvocationContext(tenantId, credentials, baseUrl, httpClient);
    }

    private static HttpRequestMessage BuildSignedRequest(IyzicoInvocationContext ctx, string path, string body)
    {
        var randomString = IyzicoSignatureBuilder.GenerateRandomString();
        var authorization = IyzicoSignatureBuilder.BuildAuthorizationHeader(
            ctx.Credentials.ApiKey,
            ctx.Credentials.SecretKey,
            randomString,
            body);

        var request = new HttpRequestMessage(HttpMethod.Post, ctx.BaseUrl + path)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.TryAddWithoutValidation(IyzicoSignatureBuilder.AuthorizationHeader, authorization);
        request.Headers.TryAddWithoutValidation(IyzicoSignatureBuilder.RandomHeader, randomString);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static IyzicoErrorResponse? TryParseError(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            return JsonSerializer.Deserialize<IyzicoErrorResponse>(raw, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsSuccess(string? status) =>
        string.Equals(status, "success", StringComparison.OrdinalIgnoreCase);

    private static AsyncRetryPolicy<HttpResponseMessage> BuildRetryPolicy(ILogger<IyzicoPaymentProvider> logger) =>
        Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>(static ex => !ex.CancellationToken.IsCancellationRequested)
            .OrResult(static r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: static attempt => TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt - 1)),
                onRetry: (outcome, delay, attempt, _) =>
                {
                    logger.LogWarning(
                        outcome.Exception,
                        "Iyzico HTTP attempt {Attempt} failed (status {Status}); retrying in {Delay}.",
                        attempt,
                        outcome.Result?.StatusCode,
                        delay);
                });

    private sealed record IyzicoInvocationContext(
        Guid TenantId,
        IyzicoCredentials Credentials,
        string BaseUrl,
        HttpClient HttpClient);
}
