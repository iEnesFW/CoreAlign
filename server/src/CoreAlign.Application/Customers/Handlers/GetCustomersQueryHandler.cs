using CoreAlign.Application.Common;
using CoreAlign.Application.Customers.DTOs;
using CoreAlign.Application.Customers.Mapping;
using CoreAlign.Application.Customers.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Customers.Handlers;

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, PagedResult<CustomerDto>>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomersQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<PagedResult<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, total) = await _customerRepository.SearchAsync(
            request.Search,
            request.IsActive,
            page,
            pageSize,
            cancellationToken);

        var dtos = items.Select(CustomerMapper.ToDto).ToList();

        return new PagedResult<CustomerDto>
        {
            Items = dtos,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
