using CoreAlign.Application.Customers.DTOs;
using MediatR;

namespace CoreAlign.Application.Customers.Queries;

public record GetCustomerAnalyticsQuery(Guid Id, int MonthsBack = 12) : IRequest<CustomerAnalyticsDto>;
