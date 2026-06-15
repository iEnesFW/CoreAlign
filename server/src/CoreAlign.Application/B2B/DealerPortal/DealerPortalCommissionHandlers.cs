using CoreAlign.Application.Common;
using CoreAlign.Application.Documents;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.B2B.DealerPortal;

public class ListDealerCommissionsHandler : IRequestHandler<ListDealerCommissionsQuery, PagedResult<DealerCommissionEntryDto>>
{
    private readonly IPortalScopeService _scope;
    private readonly IDealerCommissionLedgerRepository _ledger;

    public ListDealerCommissionsHandler(IPortalScopeService scope, IDealerCommissionLedgerRepository ledger)
    {
        _scope = scope;
        _ledger = ledger;
    }

    public async Task<PagedResult<DealerCommissionEntryDto>> Handle(ListDealerCommissionsQuery request, CancellationToken cancellationToken)
    {
        var dealerId = await _scope.GetCurrentDealerAccountIdAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, total) = await _ledger.SearchAsync(
            dealerId,
            request.Status,
            request.FromUtc,
            request.ToUtc,
            page,
            pageSize,
            cancellationToken);

        return new PagedResult<DealerCommissionEntryDto>
        {
            Items = items.Select(e => new DealerCommissionEntryDto(
                Id: e.Id,
                OrderId: e.OrderId,
                ShipmentId: e.ShipmentId,
                CustomerId: e.CustomerId,
                Currency: e.Currency,
                OrderTotal: e.OrderTotal,
                CommissionPercent: e.CommissionPercent,
                CommissionAmount: e.CommissionAmount,
                Status: e.Status,
                AccruedAtUtc: e.AccruedAtUtc,
                PaidOutAtUtc: e.PaidOutAtUtc,
                Notes: e.Notes)).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}

public class GetDealerCommissionSummaryHandler : IRequestHandler<GetDealerCommissionSummaryQuery, DealerCommissionSummaryDto>
{
    private readonly IPortalScopeService _scope;
    private readonly IDealerCommissionLedgerRepository _ledger;

    public GetDealerCommissionSummaryHandler(IPortalScopeService scope, IDealerCommissionLedgerRepository ledger)
    {
        _scope = scope;
        _ledger = ledger;
    }

    public async Task<DealerCommissionSummaryDto> Handle(GetDealerCommissionSummaryQuery request, CancellationToken cancellationToken)
    {
        var dealerId = await _scope.GetCurrentDealerAccountIdAsync(cancellationToken);
        var summary = await _ledger.GetSummaryAsync(dealerId, DateTime.UtcNow, cancellationToken);
        return new DealerCommissionSummaryDto(
            YtdAccrued: summary.YtdAccrued,
            YtdPaid: summary.YtdPaid,
            ThisMonthAccrued: summary.ThisMonthAccrued,
            ThisMonthPaid: summary.ThisMonthPaid,
            LifetimeAccrued: summary.LifetimeAccrued,
            LifetimePaid: summary.LifetimePaid,
            Currency: summary.Currency);
    }
}

public class DownloadDealerCommissionStatementHandler : IRequestHandler<DownloadDealerCommissionStatementQuery, DocumentResult>
{
    private readonly IPortalScopeService _scope;
    private readonly IDocumentService _documents;

    public DownloadDealerCommissionStatementHandler(IPortalScopeService scope, IDocumentService documents)
    {
        _scope = scope;
        _documents = documents;
    }

    public async Task<DocumentResult> Handle(DownloadDealerCommissionStatementQuery request, CancellationToken cancellationToken)
    {
        var dealerId = await _scope.GetCurrentDealerAccountIdAsync(cancellationToken);
        if (request.ToUtc < request.FromUtc)
        {
            throw new ArgumentException("Date range is invalid.", nameof(request));
        }
        return await _documents.RenderDealerCommissionStatementPdfAsync(dealerId, request.FromUtc, request.ToUtc, cancellationToken);
    }
}
