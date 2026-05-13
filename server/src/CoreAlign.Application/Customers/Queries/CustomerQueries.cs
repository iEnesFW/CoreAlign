using CoreAlign.Application.Common;
using CoreAlign.Application.Customers.DTOs;
using MediatR;

namespace CoreAlign.Application.Customers.Queries;

public record GetCustomersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null
) : IRequest<PagedResult<CustomerDto>>;

public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto>;
