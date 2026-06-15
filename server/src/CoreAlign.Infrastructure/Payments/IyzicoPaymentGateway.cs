using System.Globalization;
using System.Text.Json;
using CoreAlign.Application.Billing.Payments;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.Payments;

/// <summary>
/// Iyzico (https://iyzico.com) Checkout Form provider.
///
/// <para><b>How the redirect flow works</b></para>
/// <list type="number">
///   <item><c>CreateIntentAsync</c> initialises a Checkout Form session and returns
///     <see cref="PaymentIntentResult.RedirectUrl"/> — the SPA hands control to
///     Iyzico's hosted payment page.</item>
///   <item>After the customer pays, Iyzico POSTs the result to
///     <c>{CallbackBaseUrl}/api/v1/billing/webhooks/iyzico</c> as
///     <c>application/x-www-form-urlencoded</c> with a single <c>token</c> field.
///     <c>HandleWebhookAsync</c> retrieves the form result, maps to Succeeded /
///     Failed / Pending and lets the generic pipeline transition the order.</item>
///   <item>Out-of-band push notifications (refund, bank-transfer auth, ...) arrive
///     as JSON with an <c>x-iyzi-signature</c> HMAC-SHA1 header. The signature is
///     verified constant-time; unknown event types are acknowledged but do not
///     mutate order state.</item>
/// </list>
///
/// <para><b>Operator setup</b></para>
/// <list type="number">
///   <item>Register at https://merchant.iyzipay.com (live) or
///     https://sandbox-merchant.iyzipay.com (sandbox).</item>
///   <item>Set <c>Billing:Iyzico:ApiKey</c> and <c>Billing:Iyzico:SecretKey</c>
///     via env vars / secret manager. NEVER commit them.</item>
///   <item>Point <c>Billing:Iyzico:CallbackBaseUrl</c> at the public origin of
///     this API host (e.g. <c>https://app.corealign.io</c>). The full callback
///     URL is then <c>{CallbackBaseUrl}/api/v1/billing/webhooks/iyzico</c>.</item>
///   <item>In the Iyzico merchant dashboard, register that same URL as the
///     webhook target for refund / chargeback notifications.</item>
///   <item>Set <c>Billing:DefaultGatewayName</c> to <c>iyzico</c> in production.</item>
/// </list>
/// </summary>
public sealed class IyzicoPaymentGateway : IPaymentGateway
{
    public const string GatewayName = "iyzico";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IOptions<IyzicoOptions> _options;
    private readonly ILogger<IyzicoPaymentGateway> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public IyzicoPaymentGateway(
        IOptions<IyzicoOptions> options,
        ILogger<IyzicoPaymentGateway> logger,
        IServiceScopeFactory scopeFactory)
    {
        _options = options;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public string Name => GatewayName;

    public async Task<PaymentIntentResult> CreateIntentAsync(PaymentIntentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.BillingInfo is null)
        {
            throw new PaymentGatewayException("BillingInfo is required for the Iyzico gateway.", "BILLING_INFO_REQUIRED");
        }
        if (request.LineItems is null || request.LineItems.Count == 0)
        {
            throw new PaymentGatewayException("At least one line item is required.", "LINE_ITEMS_REQUIRED");
        }

        var options = _options.Value;
        var sdkOptions = options.ToIyzicoSdkOptions();

        var iyzReq = BuildCheckoutInitializeRequest(request, options);

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["OrderId"] = request.OrderId,
            ["OrderNumber"] = request.OrderNumber,
            ["Currency"] = request.Currency,
            ["Amount"] = IyzicoHelpers.FormatAmount(request.Amount),
            ["Provider"] = GatewayName,
        });
        _logger.LogInformation("Initialising Iyzico checkout form for order {OrderNumber}.", request.OrderNumber);

        CheckoutFormInitialize response;
        try
        {
            response = await Task.Run(() => CheckoutFormInitialize.Create(iyzReq, sdkOptions), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Iyzico checkout initialise threw for order {OrderNumber}.", request.OrderNumber);
            throw new PaymentGatewayException("Iyzico checkout initialisation failed.", "IYZICO_TRANSPORT_ERROR");
        }

        if (response is null || !string.Equals(response.Status, "success", StringComparison.OrdinalIgnoreCase))
        {
            var safeMessage = response?.ErrorMessage ?? "Unknown Iyzico error.";
            var errorCode = response?.ErrorCode ?? "IYZICO_INIT_FAILED";
            _logger.LogWarning("Iyzico rejected checkout initialise: {ErrorCode}", errorCode);
            throw new PaymentGatewayException(safeMessage, errorCode);
        }

        var metadata = new Dictionary<string, string>
        {
            ["iyzicoConversationId"] = response.ConversationId ?? request.OrderId.ToString(),
            ["paymentPageUrl"] = response.PaymentPageUrl ?? string.Empty,
        };

        var raw = JsonSerializer.Serialize(new
        {
            status = response.Status,
            token = response.Token,
            checkoutFormContent = (string?)null,
            paymentPageUrl = response.PaymentPageUrl,
            conversationId = response.ConversationId,
        }, JsonOptions);

        return new PaymentIntentResult(
            IntentId: response.Token ?? throw new PaymentGatewayException("Iyzico returned no token.", "IYZICO_NO_TOKEN"),
            RedirectUrl: response.PaymentPageUrl,
            Status: PaymentIntentStatus.Pending,
            Metadata: metadata,
            RawJson: raw);
    }

    public async Task<WebhookProcessingResult> HandleWebhookAsync(string payload, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload)) throw new ArgumentException("Payload is required.", nameof(payload));
        ArgumentNullException.ThrowIfNull(headers);

        var options = _options.Value;

        if (IyzicoHelpers.IsJsonPush(headers))
        {
            return await HandlePushNotificationAsync(payload, headers, options, cancellationToken).ConfigureAwait(false);
        }

        var form = IyzicoHelpers.ParseFormUrlEncoded(payload);
        if (!form.TryGetValue("token", out var token) || string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Iyzico callback is missing the 'token' field.", nameof(payload));
        }

        var sdkOptions = options.ToIyzicoSdkOptions();
        var retrieveRequest = new RetrieveCheckoutFormRequest
        {
            Locale = options.DefaultLocale,
            ConversationId = string.Empty,
            Token = token,
        };

        CheckoutForm response;
        try
        {
            response = await Task.Run(() => CheckoutForm.Retrieve(retrieveRequest, sdkOptions), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Iyzico checkout retrieve threw for token (redacted).");
            throw new PaymentGatewayException("Iyzico checkout retrieve failed.", "IYZICO_TRANSPORT_ERROR");
        }

        if (response is null)
        {
            throw new PaymentGatewayException("Iyzico returned an empty response.", "IYZICO_EMPTY_RESPONSE");
        }

        var status = IyzicoHelpers.MapPaymentStatus(response.PaymentStatus, response.Status);
        var failureReason = status == PaymentIntentStatus.Failed
            ? (response.ErrorMessage ?? response.PaymentStatus ?? "Iyzico reported failure.")
            : null;

        var raw = JsonSerializer.Serialize(new
        {
            status = response.Status,
            paymentStatus = response.PaymentStatus,
            paymentId = response.PaymentId,
            errorCode = response.ErrorCode,
            errorMessage = response.ErrorMessage,
            errorGroup = response.ErrorGroup,
            conversationId = response.ConversationId,
        }, JsonOptions);

        _logger.LogInformation(
            "Iyzico webhook retrieve mapped paymentStatus {PaymentStatus} -> {MappedStatus}.",
            response.PaymentStatus, status);

        return new WebhookProcessingResult(
            IntentId: token,
            Status: status,
            Reference: response.PaymentId,
            FailureReason: failureReason,
            RawJson: raw);
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

        var options = _options.Value;
        var sdkOptions = options.ToIyzicoSdkOptions();

        var refundRequest = new CreateRefundRequest
        {
            Locale = options.DefaultLocale,
            ConversationId = request.IntentId,
            PaymentTransactionId = paymentTxnId,
            Price = IyzicoHelpers.FormatAmount(request.Amount.Value),
            Ip = "127.0.0.1",
            Currency = request.Currency is null ? null : IyzicoHelpers.MapCurrency(request.Currency).ToString(),
        };

        Refund response;
        try
        {
            response = await Task.Run(() => Refund.Create(refundRequest, sdkOptions), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Iyzico refund transport failed for txn {PaymentTransactionId}.", paymentTxnId);
            return new RefundResult(false, null, null, "Iyzico refund transport failed.");
        }

        if (response is null || !string.Equals(response.Status, "success", StringComparison.OrdinalIgnoreCase))
        {
            var msg = response?.ErrorMessage ?? "Iyzico refund failed.";
            return new RefundResult(false, null, null, msg);
        }

        var raw = JsonSerializer.Serialize(new
        {
            status = response.Status,
            paymentId = response.PaymentId,
            paymentTransactionId = response.PaymentTransactionId,
            price = response.Price,
            currency = response.Currency,
        }, JsonOptions);

        return new RefundResult(true, response.PaymentId, raw, null);
    }

    private async Task<WebhookProcessingResult> HandlePushNotificationAsync(string payload, IReadOnlyDictionary<string, string> headers, IyzicoOptions options, CancellationToken cancellationToken)
    {
        headers.TryGetValue("x-iyzi-signature", out var signature);
        var verified = IyzicoHelpers.VerifyPushSignature(options.ApiKey, options.SecretKey, payload, signature);
        if (!verified)
        {
            _logger.LogWarning("Iyzico push notification signature verification FAILED.");
            throw new PaymentWebhookSignatureException("Iyzico push notification signature is invalid.");
        }

        string? eventType = null;
        string? token = null;
        string? conversationId = null;
        string? paymentTransactionId = null;
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("eventType", out var et)) eventType = et.GetString();
            if (doc.RootElement.TryGetProperty("token", out var tk)) token = tk.GetString();
            if (doc.RootElement.TryGetProperty("conversationId", out var cv)) conversationId = cv.GetString();
            if (doc.RootElement.TryGetProperty("paymentTransactionId", out var pt)) paymentTransactionId = pt.GetString();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Iyzico push notification body is not valid JSON.");
            throw new ArgumentException("Iyzico push notification body is not valid JSON.", nameof(payload));
        }

        var resolvedEventType = string.IsNullOrWhiteSpace(eventType) ? "unknown" : eventType;
        var resolvedEventId = !string.IsNullOrWhiteSpace(conversationId)
            ? conversationId
            : (!string.IsNullOrWhiteSpace(paymentTransactionId)
                ? paymentTransactionId
                : token);

        _logger.LogInformation("Iyzico push notification received: eventType={EventType}", resolvedEventType);

        if (!string.IsNullOrWhiteSpace(resolvedEventId))
        {
            using var scope = _scopeFactory.CreateScope();
            var processed = scope.ServiceProvider.GetRequiredService<IProcessedWebhookEventRepository>();

            if (await processed.ExistsAsync(GatewayName, resolvedEventId, resolvedEventType, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                _logger.LogInformation(
                    "Iyzico push notification replay detected for eventId={EventId} eventType={EventType}; skipping.",
                    resolvedEventId, resolvedEventType);
                return new WebhookProcessingResult(
                    IntentId: token ?? string.Empty,
                    Status: PaymentIntentStatus.Pending,
                    Reference: resolvedEventType,
                    FailureReason: null,
                    RawJson: payload);
            }

            await processed.AddAsync(new ProcessedWebhookEvent
            {
                Gateway = GatewayName,
                EventId = resolvedEventId,
                EventType = resolvedEventType,
                ProcessedAtUtc = DateTime.UtcNow,
            }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return new WebhookProcessingResult(
            IntentId: token ?? string.Empty,
            Status: PaymentIntentStatus.Pending,
            Reference: resolvedEventType,
            FailureReason: null,
            RawJson: payload);
    }

    private CreateCheckoutFormInitializeRequest BuildCheckoutInitializeRequest(PaymentIntentRequest request, IyzicoOptions options)
    {
        var billing = request.BillingInfo!;
        var lineItems = request.LineItems!;
        var formattedTotal = IyzicoHelpers.FormatAmount(request.Amount);
        var formattedLineSum = IyzicoHelpers.FormatAmount(lineItems.Sum(l => l.UnitPrice));
        if (!string.Equals(formattedTotal, formattedLineSum, StringComparison.Ordinal))
        {
            throw new PaymentGatewayException(
                $"Iyzico requires the sum of basket items ({formattedLineSum}) to equal the total ({formattedTotal}).",
                "IYZICO_BASKET_TOTAL_MISMATCH");
        }

        var nowFormatted = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        var buyer = new Buyer
        {
            Id = request.CreatedByUserId == Guid.Empty ? request.TenantId.ToString() : request.CreatedByUserId.ToString(),
            Name = billing.Name,
            Surname = billing.Surname,
            GsmNumber = billing.GsmNumber,
            Email = billing.Email,
            IdentityNumber = billing.IdentityNumber,
            LastLoginDate = nowFormatted,
            RegistrationDate = nowFormatted,
            RegistrationAddress = billing.Address,
            Ip = billing.IpAddress,
            City = billing.City,
            Country = billing.Country,
            ZipCode = billing.ZipCode,
        };

        var contactName = string.IsNullOrWhiteSpace(billing.Surname)
            ? billing.Name
            : $"{billing.Name} {billing.Surname}".Trim();
        var address = new Address
        {
            ContactName = contactName,
            City = billing.City,
            Country = billing.Country,
            Description = billing.Address,
            ZipCode = billing.ZipCode,
        };

        var basket = lineItems.Select(l => new BasketItem
        {
            Id = l.Id,
            Name = Truncate(l.Name, 100),
            Category1 = string.IsNullOrWhiteSpace(l.Category) ? "Software" : l.Category,
            ItemType = BasketItemType.VIRTUAL.ToString(),
            Price = IyzicoHelpers.FormatAmount(l.UnitPrice),
        }).ToList();

        var callbackBase = string.IsNullOrWhiteSpace(options.CallbackBaseUrl)
            ? throw new PaymentGatewayException("Billing:Iyzico:CallbackBaseUrl is not configured.", "IYZICO_CALLBACK_URL_MISSING")
            : options.CallbackBaseUrl.TrimEnd('/');

        return new CreateCheckoutFormInitializeRequest
        {
            Locale = options.DefaultLocale,
            ConversationId = request.OrderId.ToString(),
            Price = formattedTotal,
            PaidPrice = formattedTotal,
            Currency = IyzicoHelpers.MapCurrency(request.Currency).ToString(),
            BasketId = Truncate(request.OrderNumber, 64),
            PaymentGroup = PaymentGroup.SUBSCRIPTION.ToString(),
            CallbackUrl = $"{callbackBase}/api/v1/billing/webhooks/iyzico",
            EnabledInstallments = options.AllowInstallments
                ? new List<int> { 2, 3, 6, 9 }
                : new List<int>(),
            Buyer = buyer,
            BillingAddress = address,
            ShippingAddress = address,
            BasketItems = basket,
        };
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
