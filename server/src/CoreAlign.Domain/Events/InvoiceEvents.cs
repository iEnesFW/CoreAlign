using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Events;

// WHY the GL breakdown travels on the event: domain events are dispatched INSIDE SaveChanges,
// before the invoice row exists, so a subscriber that re-reads the invoice from the database gets
// null and silently books nothing.
public record InvoiceIssuedEvent(
    Guid TenantId,
    Guid InvoiceId,
    Guid CustomerId,
    Guid? OrderId,
    string InvoiceNumber,
    InvoiceType Type,
    decimal Amount,
    string Currency,
    DateTime OccurredAtUtc,
    decimal ExchangeRate = 1m,
    decimal TaxableTotal = 0m,
    decimal TaxTotal = 0m,
    decimal WithholdingTotal = 0m,
    decimal ShippingCost = 0m,
    decimal RoundingAdjustment = 0m) : IDomainEvent;

public record InvoicePaidEvent(
    Guid TenantId,
    Guid InvoiceId,
    Guid CustomerId,
    string InvoiceNumber,
    decimal Amount,
    string Currency,
    DateTime OccurredAtUtc) : IDomainEvent;

public record InvoicePartiallyPaidEvent(
    Guid TenantId,
    Guid InvoiceId,
    Guid CustomerId,
    string InvoiceNumber,
    decimal AmountApplied,
    decimal Remaining,
    string Currency,
    DateTime OccurredAtUtc) : IDomainEvent;

public record InvoiceVoidedEvent(
    Guid TenantId,
    Guid InvoiceId,
    Guid CustomerId,
    string InvoiceNumber,
    decimal Amount,
    string Currency,
    string? Reason,
    DateTime OccurredAtUtc,
    decimal ExchangeRate = 1m) : IDomainEvent;

public record InvoiceCancelledEvent(
    Guid TenantId,
    Guid InvoiceId,
    Guid CustomerId,
    string InvoiceNumber,
    decimal Amount,
    string Currency,
    bool WasIssued,
    DateTime OccurredAtUtc,
    decimal ExchangeRate = 1m) : IDomainEvent;

public record InvoiceWrittenOffEvent(
    Guid TenantId,
    Guid InvoiceId,
    Guid CustomerId,
    string InvoiceNumber,
    decimal Amount,
    string Currency,
    string? Reason,
    DateTime OccurredAtUtc,
    decimal ExchangeRate = 1m) : IDomainEvent;

public record PaymentConfirmedEvent(
    Guid TenantId,
    Guid PaymentId,
    Guid CustomerId,
    string PaymentNumber,
    PaymentDirection Direction,
    decimal Amount,
    string Currency,
    DateTime OccurredAtUtc,
    decimal ExchangeRate = 1m) : IDomainEvent;

public record PaymentAppliedEvent(
    Guid TenantId,
    Guid PaymentId,
    Guid InvoiceId,
    Guid CustomerId,
    decimal AppliedAmount,
    DateTime OccurredAtUtc) : IDomainEvent;

public record PaymentVoidedEvent(
    Guid TenantId,
    Guid PaymentId,
    Guid CustomerId,
    string PaymentNumber,
    decimal Amount,
    string Currency,
    DateTime OccurredAtUtc,
    decimal ExchangeRate = 1m) : IDomainEvent;
