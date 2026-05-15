using CoreAlign.Application.Customers.DTOs;
using MediatR;

namespace CoreAlign.Application.Customers.Queries;

public record GetCustomerOverviewQuery(Guid Id) : IRequest<CustomerOverviewDto>;
