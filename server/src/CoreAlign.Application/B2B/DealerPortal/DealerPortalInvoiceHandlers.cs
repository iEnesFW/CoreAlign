using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Application.Invoices.Handlers;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.B2B.DealerPortal;

public class ListDealerPortalInvoicesHandler : IRequestHandler<ListDealerPortalInvoicesQuery, PagedResult<InvoiceSummaryDto>>
{
    private readonly IPortalScopeService _scope;
    private readonly IInvoiceRepository _invoices;

    public ListDealerPortalInvoicesHandler(IPortalScopeService scope, IInvoiceRepository invoices)
    {
        _scope = scope;
        _invoices = invoices;
    }

    public async Task<PagedResult<InvoiceSummaryDto>> Handle(ListDealerPortalInvoicesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var allowed = await _scope.GetDealerAllowedCustomerIdsAsync(cancellationToken);
        if (allowed.Count == 0)
        {
            return new PagedResult<InvoiceSummaryDto>
            {
                Items = Array.Empty<InvoiceSummaryDto>(),
                Total = 0,
                Page = page,
                PageSize = pageSize,
            };
        }

        IReadOnlyCollection<Guid> targetCustomers;
        if (request.CustomerId is Guid requestedCustomerId)
        {
            if (!allowed.Contains(requestedCustomerId))
            {
                throw new DealerCustomerNotAuthorizedException();
            }
            targetCustomers = new[] { requestedCustomerId };
        }
        else
        {
            targetCustomers = allowed;
        }

        InvoiceStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<InvoiceStatus>(request.Status, ignoreCase: true, out var parsedStatus))
        {
            statusFilter = parsedStatus;
        }

        var (items, total) = await _invoices.SearchForCustomersAsync(
            targetCustomers,
            statusFilter,
            request.FromUtc,
            request.ToUtc,
            page,
            pageSize,
            cancellationToken);

        return new PagedResult<InvoiceSummaryDto>
        {
            Items = items.Select(InvoiceMapper.ToSummaryDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}

public class GetDealerPortalInvoiceByIdHandler : IRequestHandler<GetDealerPortalInvoiceByIdQuery, InvoiceDto>
{
    private readonly IPortalScopeService _scope;
    private readonly IInvoiceRepository _invoices;

    public GetDealerPortalInvoiceByIdHandler(IPortalScopeService scope, IInvoiceRepository invoices)
    {
        _scope = scope;
        _invoices = invoices;
    }

    public async Task<InvoiceDto> Handle(GetDealerPortalInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var allowed = await _scope.GetDealerAllowedCustomerIdsAsync(cancellationToken);
        var invoice = await _invoices.GetWithLinesAsync(request.InvoiceId, cancellationToken);
        if (invoice is null || !allowed.Contains(invoice.CustomerId))
        {
            throw new InvoiceNotFoundException();
        }
        return InvoiceMapper.ToDto(invoice);
    }
}
