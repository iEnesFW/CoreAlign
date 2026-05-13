using CoreAlign.Application.Common;
using CoreAlign.Application.Customers.DTOs;
using MediatR;

namespace CoreAlign.Application.Customers.Queries;

public record GetCustomerAddressesQuery(Guid CustomerId) : IRequest<List<CustomerAddressDto>>;

public record GetCustomerContactsQuery(Guid CustomerId) : IRequest<List<CustomerContactDto>>;
