using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Events;

public record InvoiceIssuedEvent(
    Guid TenantId,
    Guid InvoiceId,
    Guid CustomerId,
    Guid? OrderId,
    string InvoiceNumber,
    InvoiceType Type,
    decimal Amount,
    string Currency,
    DateTime OccurredAtUtc) : IDomainEvent;

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
    DateTime OccurredAtUtc) : IDomainEvent;

public record InvoiceCancelledEvent(
    Guid TenantId,
    Guid InvoiceId,
    Guid CustomerId,
    string InvoiceNumber,
    decimal Amount,
    string Currency,
    bool WasIssued,
    DateTime OccurredAtUtc) : IDomainEvent;

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
    DateTime OccurredAtUtc) : IDomainEvent;
