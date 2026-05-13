using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Application.Invoices.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Invoices.Handlers;

public class GetInvoicesByOrderQueryHandler : IRequestHandler<GetInvoicesByOrderQuery, List<InvoiceSummaryDto>>
{
    private readonly IInvoiceRepository _invoiceRepository;

    public GetInvoicesByOrderQueryHandler(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<List<InvoiceSummaryDto>> Handle(GetInvoicesByOrderQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        var result = invoice is null
            ? new List<InvoiceSummaryDto>()
            : new List<InvoiceSummaryDto> { InvoiceMapper.ToSummaryDto(invoice) };

        return result;
    }
}
