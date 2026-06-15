using System.Text.Json.Serialization;

namespace CoreAlign.Infrastructure.Providers.Payment.Stripe;

public sealed record StripeCredentials(
    string SecretKey,
    string WebhookSigningSecret,
    string? AccountId,
    bool IsSandbox);

public sealed record StripeChargeRequest(
    long AmountMinor,
    string Currency,
    string? PaymentMethodId,
    string? CustomerId,
    string? Description,
    string? OrderReference,
    string? IdempotencyKey,
    bool Confirm,
    bool OffSession,
    string CaptureMethod,
    string? ReturnUrl,
    IReadOnlyDictionary<string, string>? Metadata);

public sealed record StripeNextAction(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("use_stripe_sdk")] object? UseStripeSdk,
    [property: JsonPropertyName("redirect_to_url")] StripeRedirectToUrl? RedirectToUrl);

public sealed record StripeRedirectToUrl(
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("return_url")] string? ReturnUrl);

public sealed record StripeChargeResult(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("client_secret")] string? ClientSecret,
    [property: JsonPropertyName("next_action")] StripeNextAction? NextAction,
    [property: JsonPropertyName("amount")] long Amount,
    [property: JsonPropertyName("amount_received")] long? AmountReceived,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("payment_method")] string? PaymentMethod,
    [property: JsonPropertyName("customer")] string? Customer,
    [property: JsonPropertyName("latest_charge")] string? LatestCharge,
    [property: JsonPropertyName("livemode")] bool LiveMode);

public sealed record StripeRefundRequest(
    string PaymentIntentId,
    long? AmountMinor,
    string? Reason,
    string? IdempotencyKey,
    IReadOnlyDictionary<string, string>? Metadata);

public sealed record StripeRefundResult(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("amount")] long Amount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("payment_intent")] string? PaymentIntent,
    [property: JsonPropertyName("charge")] string? Charge,
    [property: JsonPropertyName("reason")] string? Reason);

public sealed record StripePaymentMethodCard(
    [property: JsonPropertyName("brand")] string? Brand,
    [property: JsonPropertyName("last4")] string? Last4,
    [property: JsonPropertyName("exp_month")] int? ExpMonth,
    [property: JsonPropertyName("exp_year")] int? ExpYear,
    [property: JsonPropertyName("country")] string? Country,
    [property: JsonPropertyName("funding")] string? Funding);

public sealed record StripePaymentMethodResult(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("customer")] string? Customer,
    [property: JsonPropertyName("card")] StripePaymentMethodCard? Card);

public sealed record StripeErrorBody(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("decline_code")] string? DeclineCode,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("param")] string? Param,
    [property: JsonPropertyName("payment_intent")] StripeChargeResult? PaymentIntent);

public sealed record StripeErrorResponse(
    [property: JsonPropertyName("error")] StripeErrorBody? Error);

public sealed class StripeProviderException : Exception
{
    public string? ErrorCode { get; }
    public string? DeclineCode { get; }
    public int HttpStatusCode { get; }

    public StripeProviderException(string message, string? errorCode, string? declineCode, int httpStatusCode)
        : base(message)
    {
        ErrorCode = errorCode;
        DeclineCode = declineCode;
        HttpStatusCode = httpStatusCode;
    }

    public static StripeProviderException FromBody(int statusCode, string rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return new StripeProviderException($"Stripe returned HTTP {statusCode} with empty body.", null, null, statusCode);
        }

        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<StripeErrorResponse>(rawBody);
            var err = parsed?.Error;
            var msg = err?.Message ?? $"Stripe returned HTTP {statusCode}.";
            return new StripeProviderException(msg, err?.Code ?? err?.Type, err?.DeclineCode, statusCode);
        }
        catch (System.Text.Json.JsonException)
        {
            return new StripeProviderException($"Stripe returned HTTP {statusCode} with non-JSON body.", null, null, statusCode);
        }
    }
}
