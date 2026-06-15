using CoreAlign.Application.Common;
using CoreAlign.Application.Customers.DTOs;
using MediatR;

namespace CoreAlign.Application.CustomerPortal.Addresses;

public record ListPortalAddressesQuery() : IRequest<IReadOnlyList<CustomerAddressDto>>;

public record CreatePortalAddressCommand(
    string Label,
    string Line1,
    string? Line2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    bool IsPrimary
) : IRequest<CustomerAddressDto>, ITransactionalRequest;

public record UpdatePortalAddressCommand(
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

public record DeletePortalAddressCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;
