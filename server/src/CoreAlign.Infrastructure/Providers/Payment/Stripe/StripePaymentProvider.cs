using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreAlign.Application.Billing.Payments;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.Payment;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using PaymentIntentRequest = CoreAlign.Application.Providers.Payment.PaymentIntentRequest;
using BillingPaymentIntentRequest = CoreAlign.Application.Billing.Payments.PaymentIntentRequest;

namespace CoreAlign.Infrastructure.Providers.Payment.Stripe;

/// <summary>
/// Stripe REST integration without the official SDK. Speaks raw HTTP to
/// <c>api.stripe.com</c> using a Bearer secret key, form-encoded bodies, and
/// an idempotency key for safe retry of mutating calls.
/// </summary>
public sealed class StripePaymentProvider : IPaymentProvider
{
    public const string ProviderKey = "stripe";
    public const string HttpClientName = "StripePayment";
    public const string ApiBaseUrl = "https://api.stripe.com";
    public const string StripeApiVersion = "2024-11-20";

    private const string TestKeyPrefix = "sk_test_";
    private const string LiveKeyPrefix = "sk_live_";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITenantProviderConfigResolver _configResolver;
    private readonly IProviderCredentialProtector _credentialProtector;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<StripePaymentProvider> _logger;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public StripePaymentProvider(
        IHttpClientFactory httpClientFactory,
        ITenantProviderConfigResolver configResolver,
        IProviderCredentialProtector credentialProtector,
        ITenantContext tenantContext,
        ILogger<StripePaymentProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configResolver = configResolver;
        _credentialProtector = credentialProtector;
        _tenantContext = tenantContext;
        _logger = logger;
        _retryPolicy = BuildRetryPolicy();
    }

    public string Name => ProviderKey;

    public string DisplayName => "Stripe";

    public ProviderCapabilities Capabilities => new(
        ProviderCapability.WebhookCallback
            | ProviderCapability.Webhook
            | ProviderCapability.Refund
            | ProviderCapability.SignatureValidation
            | ProviderCapability.RealTimeStatus,
        new Dictionary<string, string>
        {
            ["transport"] = "rest",
            ["api_version"] = StripeApiVersion,
            ["region"] = "global",
        });

