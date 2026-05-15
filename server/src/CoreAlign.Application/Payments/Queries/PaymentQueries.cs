using CoreAlign.Application.Common;
using CoreAlign.Application.Payments.DTOs;
using MediatR;

namespace CoreAlign.Application.Payments.Queries;

public record GetPaymentByIdQuery(Guid Id) : IRequest<PaymentDto?>;

public record SearchPaymentsQuery(
    string? Search = null,
    Guid? CustomerId = null,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<PaymentSummaryDto>>;

public record GetPaymentsByCustomerQuery(Guid CustomerId) : IRequest<IReadOnlyList<PaymentSummaryDto>>;

public record GetPaymentsByInvoiceQuery(Guid InvoiceId) : IRequest<IReadOnlyList<PaymentApplicationDto>>;

public record GetCustomerLedgerQuery(
    Guid CustomerId,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<CustomerLedgerEntryDto>>;

public record GetCustomerAgingQuery(Guid CustomerId, DateTime? AsOfUtc = null) : IRequest<CustomerAgingDto>;

public record GetOpenInvoicesForCustomerQuery(Guid CustomerId) : IRequest<IReadOnlyList<Application.Invoices.DTOs.InvoiceSummaryDto>>;
