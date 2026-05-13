using CoreAlign.Application.Common;
using CoreAlign.Application.Products.DTOs;
using CoreAlign.Application.Products.Mapping;
using CoreAlign.Application.Products.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Products.Handlers;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, total) = await _productRepository.SearchAsync(
            request.Search,
            request.IsActive,
            page,
            pageSize,
            cancellationToken);

        var dtos = items.Select(ProductMapper.ToDto).ToList();

        return new PagedResult<ProductDto>
        {
            Items = dtos,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
