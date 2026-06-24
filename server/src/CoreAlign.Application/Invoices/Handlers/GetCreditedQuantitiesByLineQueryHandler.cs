using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Application.Invoices.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Invoices.Handlers;

public class GetCreditedQuantitiesByLineQueryHandler
    : IRequestHandler<GetCreditedQuantitiesByLineQuery, List<CreditedLineQuantityDto>>
{
    private readonly IInvoiceRepository _invoices;

    public GetCreditedQuantitiesByLineQueryHandler(IInvoiceRepository invoices)
    {
        _invoices = invoices;
    }

    public async Task<List<CreditedLineQuantityDto>> Handle(
        GetCreditedQuantitiesByLineQuery request,
        CancellationToken cancellationToken)
    {
        var notes = await _invoices.GetCreditNotesForInvoiceAsync(request.InvoiceId, cancellationToken);
        return CreditNoteCalculations.SumCreditedByOriginLine(notes)
            .Select(kv => new CreditedLineQuantityDto
            {
                InvoiceLineId = kv.Key,
                CreditedQuantity = kv.Value,
            })
            .ToList();
    }
}
