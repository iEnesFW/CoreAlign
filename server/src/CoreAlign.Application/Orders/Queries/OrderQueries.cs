using CoreAlign.Application.Common;
using CoreAlign.Application.Orders.DTOs;
using MediatR;

namespace CoreAlign.Application.Orders.Queries;

public record GetOrdersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CustomerId = null
) : IRequest<PagedResult<OrderSummaryDto>>;

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto>;
