using CoreAlign.Application.Products.DTOs;
using MediatR;

namespace CoreAlign.Application.Products.Queries;

public record GetProductComponentsQuery(Guid ParentProductId) : IRequest<List<ProductComponentDto>>;
