using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Invoices.Commands;

public record GenerateInvoiceFromOrderCommand(Guid OrderId, int DueDays = 30, string? Notes = null)
    : IRequest<InvoiceDto>, ITransactionalRequest;

public record MarkInvoiceAsPaidCommand(Guid Id) : IRequest<InvoiceDto>, ITransactionalRequest;

public record RecordInvoicePaymentCommand(
    Guid Id,
    decimal Amount,
    PaymentMethod Method = PaymentMethod.BankTransfer,
    DateTime? PaymentDate = null,
    string? ReferenceNumber = null,
    string? Notes = null) : IRequest<InvoiceDto>, ITransactionalRequest;

public record CancelInvoiceCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;

public record StandaloneInvoiceLineInput(
    Guid? ProductId,
    string ProductSku,
    string ProductName,
    string? Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRatePercent = 0m,
    decimal? LineDiscountPercent = null,
    decimal? LineDiscountAmount = null,
    Guid? TaxRateId = null,
    bool IsTaxInclusive = false,
    decimal? WithholdingRatePercent = null,
    Guid? UomId = null,
    string? UomCode = null);

public record CreateStandaloneInvoiceCommand(
    Guid CustomerId,
    DateTime IssueDate,
    string Currency,
    IReadOnlyList<StandaloneInvoiceLineInput> Lines,
    int DueDays = 30,
    Guid? PaymentTermsId = null,
    Guid? BillingAddressId = null,
    Guid? ShippingAddressId = null,
    decimal? ExchangeRate = null,
    decimal? HeaderDiscountPercent = null,
    decimal? HeaderDiscountAmount = null,
    decimal? ShippingCost = null,
    decimal? RoundingAdjustment = null,
    string? InternalNotes = null,
    string? PublicNotes = null,
    string? TermsAndConditions = null,
    string? Notes = null) : IRequest<InvoiceDto>, ITransactionalRequest;
