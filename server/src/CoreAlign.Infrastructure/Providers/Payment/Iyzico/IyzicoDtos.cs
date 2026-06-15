using System.Text.Json.Serialization;

namespace CoreAlign.Infrastructure.Providers.Payment.Iyzico;

public sealed record IyzicoCredentials(
    [property: JsonPropertyName("apiKey")] string ApiKey,
    [property: JsonPropertyName("secretKey")] string SecretKey,
    [property: JsonPropertyName("isSandbox")] bool IsSandbox,
    [property: JsonPropertyName("webhookSecret")] string WebhookSecret);

public sealed record IyzicoBuyer(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("surname")] string Surname,
    [property: JsonPropertyName("gsmNumber")] string GsmNumber,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("identityNumber")] string IdentityNumber,
    [property: JsonPropertyName("registrationAddress")] string RegistrationAddress,
    [property: JsonPropertyName("ip")] string Ip,
    [property: JsonPropertyName("city")] string City,
    [property: JsonPropertyName("country")] string Country,
    [property: JsonPropertyName("zipCode")] string? ZipCode,
    [property: JsonPropertyName("lastLoginDate")] string? LastLoginDate,
    [property: JsonPropertyName("registrationDate")] string? RegistrationDate);

public sealed record IyzicoAddress(
    [property: JsonPropertyName("contactName")] string ContactName,
    [property: JsonPropertyName("city")] string City,
    [property: JsonPropertyName("country")] string Country,
    [property: JsonPropertyName("address")] string Address,
    [property: JsonPropertyName("zipCode")] string? ZipCode);

public sealed record IyzicoBasketItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("category1")] string Category1,
    [property: JsonPropertyName("itemType")] string ItemType,
    [property: JsonPropertyName("price")] string Price);

/// <summary>
/// PCI-DSS safe Iyzico card payload. PAN / CVC / expiry NEVER leave the
/// frontend — the cardholder browser tokenizes the card with iyzico.js and
/// the backend only sees the opaque <c>cardToken</c> (one-shot) or the
/// stored <c>cardUserKey</c> (vault reference).
/// </summary>
public sealed record IyzicoPaymentCard(
    [property: JsonPropertyName("cardUserKey")] string? CardUserKey,
    [property: JsonPropertyName("cardToken")] string? CardToken);

public sealed record IyzicoChargeRequest(
    [property: JsonPropertyName("locale")] string Locale,
    [property: JsonPropertyName("conversationId")] string ConversationId,
    [property: JsonPropertyName("price")] string Price,
    [property: JsonPropertyName("paidPrice")] string PaidPrice,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("installment")] int Installment,
    [property: JsonPropertyName("basketId")] string BasketId,
    [property: JsonPropertyName("paymentChannel")] string PaymentChannel,
    [property: JsonPropertyName("paymentGroup")] string PaymentGroup,
    [property: JsonPropertyName("paymentCard")] IyzicoPaymentCard PaymentCard,
    [property: JsonPropertyName("buyer")] IyzicoBuyer Buyer,
    [property: JsonPropertyName("shippingAddress")] IyzicoAddress ShippingAddress,
    [property: JsonPropertyName("billingAddress")] IyzicoAddress BillingAddress,
    [property: JsonPropertyName("basketItems")] IReadOnlyList<IyzicoBasketItem> BasketItems);

public sealed record IyzicoChargeResult(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("paymentId")] string? PaymentId,
    [property: JsonPropertyName("paymentTransactionId")] string? PaymentTransactionId,
    [property: JsonPropertyName("fraudStatus")] int? FraudStatus,
    [property: JsonPropertyName("conversationId")] string? ConversationId,
    [property: JsonPropertyName("price")] decimal? Price,
    [property: JsonPropertyName("paidPrice")] decimal? PaidPrice,
    [property: JsonPropertyName("currency")] string? Currency,
    [property: JsonPropertyName("errorCode")] string? ErrorCode,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage,
    [property: JsonPropertyName("errorGroup")] string? ErrorGroup);

public sealed record Iyzico3DSecureInitRequest(
    [property: JsonPropertyName("locale")] string Locale,
    [property: JsonPropertyName("conversationId")] string ConversationId,
    [property: JsonPropertyName("price")] string Price,
    [property: JsonPropertyName("paidPrice")] string PaidPrice,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("installment")] int Installment,
    [property: JsonPropertyName("basketId")] string BasketId,
    [property: JsonPropertyName("paymentChannel")] string PaymentChannel,
    [property: JsonPropertyName("paymentGroup")] string PaymentGroup,
    [property: JsonPropertyName("callbackUrl")] string CallbackUrl,
    [property: JsonPropertyName("paymentCard")] IyzicoPaymentCard PaymentCard,
    [property: JsonPropertyName("buyer")] IyzicoBuyer Buyer,
    [property: JsonPropertyName("shippingAddress")] IyzicoAddress ShippingAddress,
    [property: JsonPropertyName("billingAddress")] IyzicoAddress BillingAddress,
    [property: JsonPropertyName("basketItems")] IReadOnlyList<IyzicoBasketItem> BasketItems);

