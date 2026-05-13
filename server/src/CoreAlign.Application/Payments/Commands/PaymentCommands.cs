using CoreAlign.Application.Common;
using CoreAlign.Application.Payments.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Payments.Commands;

public record CreatePaymentCommand(
    Guid CustomerId,
    DateTime PaymentDate,
    PaymentMethod Method,
    decimal Amount,
    string Currency = "TRY",
    PaymentDirection Direction = PaymentDirection.CustomerReceipt,
    decimal ExchangeRate = 1m,
    string? BankAccountInfo = null,
    string? ReferenceNumber = null,
    string? CheckNumber = null,
    DateTime? CheckDueDate = null,
    string? Notes = null,
    bool AutoConfirm = true,
    List<PaymentApplyLine>? Applications = null
) : IRequest<PaymentDto>, ITransactionalRequest;

public record PaymentApplyLine(Guid InvoiceId, decimal AppliedAmount);

public record UpdatePaymentCommand(
    Guid Id,
    DateTime PaymentDate,
    DateTime PostingDate,
    PaymentMethod Method,
    decimal Amount,
    decimal ExchangeRate,
    string? BankAccountInfo,
    string? ReferenceNumber,
    string? CheckNumber,
    DateTime? CheckDueDate,
    string? Notes
) : IRequest<PaymentDto>, ITransactionalRequest;

public record ConfirmPaymentCommand(Guid Id, Guid? PostedByUserId = null)
    : IRequest<PaymentDto>, ITransactionalRequest;

public record ApplyPaymentCommand(
    Guid Id,
    List<PaymentApplyLine> Applications
) : IRequest<PaymentDto>, ITransactionalRequest;

public record UnapplyPaymentCommand(Guid Id, Guid ApplicationId)
    : IRequest<PaymentDto>, ITransactionalRequest;

public record VoidPaymentCommand(Guid Id, string? Reason)
    : IRequest<PaymentDto>, ITransactionalRequest;
