using CoreAlign.Application.Common;
using CoreAlign.Application.Sales.OrderTemplates.DTOs;
using CoreAlign.Application.Sales.OrderTemplates.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Sales.OrderTemplates.Handlers;

public class GetOrderTemplatesQueryHandler : IRequestHandler<GetOrderTemplatesQuery, PagedResult<OrderTemplateDto>>
{
    private readonly IOrderTemplateRepository _repository;

    public GetOrderTemplatesQueryHandler(IOrderTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<OrderTemplateDto>> Handle(GetOrderTemplatesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var (items, total) = await _repository.SearchAsync(request.CustomerId, page, pageSize, cancellationToken);

        return new PagedResult<OrderTemplateDto>
        {
            Items = items.Select(OrderTemplateMapper.ToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}

public class GetOrderTemplateByIdQueryHandler : IRequestHandler<GetOrderTemplateByIdQuery, OrderTemplateDto?>
{
    private readonly IOrderTemplateRepository _repository;

    public GetOrderTemplateByIdQueryHandler(IOrderTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<OrderTemplateDto?> Handle(GetOrderTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetWithLinesAsync(request.Id, cancellationToken);
        return template is null ? null : OrderTemplateMapper.ToDto(template);
    }
}
