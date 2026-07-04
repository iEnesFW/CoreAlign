using System.Text.Json.Serialization;

namespace CoreAlign.Infrastructure.Providers.EFatura.Nilvera;

public sealed record NilveraCredentials(
    string ClientId,
    string ClientSecret,
    string? Username,
    string? Password,
    string WebhookSecret,
    bool IsSandbox);

public sealed record NilveraOAuthTokenRequest(
    [property: JsonPropertyName("grant_type")] string GrantType,
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("client_secret")] string ClientSecret,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("password")] string? Password,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken);

public sealed record NilveraOAuthToken(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("token_type")] string? TokenType);

public sealed record NilveraInvoiceRequest(
    [property: JsonPropertyName("ublXmlBase64")] string UblXmlBase64,
    [property: JsonPropertyName("customerVkn")] string CustomerVkn,
    [property: JsonPropertyName("customerTaxOffice")] string? CustomerTaxOffice,
    [property: JsonPropertyName("invoiceType")] string InvoiceType,
    [property: JsonPropertyName("currency")] string Currency);

public sealed record NilveraInvoiceResult(
    [property: JsonPropertyName("uuid")] string Uuid,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("gibStatus")] string? GibStatus,
    [property: JsonPropertyName("sentAt")] DateTime SentAt);

public sealed record NilveraStatusResult(
    [property: JsonPropertyName("uuid")] string Uuid,
    [property: JsonPropertyName("currentStatus")] string CurrentStatus,
    [property: JsonPropertyName("gibResponseCode")] string? GibResponseCode,
    [property: JsonPropertyName("deliveredAt")] DateTime? DeliveredAt);

public sealed record NilveraCancelRequest(
    [property: JsonPropertyName("reason")] string Reason);

public sealed record NilveraCancelResult(
    [property: JsonPropertyName("uuid")] string Uuid,
    [property: JsonPropertyName("cancelled")] bool Cancelled,
    [property: JsonPropertyName("cancelledAt")] DateTime? CancelledAt);

public sealed record NilveraCreditNoteRequest(
    [property: JsonPropertyName("originalUuid")] string OriginalUuid,
    [property: JsonPropertyName("refundAmount")] decimal RefundAmount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("reason")] string? Reason);

public sealed record NilveraTaxpayerResult(
    [property: JsonPropertyName("taxNumber")] string TaxNumber,
    [property: JsonPropertyName("isRegistered")] bool IsRegistered,
    [property: JsonPropertyName("alias")] string? Alias,
    [property: JsonPropertyName("title")] string? Title);

public sealed record NilveraCreditNoteResult(
    [property: JsonPropertyName("uuid")] string Uuid,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("issuedAt")] DateTime IssuedAt);

public sealed record NilveraIncomingInvoice(
    [property: JsonPropertyName("uuid")] string Uuid,
    [property: JsonPropertyName("senderVkn")] string SenderVkn,
    [property: JsonPropertyName("documentNumber")] string DocumentNumber,
    [property: JsonPropertyName("issueDate")] DateTime IssueDate,
    [property: JsonPropertyName("status")] string Status);

public sealed record NilveraIncomingListResult(
    [property: JsonPropertyName("items")] IReadOnlyList<NilveraIncomingInvoice> Items);

public sealed record NilveraErrorResponse(
    [property: JsonPropertyName("errorCode")] string? ErrorCode,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("traceId")] string? TraceId);
