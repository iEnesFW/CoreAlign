using CoreAlign.Application.Common;
using CoreAlign.Application.Products.DTOs;
using MediatR;

namespace CoreAlign.Application.Products.Queries;

public record GetProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null
) : IRequest<PagedResult<ProductDto>>;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;
