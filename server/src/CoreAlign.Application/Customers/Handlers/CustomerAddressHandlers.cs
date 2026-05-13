using CoreAlign.Application.Common;
using CoreAlign.Application.Customers.Commands;
using CoreAlign.Application.Customers.DTOs;
using CoreAlign.Application.Customers.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Customers.Handlers;

internal static class CustomerAddressMapper
{
    public static CustomerAddressDto MapToDto(CustomerAddress address) => new()
    {
        Id = address.Id,
        CustomerId = address.CustomerId,
        Label = address.Label,
        Line1 = address.Line1,
        Line2 = address.Line2,
        City = address.City,
        State = address.State,
        PostalCode = address.PostalCode,
        Country = address.Country,
        IsPrimary = address.IsPrimary,
        CreatedAtUtc = address.CreatedAtUtc,
        UpdatedAtUtc = address.UpdatedAtUtc
    };
}

public class GetCustomerAddressesQueryHandler : IRequestHandler<GetCustomerAddressesQuery, List<CustomerAddressDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerAddressRepository _addressRepository;

    public GetCustomerAddressesQueryHandler(ICustomerRepository customerRepository, ICustomerAddressRepository addressRepository)
    {
        _customerRepository = customerRepository;
        _addressRepository = addressRepository;
    }

    public async Task<List<CustomerAddressDto>> Handle(GetCustomerAddressesQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var items = await _addressRepository.GetByCustomerAsync(customer.Id, cancellationToken);
        return items.Select(CustomerAddressMapper.MapToDto).ToList();
    }
}

public class CreateCustomerAddressCommandHandler : IRequestHandler<CreateCustomerAddressCommand, CustomerAddressDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerAddressRepository _addressRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerAddressCommandHandler(
        ICustomerRepository customerRepository,
        ICustomerAddressRepository addressRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _addressRepository = addressRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerAddressDto> Handle(CreateCustomerAddressCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var address = new CustomerAddress(customer.Id, request.Label, request.Line1)
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
            await _addressRepository.ClearPrimaryAsync(customer.Id, null, cancellationToken);
        }

        await _addressRepository.AddAsync(address, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CustomerAddressMapper.MapToDto(address);
    }
}

public class UpdateCustomerAddressCommandHandler : IRequestHandler<UpdateCustomerAddressCommand, CustomerAddressDto>
{
    private readonly ICustomerAddressRepository _addressRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerAddressCommandHandler(ICustomerAddressRepository addressRepository, IUnitOfWork unitOfWork)
    {
        _addressRepository = addressRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerAddressDto> Handle(UpdateCustomerAddressCommand request, CancellationToken cancellationToken)
    {
        var address = await _addressRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new CustomerAddressNotFoundException();

        if (address.CustomerId != request.CustomerId)
        {
            throw new CustomerAddressNotFoundException();
        }

        if (request.IsPrimary)
        {
            await _addressRepository.ClearPrimaryAsync(address.CustomerId, address.Id, cancellationToken);
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

        _addressRepository.Update(address);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CustomerAddressMapper.MapToDto(address);
    }
}

public class DeleteCustomerAddressCommandHandler : IRequestHandler<DeleteCustomerAddressCommand, bool>
{
    private readonly ICustomerAddressRepository _addressRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCustomerAddressCommandHandler(ICustomerAddressRepository addressRepository, IUnitOfWork unitOfWork)
    {
        _addressRepository = addressRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteCustomerAddressCommand request, CancellationToken cancellationToken)
    {
        var address = await _addressRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new CustomerAddressNotFoundException();

        if (address.CustomerId != request.CustomerId)
        {
            throw new CustomerAddressNotFoundException();
        }

        _addressRepository.Remove(address);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
