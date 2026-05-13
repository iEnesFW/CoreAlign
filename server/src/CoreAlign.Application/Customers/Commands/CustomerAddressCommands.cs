using CoreAlign.Application.Common;
using CoreAlign.Application.Customers.DTOs;
using MediatR;

namespace CoreAlign.Application.Customers.Commands;

public record CreateCustomerAddressCommand(
    Guid CustomerId,
    string Label,
    string Line1,
    string? Line2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    bool IsPrimary
) : IRequest<CustomerAddressDto>, ITransactionalRequest;

public record UpdateCustomerAddressCommand(
    Guid CustomerId,
    Guid Id,
    string Label,
    string Line1,
    string? Line2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    bool IsPrimary
) : IRequest<CustomerAddressDto>, ITransactionalRequest;

public record DeleteCustomerAddressCommand(
    Guid CustomerId,
    Guid Id
) : IRequest<bool>, ITransactionalRequest;
