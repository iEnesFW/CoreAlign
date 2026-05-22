using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Application.Invoices.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Invoices.Handlers;

public class GetCreditNotesForInvoiceQueryHandler
    : IRequestHandler<GetCreditNotesForInvoiceQuery, List<InvoiceSummaryDto>>
{
    private readonly IInvoiceRepository _invoices;

    public GetCreditNotesForInvoiceQueryHandler(IInvoiceRepository invoices)
    {
        _invoices = invoices;
    }

    public async Task<List<InvoiceSummaryDto>> Handle(
        GetCreditNotesForInvoiceQuery request,
        CancellationToken cancellationToken)
    {
        var notes = await _invoices.GetCreditNotesForInvoiceAsync(request.InvoiceId, cancellationToken);
        return notes.Select(InvoiceMapper.ToSummaryDto).ToList();
    }
}
