using CoreAlign.Application.Common;
using CoreAlign.Application.Customers.DTOs;
using MediatR;

namespace CoreAlign.Application.Customers.Queries;

public record GetCustomerTransactionsQuery(Guid Id, int Page = 1, int PageSize = 50)
    : IRequest<PagedResult<CustomerTransactionDto>>;
