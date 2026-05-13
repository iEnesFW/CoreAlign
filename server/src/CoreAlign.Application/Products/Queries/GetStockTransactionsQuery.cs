using CoreAlign.Application.Common;
using CoreAlign.Application.Products.DTOs;
using MediatR;

namespace CoreAlign.Application.Products.Queries;

public record GetStockTransactionsQuery(Guid Id, int Page = 1, int PageSize = 50)
    : IRequest<PagedResult<StockTransactionDto>>;
