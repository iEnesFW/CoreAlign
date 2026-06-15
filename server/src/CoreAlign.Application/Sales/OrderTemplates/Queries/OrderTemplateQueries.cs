using CoreAlign.Application.Common;
using CoreAlign.Application.Sales.OrderTemplates.DTOs;
using MediatR;

namespace CoreAlign.Application.Sales.OrderTemplates.Queries;

public record GetOrderTemplatesQuery(
    Guid? CustomerId = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<OrderTemplateDto>>;

public record GetOrderTemplateByIdQuery(Guid Id) : IRequest<OrderTemplateDto?>;
