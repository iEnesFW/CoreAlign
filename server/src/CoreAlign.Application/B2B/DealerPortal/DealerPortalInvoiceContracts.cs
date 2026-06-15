using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.DTOs;
using MediatR;

namespace CoreAlign.Application.B2B.DealerPortal;

public record ListDealerPortalInvoicesQuery(
    Guid? CustomerId = null,
    string? Status = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<InvoiceSummaryDto>>;

public record GetDealerPortalInvoiceByIdQuery(Guid InvoiceId) : IRequest<InvoiceDto>;
