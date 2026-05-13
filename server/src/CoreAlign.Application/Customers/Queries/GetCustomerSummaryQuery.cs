using CoreAlign.Application.Common;
using CoreAlign.Application.Customers.DTOs;
using MediatR;

namespace CoreAlign.Application.Customers.Queries;

public record GetCustomerSummaryQuery(Guid Id) : IRequest<CustomerSummaryDto>;
