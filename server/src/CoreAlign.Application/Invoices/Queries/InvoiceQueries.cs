using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.DTOs;
using MediatR;

namespace CoreAlign.Application.Invoices.Queries;

public record GetInvoicesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CustomerId = null,
    string? StatusBucket = null,
    bool DueSoonOnly = false)
    : IRequest<PagedResult<InvoiceSummaryDto>>;

public record GetInvoiceAggregatesQuery(string? Search = null, Guid? CustomerId = null)
    : IRequest<InvoiceAggregatesDto>;

public record GetInvoiceByIdQuery(Guid Id) : IRequest<InvoiceDto>;

public record GetInvoicesByOrderQuery(Guid OrderId) : IRequest<List<InvoiceSummaryDto>>;

public record GetCreditNotesForInvoiceQuery(Guid InvoiceId) : IRequest<List<InvoiceSummaryDto>>;

public record GetCreditedQuantitiesByLineQuery(Guid InvoiceId) : IRequest<List<CreditedLineQuantityDto>>;
