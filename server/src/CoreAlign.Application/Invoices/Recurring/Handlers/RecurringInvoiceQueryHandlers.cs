using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.Recurring.DTOs;
using CoreAlign.Application.Invoices.Recurring.Queries;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Invoices.Recurring.Handlers;

public class GetRecurringInvoiceTemplatesQueryHandler
    : IRequestHandler<GetRecurringInvoiceTemplatesQuery, PagedResult<RecurringInvoiceTemplateSummaryDto>>
{
    private readonly IRecurringInvoiceTemplateRepository _repository;
    private readonly ICustomerRepository _customers;

    public GetRecurringInvoiceTemplatesQueryHandler(
        IRecurringInvoiceTemplateRepository repository,
        ICustomerRepository customers)
    {
        _repository = repository;
        _customers = customers;
    }

    public async Task<PagedResult<RecurringInvoiceTemplateSummaryDto>> Handle(
        GetRecurringInvoiceTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var (items, total) = await _repository.SearchAsync(
            request.Search, request.CustomerId, request.Status, page, pageSize, cancellationToken);

        var customerIds = items.Select(t => t.CustomerId).Distinct().ToList();
        var customers = await _customers.GetByIdsAsync(customerIds, cancellationToken);

        return new PagedResult<RecurringInvoiceTemplateSummaryDto>
        {
            Items = items.Select(t => new RecurringInvoiceTemplateSummaryDto
            {
                Id = t.Id,
                Name = t.Name,
                CustomerId = t.CustomerId,
                CustomerName = customers.TryGetValue(t.CustomerId, out var c) ? c.Name : string.Empty,
                Currency = t.Currency,
                Frequency = t.Frequency,
                IntervalCount = t.IntervalCount,
                NextRunDate = t.NextRunDate,
                OccurrencesGenerated = t.OccurrencesGenerated,
                Status = t.Status,
                LineCount = t.Lines.Count,
                CreatedAtUtc = t.CreatedAtUtc
            }).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}

public class GetRecurringInvoiceTemplateByIdQueryHandler
    : IRequestHandler<GetRecurringInvoiceTemplateByIdQuery, RecurringInvoiceTemplateDto>
{
    private readonly IRecurringInvoiceTemplateRepository _repository;

    public GetRecurringInvoiceTemplateByIdQueryHandler(IRecurringInvoiceTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<RecurringInvoiceTemplateDto> Handle(
        GetRecurringInvoiceTemplateByIdQuery request,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetWithLinesAsync(request.Id, cancellationToken)
            ?? throw new RecurringInvoiceTemplateNotFoundException();
        return RecurringInvoiceMapper.ToDto(template);
    }
}
