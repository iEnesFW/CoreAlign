using System.Text.Json.Serialization;

namespace CoreAlign.Infrastructure.Providers.Payment.PayTR;

public sealed record PayTRCredentials(
    string MerchantId,
    string MerchantKey,
    string MerchantSalt,
    bool IsSandbox);

/// <summary>
/// PCI-DSS safe PayTR charge payload. Raw card data NEVER touches the
/// backend; the cardholder's browser tokenizes the card with the PayTR
/// iframe SDK and the application persists only the opaque
/// <c>UserToken</c> (vault reference) plus optional <c>CardToken</c>.
/// </summary>
public sealed record PayTRChargeRequest(
    string MerchantOid,
    string Email,
    decimal PaymentAmount,
    string Currency,
    string UserIp,
    string UserName,
    string UserAddress,
    string UserPhone,
    string MerchantOkUrl,
    string MerchantFailUrl,
    IReadOnlyList<string> UserBasket,
    string? UserToken = null,
    string? CardToken = null,
    int Installment = 0);

public sealed record PayTRChargeResult(
    string Status,
    string? Token,
    string? PaymentId,
    string? ErrorMessage,
    string? IframeUrl,
    string RawJson);

/// <summary>
/// PayTR vault tokenization request. Accepts only the ephemeral
/// <c>EphemeralCardToken</c> obtained client-side from the PayTR iframe
/// SDK plus a display alias. PAN / CVC / expiry NEVER reach the backend.
/// </summary>
public sealed record PayTRTokenizeRequest(
    string MerchantOid,
    string Email,
    string UserIp,
    string EphemeralCardToken,
    string CardAlias);

public sealed record PayTRTokenizeResult(
    string Status,
    string? UserToken,
    string? CardToken,
    string? Last4,
    string? Brand,
    string? ErrorMessage,
    string RawJson);

public sealed record PayTRRefundRequest(
    string MerchantOid,
    decimal ReturnAmount,
    string? ReferenceId);

public sealed record PayTRRefundResult(
    string Status,
    string? ReturnRefId,
    string? ErrorMessage,
    string RawJson);

public sealed record PayTRStatusResult(
    string Status,
    string? PaymentStatus,
    decimal? PaymentTotal,
    string? PaymentDate,
    string? FailReason,
    string RawJson);

public sealed record PayTRCallbackPayload(
    string MerchantOid,
    string Status,
    decimal TotalAmount,
    string Hash,
    string? PaymentType,
    string? Currency,
    string? PaymentAmount,
    string? FailedReasonCode,
    string? FailedReasonMsg);

public sealed class PayTRProviderException : Exception
{
    public string ErrorCode { get; }
    public PayTRProviderException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}

internal sealed record PayTRTokenApiResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("token")] string? Token,
    [property: JsonPropertyName("reason")] string? Reason,
    [property: JsonPropertyName("err_msg")] string? ErrorMessage);

internal sealed record PayTRRefundApiResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("return_ref_id")] string? ReturnRefId,
    [property: JsonPropertyName("err_no")] string? ErrorNumber,
    [property: JsonPropertyName("err_msg")] string? ErrorMessage);

internal sealed record PayTRStatusApiResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("payment_status")] string? PaymentStatus,
    [property: JsonPropertyName("payment_total")] decimal? PaymentTotal,
    [property: JsonPropertyName("payment_date")] string? PaymentDate,
    [property: JsonPropertyName("fail_reason")] string? FailReason,
    [property: JsonPropertyName("err_msg")] string? ErrorMessage);

internal sealed record PayTRTokenizeApiResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("user_token")] string? UserToken,
    [property: JsonPropertyName("card_token")] string? CardToken,
    [property: JsonPropertyName("last_4")] string? Last4,
    [property: JsonPropertyName("card_brand")] string? Brand,
    [property: JsonPropertyName("err_no")] string? ErrorNumber,
    [property: JsonPropertyName("err_msg")] string? ErrorMessage);
