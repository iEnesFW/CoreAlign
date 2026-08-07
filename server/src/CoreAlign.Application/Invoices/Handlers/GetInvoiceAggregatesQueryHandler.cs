using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Application.Invoices.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Invoices.Handlers;

public class GetInvoiceAggregatesQueryHandler
    : IRequestHandler<GetInvoiceAggregatesQuery, InvoiceAggregatesDto>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IFiscalYearResolver _fiscalYear;

    public GetInvoiceAggregatesQueryHandler(IInvoiceRepository invoiceRepository, IFiscalYearResolver fiscalYear)
    {
        _invoiceRepository = invoiceRepository;
        _fiscalYear = fiscalYear;
    }

    public async Task<InvoiceAggregatesDto> Handle(
        GetInvoiceAggregatesQuery request,
        CancellationToken cancellationToken)
    {
        var window = await _fiscalYear.ResolveAsync(request.FiscalYear, cancellationToken);

        var aggregates = await _invoiceRepository.GetAggregatesAsync(
            request.Search,
            request.CustomerId,
            DateTime.UtcNow,
            window?.StartUtc,
            window?.EndExclusiveUtc,
            cancellationToken);

        return new InvoiceAggregatesDto
        {
            TotalCount = aggregates.TotalCount,
            OpenCount = aggregates.OpenCount,
            PartiallyPaidCount = aggregates.PartiallyPaidCount,
            OverdueCount = aggregates.OverdueCount,
            PaidCount = aggregates.PaidCount,
            CancelledCount = aggregates.CancelledCount,
            DueSoonCount = aggregates.DueSoonCount,
            OutstandingTotal = aggregates.OutstandingTotal,
            PaidTotal = aggregates.PaidTotal,
            OverdueTotal = aggregates.OverdueTotal,
        };
    }
}
