using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Application.Invoices.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Invoices.Handlers;

public class GetInvoicesQueryHandler : IRequestHandler<GetInvoicesQuery, PagedResult<InvoiceSummaryDto>>
{
    private readonly IInvoiceRepository _invoiceRepository;

    public GetInvoicesQueryHandler(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<PagedResult<InvoiceSummaryDto>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, total) = await _invoiceRepository.SearchAsync(
            request.Search,
            request.CustomerId,
            page,
            pageSize,
            request.StatusBucket,
            request.DueSoonOnly,
            DateTime.UtcNow,
            cancellationToken);
        var dtos = items.Select(InvoiceMapper.ToSummaryDto).ToList();

        return new PagedResult<InvoiceSummaryDto>
        {
            Items = dtos,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
