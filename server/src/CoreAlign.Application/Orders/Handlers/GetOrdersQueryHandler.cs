using CoreAlign.Application.Common;
using CoreAlign.Application.Orders.DTOs;
using CoreAlign.Application.Orders.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Orders.Handlers;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, PagedResult<OrderSummaryDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IFiscalYearResolver _fiscalYear;

    public GetOrdersQueryHandler(IOrderRepository orderRepository, IFiscalYearResolver fiscalYear)
    {
        _orderRepository = orderRepository;
        _fiscalYear = fiscalYear;
    }

    public async Task<PagedResult<OrderSummaryDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var window = await _fiscalYear.ResolveAsync(request.FiscalYear, cancellationToken);

        var (items, total) = await _orderRepository.SearchAsync(
            request.Search,
            request.CustomerId,
            page,
            pageSize,
            window?.StartUtc,
            window?.EndExclusiveUtc,
            cancellationToken: cancellationToken);

        var dtos = items.Select(OrderMapper.ToSummaryDto).ToList();

        return new PagedResult<OrderSummaryDto>
        {
            Items = dtos,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
