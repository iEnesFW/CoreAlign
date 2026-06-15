using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.CustomerPortal.Payments;

public record InitiateInvoicePaymentCommand(
    Guid InvoiceId,
    PortalBillingInfoInput? BillingInfo,
    string? BuyerIpAddress,
    string? GatewayName = null) : IRequest<InitiateInvoicePaymentResult>, ITransactionalRequest;

public record PortalBillingInfoInput(
    string Name,
    string Surname,
    string Email,
    string GsmNumber,
    string IdentityNumber,
    string Address,
    string City,
    string Country,
    string ZipCode);

public record InitiateInvoicePaymentResult(
    Guid PaymentSessionId,
    string GatewayName,
    string IntentId,
    string? RedirectUrl,
    decimal Amount,
    string Currency,
    string InvoiceNumber);

public record InvoicePaymentSessionDto(
    Guid Id,
    Guid InvoiceId,
    string GatewayName,
    string IntentId,
    decimal Amount,
    string Currency,
    string Status,
    string? RedirectUrl,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);
