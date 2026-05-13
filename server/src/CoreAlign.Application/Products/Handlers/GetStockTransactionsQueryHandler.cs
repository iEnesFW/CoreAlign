using CoreAlign.Application.Common;
using CoreAlign.Application.Products.DTOs;
using CoreAlign.Application.Products.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Products.Handlers;

public class GetStockTransactionsQueryHandler : IRequestHandler<GetStockTransactionsQuery, PagedResult<StockTransactionDto>>
{
    private readonly IStockTransactionRepository _repository;

    public GetStockTransactionsQueryHandler(IStockTransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<StockTransactionDto>> Handle(GetStockTransactionsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var (items, total) = await _repository.GetByProductAsync(request.Id, page, pageSize, cancellationToken);
        var dtos = items.Select(t => new StockTransactionDto
        {
            Id = t.Id,
            ProductId = t.ProductId,
            OccurredAtUtc = t.OccurredAtUtc,
            Type = t.Type.ToString(),
            Quantity = t.Quantity,
            BalanceAfter = t.BalanceAfter,
            OrderId = t.OrderId,
            Reference = t.Reference,
            Notes = t.Notes
        }).ToList();

        return new PagedResult<StockTransactionDto>
        {
            Items = dtos,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
