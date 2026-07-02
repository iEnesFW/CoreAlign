using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.Recurring.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Invoices.Recurring.Queries;

public record GetRecurringInvoiceTemplatesQuery(
    string? Search = null,
    Guid? CustomerId = null,
    RecurringInvoiceStatus? Status = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<RecurringInvoiceTemplateSummaryDto>>;

public record GetRecurringInvoiceTemplateByIdQuery(Guid Id) : IRequest<RecurringInvoiceTemplateDto>;
