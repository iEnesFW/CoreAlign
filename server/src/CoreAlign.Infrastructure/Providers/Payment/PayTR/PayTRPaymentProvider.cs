using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;
using CoreAlign.Application.Billing.Payments;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.Payment;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using BillingPaymentIntentRequest = CoreAlign.Application.Billing.Payments.PaymentIntentRequest;
using ProviderPaymentIntentRequest = CoreAlign.Application.Providers.Payment.PaymentIntentRequest;

namespace CoreAlign.Infrastructure.Providers.Payment.PayTR;

/// <summary>
/// PayTR (https://www.paytr.com) payment provider. Implements the F1.8
/// <see cref="IPaymentProvider"/> contract — both the high-level method
/// catalogue / link creation API and the underlying <see cref="IPaymentGateway"/>
/// CreateIntent/Webhook/Capture/Refund pipeline.
///
/// <para><b>Hashing</b></para>
/// Every PayTR request carries an HMAC-SHA256 signature built by
/// <see cref="PayTRHashBuilder"/>; the merchant salt is folded into the hash
/// payload and the merchant key is the HMAC key. Callbacks are verified by
/// <see cref="PayTRWebhookVerifier"/>.
///
/// <para><b>Sandbox vs production</b></para>
/// PayTR exposes the same hostname for sandbox and production; the only
/// difference is the <c>test_mode=1</c> body parameter the SDK injects when
/// <see cref="PayTRCredentials.IsSandbox"/> is true.
///
/// <para><b>Tokenisation</b></para>
/// Raw card numbers NEVER touch the database; <see cref="TokenizeCardAsync"/>
/// stores them at PayTR and returns the opaque <c>user_token</c> / <c>card_token</c>
/// pair which the application persists in place of PAN.
/// </summary>
public sealed class PayTRPaymentProvider : IPaymentProvider
{
    public const string ProviderKey = "paytr";
    public const string HttpClientName = "PayTRPayment";

    private const string BaseUrl = "https://www.paytr.com";
    private const string GetTokenPath = "/odeme/api/get-token";
    private const string RefundPath = "/odeme/iade";
    private const string StatusPath = "/odeme/durum-sorgu";
    private const string TokenizePath = "/cuzdan/api/kart-kaydet";
    private const string DefaultCurrency = "TL";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITenantProviderConfigResolver _configResolver;
    private readonly IProviderCredentialProtector _credentialProtector;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PayTRPaymentProvider> _logger;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public PayTRPaymentProvider(
        IHttpClientFactory httpClientFactory,
        ITenantProviderConfigResolver configResolver,
        IProviderCredentialProtector credentialProtector,
        ITenantContext tenantContext,
        ILogger<PayTRPaymentProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configResolver = configResolver;
        _credentialProtector = credentialProtector;
        _tenantContext = tenantContext;
        _logger = logger;
        _retryPolicy = BuildRetryPolicy();
    }

    public string Name => ProviderKey;

    public string DisplayName => "PayTR";

    public ProviderCapabilities Capabilities => new(
        ProviderCapability.WebhookCallback
            | ProviderCapability.RealTimeStatus
            | ProviderCapability.Refund
            | ProviderCapability.SignatureValidation,
        new Dictionary<string, string>
        {
            ["transport"] = "rest+hmac-sha256",
            ["region"] = "TR",
        });