public sealed record Iyzico3DSecureInitResult(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("htmlContent")] string? HtmlContent,
    [property: JsonPropertyName("conversationId")] string? ConversationId,
    [property: JsonPropertyName("paymentId")] string? PaymentId,
    [property: JsonPropertyName("errorCode")] string? ErrorCode,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage);

public sealed record Iyzico3DSecureVerifyRequest(
    [property: JsonPropertyName("locale")] string Locale,
    [property: JsonPropertyName("conversationId")] string ConversationId,
    [property: JsonPropertyName("paymentId")] string PaymentId,
    [property: JsonPropertyName("conversationData")] string? ConversationData);

public sealed record IyzicoRefundRequest(
    [property: JsonPropertyName("locale")] string Locale,
    [property: JsonPropertyName("conversationId")] string ConversationId,
    [property: JsonPropertyName("paymentTransactionId")] string PaymentTransactionId,
    [property: JsonPropertyName("price")] string Price,
    [property: JsonPropertyName("ip")] string Ip,
    [property: JsonPropertyName("currency")] string? Currency);

public sealed record IyzicoRefundResult(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("paymentId")] string? PaymentId,
    [property: JsonPropertyName("paymentTransactionId")] string? PaymentTransactionId,
    [property: JsonPropertyName("price")] decimal? Price,
    [property: JsonPropertyName("currency")] string? Currency,
    [property: JsonPropertyName("errorCode")] string? ErrorCode,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage);

public sealed record IyzicoTransactionLookupRequest(
    [property: JsonPropertyName("locale")] string Locale,
    [property: JsonPropertyName("conversationId")] string ConversationId,
    [property: JsonPropertyName("paymentId")] string PaymentId,
    [property: JsonPropertyName("paymentConversationId")] string? PaymentConversationId);

public sealed record IyzicoTransactionLookupResult(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("paymentId")] string? PaymentId,
    [property: JsonPropertyName("paymentStatus")] string? PaymentStatus,
    [property: JsonPropertyName("fraudStatus")] int? FraudStatus,
    [property: JsonPropertyName("price")] decimal? Price,
    [property: JsonPropertyName("paidPrice")] decimal? PaidPrice,
    [property: JsonPropertyName("currency")] string? Currency,
    [property: JsonPropertyName("errorCode")] string? ErrorCode,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage);

/// <summary>
/// Vault-storage request. The caller supplies an ephemeral
/// <c>cardToken</c> obtained client-side from iyzico.js; the backend never
/// receives a raw PAN. The returned <c>cardUserKey</c> + <c>cardToken</c>
/// pair are persisted in place of card data.
/// </summary>
public sealed record IyzicoTokenizeRequest(
    [property: JsonPropertyName("locale")] string Locale,
    [property: JsonPropertyName("conversationId")] string ConversationId,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("externalId")] string? ExternalId,
    [property: JsonPropertyName("cardUserKey")] string? CardUserKey,
    [property: JsonPropertyName("card")] IyzicoVaultCardReference Card);

/// <summary>
/// Vault card reference for Iyzico card storage. Only the client-side
/// alias and the ephemeral token coming back from iyzico.js are sent.
/// </summary>
public sealed record IyzicoVaultCardReference(
    [property: JsonPropertyName("cardAlias")] string CardAlias,
    [property: JsonPropertyName("cardToken")] string CardToken);

public sealed record IyzicoTokenizeResult(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("cardToken")] string? CardToken,
    [property: JsonPropertyName("cardUserKey")] string? CardUserKey,
    [property: JsonPropertyName("binNumber")] string? BinNumber,
    [property: JsonPropertyName("lastFourDigits")] string? LastFourDigits,
    [property: JsonPropertyName("cardType")] string? CardType,
    [property: JsonPropertyName("cardAssociation")] string? CardAssociation,
    [property: JsonPropertyName("cardFamily")] string? CardFamily,
    [property: JsonPropertyName("errorCode")] string? ErrorCode,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage);

public sealed record IyzicoErrorResponse(
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("errorCode")] string? ErrorCode,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage,
    [property: JsonPropertyName("errorGroup")] string? ErrorGroup,
    [property: JsonPropertyName("conversationId")] string? ConversationId);

public sealed class IyzicoProviderException : Exception
{
    public string ErrorCode { get; }

    public IyzicoProviderException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    public IyzicoProviderException(string errorCode, string message, Exception inner) : base(message, inner)
    {
        ErrorCode = errorCode;
    }
}
