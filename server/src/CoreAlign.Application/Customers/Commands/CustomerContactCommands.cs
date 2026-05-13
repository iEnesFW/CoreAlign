using CoreAlign.Application.Common;
using CoreAlign.Application.Customers.DTOs;
using MediatR;

namespace CoreAlign.Application.Customers.Commands;

public record CreateCustomerContactCommand(
    Guid CustomerId,
    string Name,
    string? Role,
    string? Email,
    string? Phone,
    string? Notes,
    bool IsPrimary
) : IRequest<CustomerContactDto>, ITransactionalRequest;

public record UpdateCustomerContactCommand(
    Guid CustomerId,
    Guid Id,
    string Name,
    string? Role,
    string? Email,
    string? Phone,
    string? Notes,
    bool IsPrimary
) : IRequest<CustomerContactDto>, ITransactionalRequest;

public record DeleteCustomerContactCommand(
    Guid CustomerId,
    Guid Id
) : IRequest<bool>, ITransactionalRequest;