    public async Task<ProviderHealthCheckResult> CheckHealthAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        const string endpoint = "/test";
        var started = DateTime.UtcNow;
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl + endpoint);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var elapsed = DateTime.UtcNow - started;
            return (int)response.StatusCode < 500
                ? ProviderHealthCheckResult.Healthy(Name, elapsed, endpoint, (int)response.StatusCode)
                : ProviderHealthCheckResult.Unhealthy(Name, $"HTTP {(int)response.StatusCode}", elapsed, endpoint, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PayTR health check failed for tenant {TenantId}.", tenantId);
            return ProviderHealthCheckResult.Unhealthy(Name, ex.Message, DateTime.UtcNow - started, endpoint);
        }
    }

    public Task<IReadOnlyList<PaymentMethodDescriptor>> ListMethodsAsync(Guid tenantId, CancellationToken ct)
    {
        IReadOnlyList<PaymentMethodDescriptor> list = new[]
        {
            new PaymentMethodDescriptor(
                PaymentMethodKind.ThreeDS,
                "PayTR 3D Secure",
                MinAmount: 1m,
                MaxAmount: 250_000m,
                SupportedCurrencies: new[] { "TRY", "USD", "EUR", "GBP" }),
            new PaymentMethodDescriptor(
                PaymentMethodKind.Installment,
                "PayTR Installment",
                MinAmount: 50m,
                MaxAmount: 250_000m,
                SupportedCurrencies: new[] { "TRY" }),
            new PaymentMethodDescriptor(
                PaymentMethodKind.CardOnFile,
                "PayTR Saved Card",
                MinAmount: 1m,
                MaxAmount: 250_000m,
                SupportedCurrencies: new[] { "TRY" }),
        };
        return Task.FromResult(list);
    }

    public async Task<PaymentLinkResult> CreateLinkAsync(ProviderPaymentIntentRequest req, PaymentLinkOptions opts, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(opts);

        var tenantId = _tenantContext.RequireTenantId();
        var creds = await ResolveCredentialsAsync(tenantId, ct).ConfigureAwait(false);

        var charge = new PayTRChargeRequest(
            MerchantOid: SanitizeOid(req.OrderReference),
            Email: req.BuyerEmail,
            PaymentAmount: req.Amount,
            Currency: MapCurrency(req.Currency),
            UserIp: "127.0.0.1",
            UserName: req.BuyerName,
            UserAddress: "N/A",
            UserPhone: "N/A",
            MerchantOkUrl: opts.CallbackUrl,
            MerchantFailUrl: opts.CallbackUrl,
            UserBasket: BuildSingleBasket(req.OrderReference, req.Amount));

        var result = await GetIframeTokenAsync(creds, charge, ct).ConfigureAwait(false);
        if (!string.Equals(result.Status, "success", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(result.Token))
        {
            throw new PaymentGatewayException(result.ErrorMessage ?? "PayTR iframe token request failed.", "PAYTR_TOKEN_FAILED");
        }

        var expiry = DateTime.UtcNow.AddMinutes(opts.ExpiryMinutes <= 0 ? 30 : opts.ExpiryMinutes);
        var iframeUrl = $"{BaseUrl}/odeme/guvenli/{result.Token}";
        return new PaymentLinkResult(iframeUrl, expiry, result.Token!);
    }

    public async Task<PaymentIntentResult> CreateIntentAsync(BillingPaymentIntentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.BillingInfo is null)
        {
            throw new PaymentGatewayException("BillingInfo is required for the PayTR gateway.", "BILLING_INFO_REQUIRED");
        }

        var creds = await ResolveCredentialsAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        var basket = BuildBasketFromLineItems(request);
        var charge = new PayTRChargeRequest(
            MerchantOid: SanitizeOid(request.OrderNumber),
            Email: request.BillingInfo.Email,
            PaymentAmount: request.Amount,
            Currency: MapCurrency(request.Currency),
            UserIp: string.IsNullOrWhiteSpace(request.BillingInfo.IpAddress) ? "127.0.0.1" : request.BillingInfo.IpAddress,
            UserName: BuildContactName(request.BillingInfo.Name, request.BillingInfo.Surname),
            UserAddress: request.BillingInfo.Address,
            UserPhone: request.BillingInfo.GsmNumber,
            MerchantOkUrl: BuildCallbackUrl(request),
            MerchantFailUrl: BuildCallbackUrl(request),
            UserBasket: basket);

        var result = await GetIframeTokenAsync(creds, charge, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(result.Status, "success", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(result.Token))
        {
            throw new PaymentGatewayException(result.ErrorMessage ?? "PayTR iframe token request failed.", "PAYTR_TOKEN_FAILED");
        }

        var metadata = new Dictionary<string, string>
        {
            ["paytrToken"] = result.Token!,
            ["iframeUrl"] = result.IframeUrl ?? $"{BaseUrl}/odeme/guvenli/{result.Token}",
        };

        return new PaymentIntentResult(
            IntentId: result.Token!,
            RedirectUrl: result.IframeUrl ?? $"{BaseUrl}/odeme/guvenli/{result.Token}",
            Status: PaymentIntentStatus.Pending,
            Metadata: metadata,
            RawJson: result.RawJson);
    }

    public async Task<WebhookProcessingResult> HandleWebhookAsync(string payload, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(headers);
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Payload is required.", nameof(payload));
        }

        if (!PayTRWebhookVerifier.TryParseCallback(payload, out var merchantOid, out var status, out var totalAmount, out var receivedHash))
        {
            throw new ArgumentException("PayTR callback is missing required fields.", nameof(payload));
        }

        var tenantId = _tenantContext.RequireTenantId();
        var creds = await ResolveCredentialsAsync(tenantId, cancellationToken).ConfigureAwait(false);

        var verified = PayTRHashBuilder.VerifyCallback(
            merchantOid,
            status,
            totalAmount,
            receivedHash,
            creds.MerchantKey,
            creds.MerchantSalt);
        if (!verified)
        {
            _logger.LogWarning("PayTR callback signature verification FAILED for tenant {TenantId}.", tenantId);
            throw new PaymentWebhookSignatureException("PayTR callback signature is invalid.");
        }

        var mapped = MapCallbackStatus(status);
        var failureReason = mapped == PaymentIntentStatus.Failed
            ? (HttpUtility.ParseQueryString(payload)["failed_reason_msg"] ?? "PayTR reported failure.")
            : null;

        return new WebhookProcessingResult(
            IntentId: merchantOid,
            Status: mapped,
            Reference: merchantOid,
            FailureReason: failureReason,
            RawJson: payload);
    }

    public Task<CaptureResult> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(new CaptureResult(true, request.IntentId, null, null));
    }

    public async Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.IntentId))
        {
            return new RefundResult(false, null, null, "PayTR refund requires the original merchant_oid as IntentId.");
        }
        if (request.Amount is null || request.Amount.Value <= 0m)
        {
            return new RefundResult(false, null, null, "Refund amount must be greater than zero.");
        }

        var tenantId = _tenantContext.RequireTenantId();
        var creds = await ResolveCredentialsAsync(tenantId, cancellationToken).ConfigureAwait(false);

        var refund = new PayTRRefundRequest(request.IntentId, request.Amount.Value, request.PaymentTransactionId);
        var result = await RefundInternalAsync(creds, refund, cancellationToken).ConfigureAwait(false);

        return string.Equals(result.Status, "success", StringComparison.OrdinalIgnoreCase)
            ? new RefundResult(true, result.ReturnRefId, result.RawJson, null)
            : new RefundResult(false, null, result.RawJson, result.ErrorMessage ?? "PayTR refund failed.");
    }

    /// <summary>
    /// Initiates a PayTR charge. Always routes through the iframe / vault
    /// flow — raw card data is never accepted. If the caller supplies a
    /// vault <see cref="PayTRChargeRequest.UserToken"/> the SDK collapses
    /// the iframe to a saved-card confirmation; otherwise a fresh iframe
    /// token is returned for the frontend to load.
    /// </summary>
    public async Task<PayTRChargeResult> ChargeAsync(PayTRChargeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = _tenantContext.RequireTenantId();
        var creds = await ResolveCredentialsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return await GetIframeTokenAsync(creds, request, cancellationToken).ConfigureAwait(false);
    }

    public Task<PayTRChargeResult> Initiate3DSecureAsync(PayTRChargeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ResolveCredentialsAndExecuteAsync(
            (creds, ct) => GetIframeTokenAsync(creds, request, ct),
            cancellationToken);
    }

    public Task<bool> Verify3DSecureAsync(PayTRCallbackPayload payload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return ResolveCredentialsAndExecuteAsync<bool>(
            (creds, _) =>
            {
                var verified = PayTRHashBuilder.VerifyCallback(
                    payload.MerchantOid,
                    payload.Status,
                    payload.TotalAmount,
                    payload.Hash,
                    creds.MerchantKey,
                    creds.MerchantSalt);
                return Task.FromResult(verified);
            },
            cancellationToken);
    }

    public Task<PayTRStatusResult> GetTransactionAsync(string merchantOid, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(merchantOid);
        return ResolveCredentialsAndExecuteAsync(
            (creds, ct) => GetTransactionInternalAsync(creds, merchantOid, ct),
            cancellationToken);
    }

    public Task<PayTRTokenizeResult> TokenizeCardAsync(PayTRTokenizeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ResolveCredentialsAndExecuteAsync(
            (creds, ct) => TokenizeInternalAsync(creds, request, ct),
            cancellationToken);
    }

    private async Task<TResult> ResolveCredentialsAndExecuteAsync<TResult>(
        Func<PayTRCredentials, CancellationToken, Task<TResult>> work,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var creds = await ResolveCredentialsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return await work(creds, cancellationToken).ConfigureAwait(false);
    }

    private async Task<PayTRCredentials> ResolveCredentialsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var encrypted = await _configResolver
            .GetEncryptedCredentialsAsync(tenantId, ProviderCategory.Payment, ProviderKey, cancellationToken)
            .ConfigureAwait(false);

        var credentials = _credentialProtector.UnprotectAs<PayTRCredentials>(tenantId, ProviderCategory.Payment, encrypted)
            ?? throw new PayTRProviderException("CREDENTIALS_MISSING", "PayTR credentials are not configured for the current tenant.");

        if (string.IsNullOrWhiteSpace(credentials.MerchantId)
            || string.IsNullOrWhiteSpace(credentials.MerchantKey)
            || string.IsNullOrWhiteSpace(credentials.MerchantSalt))
        {
            throw new PayTRProviderException("CREDENTIALS_INCOMPLETE", "PayTR merchant credentials are incomplete.");
        }

        return credentials;
    }

    private async Task<PayTRChargeResult> GetIframeTokenAsync(PayTRCredentials creds, PayTRChargeRequest charge, CancellationToken ct)
    {
        var amountInCents = ToCents(charge.PaymentAmount);
        var basketBase64 = EncodeBasket(charge.UserBasket);
        var hash = PayTRHashBuilder.BuildChargeHash(
            creds.MerchantId,
            charge.UserIp,
            charge.MerchantOid,
            charge.Email,
            charge.PaymentAmount,
            charge.Currency,
            creds.IsSandbox,
            creds.MerchantKey,
            creds.MerchantSalt);

        var form = new Dictionary<string, string>
        {
            ["merchant_id"] = creds.MerchantId,
            ["user_ip"] = charge.UserIp,
            ["merchant_oid"] = charge.MerchantOid,
            ["email"] = charge.Email,
            ["payment_amount"] = amountInCents.ToString(CultureInfo.InvariantCulture),
            ["currency"] = charge.Currency,
            ["test_mode"] = creds.IsSandbox ? "1" : "0",
            ["paytr_token"] = hash,
            ["user_basket"] = basketBase64,
            ["debug_on"] = creds.IsSandbox ? "1" : "0",
            ["no_installment"] = charge.Installment > 0 ? "0" : "1",
            ["max_installment"] = charge.Installment.ToString(CultureInfo.InvariantCulture),
            ["user_name"] = charge.UserName,
            ["user_address"] = charge.UserAddress,
            ["user_phone"] = charge.UserPhone,
            ["merchant_ok_url"] = charge.MerchantOkUrl,
            ["merchant_fail_url"] = charge.MerchantFailUrl,
            ["timeout_limit"] = "30",
            ["lang"] = "tr",
        };

        if (!string.IsNullOrWhiteSpace(charge.UserToken))
        {
            form["user_token"] = charge.UserToken!;
        }
        if (!string.IsNullOrWhiteSpace(charge.CardToken))
        {
            form["card_token"] = charge.CardToken!;
        }

        var (raw, status, token, errorMessage) = await PostFormAsync<PayTRTokenApiResponse>(
            GetTokenPath,
            form,
            ct,
            apiResp => (apiResp.Status, apiResp.Token, apiResp.Reason ?? apiResp.ErrorMessage)).ConfigureAwait(false);

        var iframeUrl = string.IsNullOrWhiteSpace(token) ? null : $"{BaseUrl}/odeme/guvenli/{token}";
        return new PayTRChargeResult(status, token, PaymentId: null, ErrorMessage: errorMessage, IframeUrl: iframeUrl, RawJson: raw);
    }

    private async Task<PayTRRefundResult> RefundInternalAsync(PayTRCredentials creds, PayTRRefundRequest refund, CancellationToken ct)
    {
        var amountInCents = ToCents(refund.ReturnAmount);
        var hash = PayTRHashBuilder.BuildRefundHash(
            creds.MerchantId,
            refund.MerchantOid,
            refund.ReturnAmount,
            creds.MerchantKey,
            creds.MerchantSalt);

        var form = new Dictionary<string, string>
        {
            ["merchant_id"] = creds.MerchantId,
            ["merchant_oid"] = refund.MerchantOid,
            ["return_amount"] = (amountInCents / 100m).ToString("F2", CultureInfo.InvariantCulture),
            ["paytr_token"] = hash,
        };
        if (!string.IsNullOrWhiteSpace(refund.ReferenceId))
        {
            form["reference_no"] = refund.ReferenceId!;
        }

        var (raw, status, refId, errorMessage) = await PostFormAsync<PayTRRefundApiResponse>(
            RefundPath,
            form,
            ct,
            apiResp => (apiResp.Status, apiResp.ReturnRefId, apiResp.ErrorMessage)).ConfigureAwait(false);

        return new PayTRRefundResult(status, refId, errorMessage, raw);
    }

    private async Task<PayTRStatusResult> GetTransactionInternalAsync(PayTRCredentials creds, string merchantOid, CancellationToken ct)
    {
        var hash = PayTRHashBuilder.BuildStatusHash(creds.MerchantId, merchantOid, creds.MerchantKey, creds.MerchantSalt);
        var form = new Dictionary<string, string>
        {
            ["merchant_id"] = creds.MerchantId,
            ["merchant_oid"] = merchantOid,
            ["paytr_token"] = hash,
        };

        var apiResp = await PostFormRawAsync<PayTRStatusApiResponse>(StatusPath, form, ct).ConfigureAwait(false);
        return new PayTRStatusResult(
            apiResp.Parsed.Status,
            apiResp.Parsed.PaymentStatus,
            apiResp.Parsed.PaymentTotal,
            apiResp.Parsed.PaymentDate,
            apiResp.Parsed.FailReason,
            apiResp.Raw);
    }

    private async Task<PayTRTokenizeResult> TokenizeInternalAsync(PayTRCredentials creds, PayTRTokenizeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.EphemeralCardToken))
        {
            throw new PayTRProviderException("CARD_TOKEN_MISSING", "An ephemeral card token from the PayTR iframe SDK is required; raw PAN is not accepted.");
        }

        var hash = PayTRHashBuilder.BuildTokenizeHash(creds.MerchantId, request.MerchantOid, request.Email, creds.MerchantKey, creds.MerchantSalt);
        var form = new Dictionary<string, string>
        {
            ["merchant_id"] = creds.MerchantId,
            ["merchant_oid"] = request.MerchantOid,
            ["email"] = request.Email,
            ["user_ip"] = request.UserIp,
            ["ephemeral_card_token"] = request.EphemeralCardToken,
            ["card_alias"] = request.CardAlias,
            ["test_mode"] = creds.IsSandbox ? "1" : "0",
            ["paytr_token"] = hash,
        };

        var apiResp = await PostFormRawAsync<PayTRTokenizeApiResponse>(TokenizePath, form, ct).ConfigureAwait(false);
        return new PayTRTokenizeResult(
            apiResp.Parsed.Status,
            apiResp.Parsed.UserToken,
            apiResp.Parsed.CardToken,
            apiResp.Parsed.Last4,
            apiResp.Parsed.Brand,
            apiResp.Parsed.ErrorMessage,
            apiResp.Raw);
    }

    private async Task<(string Raw, string Status, string? Field, string? ErrorMessage)> PostFormAsync<TApiResponse>(
        string path,
        IReadOnlyDictionary<string, string> form,
        CancellationToken ct,
        Func<TApiResponse, (string Status, string? Field, string? ErrorMessage)> projector)
        where TApiResponse : class
    {
        var result = await PostFormRawAsync<TApiResponse>(path, form, ct).ConfigureAwait(false);
        var (status, field, errorMessage) = projector(result.Parsed);
        return (result.Raw, status, field, errorMessage);
    }

    private async Task<(TApiResponse Parsed, string Raw)> PostFormRawAsync<TApiResponse>(
        string path,
        IReadOnlyDictionary<string, string> form,
        CancellationToken ct)
        where TApiResponse : class
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        if (client.BaseAddress is null)
        {
            client.BaseAddress = new Uri(BaseUrl);
        }

        var response = await _retryPolicy.ExecuteAsync(async pollyCt =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = new FormUrlEncodedContent(form),
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, pollyCt).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

        try
        {
            var raw = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new PayTRProviderException(
                    $"HTTP_{(int)response.StatusCode}",
                    $"PayTR returned non-success status {(int)response.StatusCode}.");
            }

            TApiResponse? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<TApiResponse>(raw, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new PayTRProviderException("RESPONSE_INVALID_JSON", $"PayTR response is not valid JSON: {ex.Message}");
            }

            if (parsed is null)
            {
                throw new PayTRProviderException("RESPONSE_EMPTY", "PayTR response body was empty.");
            }

            return (parsed, raw);
        }
        finally
        {
            response.Dispose();
        }
    }

    private AsyncRetryPolicy<HttpResponseMessage> BuildRetryPolicy() =>
        Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>(ex => !ex.CancellationToken.IsCancellationRequested)
            .OrResult(static r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: static attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)),
                onRetry: (outcome, delay, attempt, _) =>
                {
                    _logger.LogWarning(
                        outcome.Exception,
                        "PayTR HTTP attempt {Attempt} failed (status {Status}); retrying in {Delay}.",
                        attempt,
                        outcome.Result?.StatusCode,
                        delay);
                });

    private static IReadOnlyList<string> BuildSingleBasket(string description, decimal amount)
    {
        var label = string.IsNullOrWhiteSpace(description) ? "Order" : description;
        return new[] { JsonSerializer.Serialize(new object[] { label, amount.ToString("F2", CultureInfo.InvariantCulture), 1 }) };
    }

    private static IReadOnlyList<string> BuildBasketFromLineItems(BillingPaymentIntentRequest request)
    {
        if (request.LineItems is null || request.LineItems.Count == 0)
        {
            return BuildSingleBasket(request.OrderNumber, request.Amount);
        }

        var items = new List<string>(request.LineItems.Count);
        foreach (var item in request.LineItems)
        {
            items.Add(JsonSerializer.Serialize(new object[]
            {
                item.Name,
                item.UnitPrice.ToString("F2", CultureInfo.InvariantCulture),
                1,
            }));
        }
        return items;
    }

    private static string EncodeBasket(IReadOnlyList<string> basketEntries)
    {
        var parsedEntries = new List<object[]>(basketEntries.Count);
        foreach (var entry in basketEntries)
        {
            try
            {
                var arr = JsonSerializer.Deserialize<object[]>(entry);
                if (arr is not null)
                {
                    parsedEntries.Add(arr);
                }
            }
            catch (JsonException)
            {
            }
        }
        var json = JsonSerializer.Serialize(parsedEntries);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static long ToCents(decimal amount) =>
        (long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);

    private static string SanitizeOid(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return Guid.NewGuid().ToString("N");
        }
        var builder = new StringBuilder(source.Length);
        foreach (var c in source)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
            }
        }
        return builder.Length == 0 ? Guid.NewGuid().ToString("N") : builder.ToString();
    }

    private static string MapCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return DefaultCurrency;
        }
        return currency.ToUpperInvariant() switch
        {
            "TRY" or "TL" => "TL",
            "USD" => "USD",
            "EUR" => "EUR",
            "GBP" => "GBP",
            _ => currency.ToUpperInvariant(),
        };
    }

    private static string BuildContactName(string name, string surname) =>
        string.IsNullOrWhiteSpace(surname) ? name : $"{name} {surname}".Trim();

    private static string BuildCallbackUrl(BillingPaymentIntentRequest request)
    {
        if (request.Metadata is not null && request.Metadata.TryGetValue("callbackUrl", out var configured) && !string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }
        return $"/api/v1/billing/webhooks/{ProviderKey}";
    }

    private static PaymentIntentStatus MapCallbackStatus(string status) =>
        status.ToLowerInvariant() switch
        {
            "success" => PaymentIntentStatus.Succeeded,
            "failed" => PaymentIntentStatus.Failed,
            "pending" => PaymentIntentStatus.Pending,
            _ => PaymentIntentStatus.RequiresAction,
        };
}