    public async Task<ProviderHealthCheckResult> CheckHealthAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        const string endpoint = "/v1/balance";
        var started = DateTime.UtcNow;
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, ApiBaseUrl + endpoint);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var elapsed = DateTime.UtcNow - started;
            return (int)response.StatusCode is >= 200 and < 500
                ? ProviderHealthCheckResult.Healthy(Name, elapsed, endpoint, (int)response.StatusCode)
                : ProviderHealthCheckResult.Unhealthy(Name, $"HTTP {(int)response.StatusCode}", elapsed, endpoint, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stripe health check failed for tenant {TenantId}.", tenantId);
            return ProviderHealthCheckResult.Unhealthy(Name, ex.Message, DateTime.UtcNow - started, endpoint);
        }
    }

    public Task<IReadOnlyList<PaymentMethodDescriptor>> ListMethodsAsync(Guid tenantId, CancellationToken ct)
    {
        IReadOnlyList<PaymentMethodDescriptor> methods = new[]
        {
            new PaymentMethodDescriptor(
                PaymentMethodKind.ThreeDS,
                "Card (3D Secure)",
                MinAmount: 0.50m,
                MaxAmount: 999_999_999m,
                SupportedCurrencies: new[] { "USD", "EUR", "GBP", "TRY", "CHF", "AUD", "CAD", "JPY" }),
            new PaymentMethodDescriptor(
                PaymentMethodKind.CardOnFile,
                "Saved Card",
                MinAmount: 0.50m,
                MaxAmount: 999_999_999m,
                SupportedCurrencies: new[] { "USD", "EUR", "GBP", "TRY" }),
        };
        return Task.FromResult(methods);
    }

    public async Task<PaymentLinkResult> CreateLinkAsync(PaymentIntentRequest req, PaymentLinkOptions opts, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(opts);

        var ctx = await ResolveContextAsync(ct).ConfigureAwait(false);
        var minor = ToMinorUnits(req.Amount, req.Currency);
        var idempotencyKey = $"link:{req.OrderReference}:{minor}:{req.Currency}".ToLowerInvariant();

        var fields = new List<KeyValuePair<string, string>>
        {
            new("amount", minor.ToString(CultureInfo.InvariantCulture)),
            new("currency", NormalizeCurrency(req.Currency)),
            new("automatic_payment_methods[enabled]", "true"),
            new("capture_method", "automatic"),
            new("metadata[orderReference]", req.OrderReference ?? string.Empty),
            new("metadata[buyerEmail]", req.BuyerEmail ?? string.Empty),
            new("metadata[buyerName]", req.BuyerName ?? string.Empty),
        };
        if (!string.IsNullOrWhiteSpace(opts.CallbackUrl))
        {
            fields.Add(new("return_url", opts.CallbackUrl!));
        }

        var intent = await SendFormAsync<StripeChargeResult>(
            ctx,
            HttpMethod.Post,
            "/v1/payment_intents",
            fields,
            idempotencyKey,
            ct).ConfigureAwait(false);

        var expiry = DateTime.UtcNow.AddMinutes(opts.ExpiryMinutes <= 0 ? 30 : opts.ExpiryMinutes);
        var redirect = intent.NextAction?.RedirectToUrl?.Url
            ?? (intent.ClientSecret is null
                ? string.Empty
                : $"client_secret:{intent.ClientSecret}");
        return new PaymentLinkResult(redirect, expiry, intent.Id);
    }

    public async Task<PaymentIntentResult> CreateIntentAsync(BillingPaymentIntentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ctx = await ResolveContextAsync(cancellationToken).ConfigureAwait(false);

        var minor = ToMinorUnits(request.Amount, request.Currency);
        var idempotencyKey = !string.IsNullOrWhiteSpace(request.OrderNumber)
            ? $"intent:{request.TenantId:N}:{request.OrderId:N}:{request.OrderNumber}"
            : $"intent:{request.TenantId:N}:{request.OrderId:N}:{Guid.NewGuid():N}";

        var fields = new List<KeyValuePair<string, string>>
        {
            new("amount", minor.ToString(CultureInfo.InvariantCulture)),
            new("currency", NormalizeCurrency(request.Currency)),
            new("automatic_payment_methods[enabled]", "true"),
            new("capture_method", "automatic"),
            new("metadata[orderId]", request.OrderId.ToString("D")),
            new("metadata[orderNumber]", request.OrderNumber ?? string.Empty),
            new("metadata[tenantId]", request.TenantId.ToString("D")),
        };

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            fields.Add(new("description", request.Description!));
        }
        if (request.BillingInfo is not null)
        {
            fields.Add(new("receipt_email", request.BillingInfo.Email));
            fields.Add(new("shipping[name]", $"{request.BillingInfo.Name} {request.BillingInfo.Surname}".Trim()));
            fields.Add(new("shipping[address][line1]", request.BillingInfo.Address));
            fields.Add(new("shipping[address][city]", request.BillingInfo.City));
            fields.Add(new("shipping[address][country]", request.BillingInfo.Country));
            fields.Add(new("shipping[address][postal_code]", request.BillingInfo.ZipCode));
        }
        if (request.Metadata is not null)
        {
            foreach (var kv in request.Metadata)
            {
                if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                fields.Add(new($"metadata[{kv.Key}]", kv.Value ?? string.Empty));
            }
        }

        var intent = await SendFormAsync<StripeChargeResult>(
            ctx,
            HttpMethod.Post,
            "/v1/payment_intents",
            fields,
            idempotencyKey,
            cancellationToken).ConfigureAwait(false);

        var status = MapStatus(intent.Status);
        string? redirectUrl = intent.NextAction?.RedirectToUrl?.Url;
        var metadata = new Dictionary<string, string>
        {
            ["client_secret"] = intent.ClientSecret ?? string.Empty,
            ["intent_status"] = intent.Status,
        };
        if (intent.NextAction?.Type is { Length: > 0 } actionType)
        {
            metadata["next_action_type"] = actionType;
        }
        return new PaymentIntentResult(intent.Id, redirectUrl, status, metadata, SerializeRaw(intent));
    }

    public async Task<CaptureResult> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.IntentId))
        {
            throw new ArgumentException("IntentId is required.", nameof(request));
        }

        var ctx = await ResolveContextAsync(cancellationToken).ConfigureAwait(false);
        var fields = new List<KeyValuePair<string, string>>();
        if (request.Amount is decimal amount)
        {
            var existing = await SendFormAsync<StripeChargeResult>(
                ctx,
                HttpMethod.Get,
                $"/v1/payment_intents/{Uri.EscapeDataString(request.IntentId)}",
                body: null,
                idempotencyKey: null,
                cancellationToken).ConfigureAwait(false);
            var minor = ToMinorUnits(amount, existing.Currency);
            fields.Add(new("amount_to_capture", minor.ToString(CultureInfo.InvariantCulture)));
        }

        try
        {
            var captured = await SendFormAsync<StripeChargeResult>(
                ctx,
                HttpMethod.Post,
                $"/v1/payment_intents/{Uri.EscapeDataString(request.IntentId)}/capture",
                fields,
                idempotencyKey: $"capture:{request.IntentId}",
                cancellationToken).ConfigureAwait(false);

            var success = string.Equals(captured.Status, "succeeded", StringComparison.OrdinalIgnoreCase);
            return new CaptureResult(success, captured.LatestCharge ?? captured.Id, SerializeRaw(captured), success ? null : captured.Status);
        }
        catch (StripeProviderException ex)
        {
            return new CaptureResult(false, null, null, ex.Message);
        }
    }

    public async Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.IntentId))
        {
            throw new ArgumentException("IntentId is required.", nameof(request));
        }

        var ctx = await ResolveContextAsync(cancellationToken).ConfigureAwait(false);
        var fields = new List<KeyValuePair<string, string>>
        {
            new("payment_intent", request.IntentId),
        };
        if (request.Amount is decimal amount)
        {
            var currency = NormalizeCurrency(request.Currency ?? "usd");
            var minor = ToMinorUnits(amount, currency);
            fields.Add(new("amount", minor.ToString(CultureInfo.InvariantCulture)));
        }
        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            fields.Add(new("reason", NormalizeRefundReason(request.Reason!)));
        }
        if (!string.IsNullOrWhiteSpace(request.PaymentTransactionId))
        {
            fields.Add(new("metadata[paymentTransactionId]", request.PaymentTransactionId!));
        }

        try
        {
            var refundIdempotencyKey = $"refund:{request.IntentId}:{request.PaymentTransactionId ?? Guid.NewGuid().ToString("N")}";
            var refund = await SendFormAsync<StripeRefundResult>(
                ctx,
                HttpMethod.Post,
                "/v1/refunds",
                fields,
                refundIdempotencyKey,
                cancellationToken).ConfigureAwait(false);

            var success = string.Equals(refund.Status, "succeeded", StringComparison.OrdinalIgnoreCase)
                || string.Equals(refund.Status, "pending", StringComparison.OrdinalIgnoreCase);
            return new RefundResult(success, refund.Id, SerializeRaw(refund), success ? null : refund.Status);
        }
        catch (StripeProviderException ex)
        {
            return new RefundResult(false, null, null, ex.Message);
        }
    }

    public Task<WebhookProcessingResult> HandleWebhookAsync(string payload, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new PaymentWebhookSignatureException("Stripe webhook payload was empty.");
        }
        ArgumentNullException.ThrowIfNull(headers);

        StripeWebhookEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<StripeWebhookEnvelope>(payload, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new PaymentGatewayException("Stripe webhook payload was not valid JSON.", ex.GetType().Name);
        }

        if (envelope is null || envelope.Data?.Object is null)
        {
            throw new PaymentGatewayException("Stripe webhook payload missing data.object.");
        }

        var intent = envelope.Data.Object;
        var status = MapStatus(intent.Status ?? envelope.Type ?? string.Empty);
        if (string.IsNullOrWhiteSpace(intent.Id))
        {
            throw new PaymentGatewayException("Stripe webhook payment_intent missing id.");
        }

        var reference = intent.LatestCharge ?? intent.PaymentMethod;
        var failureReason = status == PaymentIntentStatus.Failed
            ? intent.LastPaymentError?.Message ?? envelope.Type
            : null;

        return Task.FromResult(new WebhookProcessingResult(intent.Id, status, reference, failureReason, payload));
    }

    public async Task<StripeChargeResult> Initiate3DSecureAsync(BillingPaymentIntentRequest request, string paymentMethodId, string returnUrl, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentMethodId);
        ArgumentException.ThrowIfNullOrWhiteSpace(returnUrl);

        var ctx = await ResolveContextAsync(cancellationToken).ConfigureAwait(false);
        var minor = ToMinorUnits(request.Amount, request.Currency);
        var idempotencyKey = $"3ds:{request.TenantId:N}:{request.OrderId:N}:{minor}";

        var fields = new List<KeyValuePair<string, string>>
        {
            new("amount", minor.ToString(CultureInfo.InvariantCulture)),
            new("currency", NormalizeCurrency(request.Currency)),
            new("payment_method", paymentMethodId),
            new("payment_method_types[]", "card"),
            new("confirm", "true"),
            new("return_url", returnUrl),
            new("metadata[orderId]", request.OrderId.ToString("D")),
            new("metadata[tenantId]", request.TenantId.ToString("D")),
        };

        return await SendFormAsync<StripeChargeResult>(
            ctx,
            HttpMethod.Post,
            "/v1/payment_intents",
            fields,
            idempotencyKey,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<StripeChargeResult> Verify3DSecureAsync(string paymentIntentId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentIntentId);
        var ctx = await ResolveContextAsync(cancellationToken).ConfigureAwait(false);

        return await SendFormAsync<StripeChargeResult>(
            ctx,
            HttpMethod.Post,
            $"/v1/payment_intents/{Uri.EscapeDataString(paymentIntentId)}/confirm",
            body: Array.Empty<KeyValuePair<string, string>>(),
            idempotencyKey: $"confirm:{paymentIntentId}",
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<StripeChargeResult> GetTransactionAsync(string paymentIntentId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentIntentId);
        var ctx = await ResolveContextAsync(cancellationToken).ConfigureAwait(false);

        return await SendFormAsync<StripeChargeResult>(
            ctx,
            HttpMethod.Get,
            $"/v1/payment_intents/{Uri.EscapeDataString(paymentIntentId)}",
            body: null,
            idempotencyKey: null,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Attaches a Stripe.js-issued payment method to a customer vault. The
    /// frontend captures card data via Stripe Elements / Stripe.js and
    /// forwards the resulting <paramref name="paymentMethodId"/>; raw PAN /
    /// CVC never traverse this backend.
    /// </summary>
    public async Task<StripePaymentMethodResult> AttachPaymentMethodAsync(
        string paymentMethodId,
        string? customerId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentMethodId);

        var ctx = await ResolveContextAsync(cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            var attachFields = new List<KeyValuePair<string, string>>
            {
                new("customer", customerId!),
            };
            return await SendFormAsync<StripePaymentMethodResult>(
                ctx,
                HttpMethod.Post,
                $"/v1/payment_methods/{Uri.EscapeDataString(paymentMethodId)}/attach",
                attachFields,
                idempotencyKey: $"attach:{paymentMethodId}:{customerId}",
                cancellationToken).ConfigureAwait(false);
        }

        return await SendFormAsync<StripePaymentMethodResult>(
            ctx,
            HttpMethod.Get,
            $"/v1/payment_methods/{Uri.EscapeDataString(paymentMethodId)}",
            body: null,
            idempotencyKey: null,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<StripeInvocationContext> ResolveContextAsync(CancellationToken ct)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var encrypted = await _configResolver
            .GetEncryptedCredentialsAsync(tenantId, ProviderCategory.Payment, ProviderKey, ct)
            .ConfigureAwait(false);

        var credentials = _credentialProtector.UnprotectAs<StripeCredentials>(tenantId, ProviderCategory.Payment, encrypted)
            ?? throw new StripeProviderException("Stripe credentials are not configured for the current tenant.", "CREDENTIALS_MISSING", null, 0);

        if (string.IsNullOrWhiteSpace(credentials.SecretKey))
        {
            throw new StripeProviderException("Stripe secret key is missing.", "SECRET_KEY_MISSING", null, 0);
        }

        var isTestKey = credentials.SecretKey.StartsWith(TestKeyPrefix, StringComparison.Ordinal);
        var isLiveKey = credentials.SecretKey.StartsWith(LiveKeyPrefix, StringComparison.Ordinal);
        if (!isTestKey && !isLiveKey)
        {
            throw new StripeProviderException("Stripe secret key must start with sk_test_ or sk_live_.", "SECRET_KEY_INVALID", null, 0);
        }
        if (credentials.IsSandbox && !isTestKey)
        {
            _logger.LogWarning("Stripe tenant {TenantId} marked sandbox but secret key is live.", tenantId);
        }
        if (!credentials.IsSandbox && !isLiveKey)
        {
            _logger.LogWarning("Stripe tenant {TenantId} marked production but secret key is test.", tenantId);
        }

        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        if (httpClient.BaseAddress is null)
        {
            httpClient.BaseAddress = new Uri(ApiBaseUrl);
        }

        return new StripeInvocationContext(tenantId, credentials, httpClient);
    }

    private async Task<T> SendFormAsync<T>(
        StripeInvocationContext ctx,
        HttpMethod method,
        string relativePath,
        IEnumerable<KeyValuePair<string, string>>? body,
        string? idempotencyKey,
        CancellationToken ct) where T : class
    {
        var bodySnapshot = body?.ToList();

        var response = await _retryPolicy.ExecuteAsync(async pollyCt =>
        {
            using var req = BuildRequest(ctx, method, relativePath, bodySnapshot, idempotencyKey);
            return await ctx.HttpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, pollyCt).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

        try
        {
            var raw = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw StripeProviderException.FromBody((int)response.StatusCode, raw);
            }

            var parsed = JsonSerializer.Deserialize<T>(raw, JsonOptions);
            return parsed ?? throw new StripeProviderException("Stripe response body was empty.", "EMPTY_BODY", null, (int)response.StatusCode);
        }
        finally
        {
            response.Dispose();
        }
    }

    private static HttpRequestMessage BuildRequest(
        StripeInvocationContext ctx,
        HttpMethod method,
        string relativePath,
        IReadOnlyCollection<KeyValuePair<string, string>>? body,
        string? idempotencyKey)
    {
        var request = new HttpRequestMessage(method, relativePath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ctx.Credentials.SecretKey);
        request.Headers.TryAddWithoutValidation("Stripe-Version", StripeApiVersion);
        if (!string.IsNullOrWhiteSpace(ctx.Credentials.AccountId))
        {
            request.Headers.TryAddWithoutValidation("Stripe-Account", ctx.Credentials.AccountId!);
        }
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey!);
        }
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (body is not null && body.Count > 0)
        {
            request.Content = new FormUrlEncodedContent(body);
        }
        else if (method == HttpMethod.Post)
        {
            request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/x-www-form-urlencoded");
        }
        return request;
    }

    private AsyncRetryPolicy<HttpResponseMessage> BuildRetryPolicy() =>
        Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>(ex => !ex.CancellationToken.IsCancellationRequested)
            .OrResult(static r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout || r.StatusCode == (HttpStatusCode)429)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: static attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)),
                onRetry: (outcome, delay, attempt, _) =>
                {
                    _logger.LogWarning(
                        outcome.Exception,
                        "Stripe HTTP attempt {Attempt} failed (status {Status}); retrying in {Delay}.",
                        attempt,
                        outcome.Result?.StatusCode,
                        delay);
                });

    private static long ToMinorUnits(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be non-negative.");
        }
        var zeroDecimal = IsZeroDecimalCurrency(currency);
        var factor = zeroDecimal ? 1m : 100m;
        return (long)Math.Round(amount * factor, MidpointRounding.AwayFromZero);
    }

    private static bool IsZeroDecimalCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency)) return false;
        return currency.ToUpperInvariant() switch
        {
            "BIF" or "CLP" or "DJF" or "GNF" or "JPY" or "KMF" or "KRW" or "MGA" or "PYG" or
            "RWF" or "UGX" or "VND" or "VUV" or "XAF" or "XOF" or "XPF" => true,
            _ => false,
        };
    }

    private static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }
        return currency.Trim().ToLowerInvariant();
    }

    private static string NormalizeRefundReason(string reason)
    {
        var normalized = reason.Trim().ToLowerInvariant().Replace(' ', '_');
        return normalized switch
        {
            "duplicate" or "fraudulent" or "requested_by_customer" => normalized,
            _ => "requested_by_customer",
        };
    }

    private static PaymentIntentStatus MapStatus(string stripeStatus)
    {
        if (string.IsNullOrWhiteSpace(stripeStatus)) return PaymentIntentStatus.Pending;
        var s = stripeStatus.Trim().ToLowerInvariant();
        return s switch
        {
            "succeeded" or "payment_intent.succeeded" => PaymentIntentStatus.Succeeded,
            "processing" => PaymentIntentStatus.Pending,
            "requires_payment_method" or "payment_intent.payment_failed" => PaymentIntentStatus.Failed,
            "canceled" or "payment_intent.canceled" => PaymentIntentStatus.Cancelled,
            "requires_action" or "requires_confirmation" or "payment_intent.requires_action" => PaymentIntentStatus.RequiresAction,
            "requires_capture" => PaymentIntentStatus.RequiresAction,
            _ => PaymentIntentStatus.Pending,
        };
    }

    private static string? SerializeRaw<T>(T value)
    {
        if (value is null) return null;
        try
        {
            return JsonSerializer.Serialize(value, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record StripeInvocationContext(
        Guid TenantId,
        StripeCredentials Credentials,
        HttpClient HttpClient);

    private sealed record StripeWebhookEnvelope(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("livemode")] bool LiveMode,
        [property: JsonPropertyName("data")] StripeWebhookData? Data);

    private sealed record StripeWebhookData(
        [property: JsonPropertyName("object")] StripeWebhookIntentObject? Object);

    private sealed record StripeWebhookIntentObject(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("amount")] long Amount,
        [property: JsonPropertyName("currency")] string? Currency,
        [property: JsonPropertyName("latest_charge")] string? LatestCharge,
        [property: JsonPropertyName("payment_method")] string? PaymentMethod,
        [property: JsonPropertyName("last_payment_error")] StripeWebhookError? LastPaymentError);

    private sealed record StripeWebhookError(
        [property: JsonPropertyName("code")] string? Code,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("decline_code")] string? DeclineCode);
}
