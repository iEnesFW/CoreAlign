using CoreAlign.Application.B2B;
using CoreAlign.Application.Common;
using CoreAlign.Application.Customers.DTOs;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.CustomerPortal.Addresses;

internal static class PortalAddressMapper
{
    public static CustomerAddressDto ToDto(CustomerAddress a) => new()
    {
        Id = a.Id,
        CustomerId = a.CustomerId,
        Label = a.Label,
        Line1 = a.Line1,
        Line2 = a.Line2,
        City = a.City,
        State = a.State,
        PostalCode = a.PostalCode,
        Country = a.Country,
        IsPrimary = a.IsPrimary,
        CreatedAtUtc = a.CreatedAtUtc,
        UpdatedAtUtc = a.UpdatedAtUtc
    };
}

public class ListPortalAddressesHandler : IRequestHandler<ListPortalAddressesQuery, IReadOnlyList<CustomerAddressDto>>
{
    private readonly IPortalScopeService _scope;
    private readonly ICustomerAddressRepository _addresses;

    public ListPortalAddressesHandler(IPortalScopeService scope, ICustomerAddressRepository addresses)
    {
        _scope = scope;
        _addresses = addresses;
    }

    public async Task<IReadOnlyList<CustomerAddressDto>> Handle(ListPortalAddressesQuery request, CancellationToken cancellationToken)
    {
        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var items = await _addresses.GetByCustomerAsync(customerId, cancellationToken);
        return items.Select(PortalAddressMapper.ToDto).ToList();
    }
}

public class CreatePortalAddressHandler : IRequestHandler<CreatePortalAddressCommand, CustomerAddressDto>
{
    private readonly IPortalScopeService _scope;
    private readonly ICustomerAddressRepository _addresses;
    private readonly IUnitOfWork _uow;

    public CreatePortalAddressHandler(IPortalScopeService scope, ICustomerAddressRepository addresses, IUnitOfWork uow)
    {
        _scope = scope;
        _addresses = addresses;
        _uow = uow;
    }

    public async Task<CustomerAddressDto> Handle(CreatePortalAddressCommand request, CancellationToken cancellationToken)
    {
        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);

        var address = new CustomerAddress(customerId, request.Label, request.Line1)
        {
            Line2 = request.Line2,
            City = request.City,
            State = request.State,
            PostalCode = request.PostalCode,
            Country = request.Country,
            IsPrimary = request.IsPrimary
        };

        if (request.IsPrimary)
        {
            await _addresses.ClearPrimaryAsync(customerId, null, cancellationToken);
        }

        await _addresses.AddAsync(address, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return PortalAddressMapper.ToDto(address);
    }
}

public class UpdatePortalAddressHandler : IRequestHandler<UpdatePortalAddressCommand, CustomerAddressDto>
{
    private readonly IPortalScopeService _scope;
    private readonly ICustomerAddressRepository _addresses;
    private readonly IUnitOfWork _uow;

    public UpdatePortalAddressHandler(IPortalScopeService scope, ICustomerAddressRepository addresses, IUnitOfWork uow)
    {
        _scope = scope;
        _addresses = addresses;
        _uow = uow;
    }

    public async Task<CustomerAddressDto> Handle(UpdatePortalAddressCommand request, CancellationToken cancellationToken)
    {
        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);

        var address = await _addresses.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new CustomerAddressNotFoundException();
        if (address.CustomerId != customerId)
        {
            throw new CustomerAddressNotFoundException();
        }

        if (request.IsPrimary)
        {
            await _addresses.ClearPrimaryAsync(customerId, address.Id, cancellationToken);
        }

        address.Update(
            request.Label,
            request.Line1,
            request.Line2,
            request.City,
            request.State,
            request.PostalCode,
            request.Country,
            request.IsPrimary);

        _addresses.Update(address);
        await _uow.SaveChangesAsync(cancellationToken);

        return PortalAddressMapper.ToDto(address);
    }
}

public class DeletePortalAddressHandler : IRequestHandler<DeletePortalAddressCommand, bool>
{
    private readonly IPortalScopeService _scope;
    private readonly ICustomerAddressRepository _addresses;
    private readonly IUnitOfWork _uow;

    public DeletePortalAddressHandler(IPortalScopeService scope, ICustomerAddressRepository addresses, IUnitOfWork uow)
    {
        _scope = scope;
        _addresses = addresses;
        _uow = uow;
    }

    public async Task<bool> Handle(DeletePortalAddressCommand request, CancellationToken cancellationToken)
    {
        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);

        var address = await _addresses.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new CustomerAddressNotFoundException();
        if (address.CustomerId != customerId)
        {
            throw new CustomerAddressNotFoundException();
        }

        _addresses.Remove(address);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
