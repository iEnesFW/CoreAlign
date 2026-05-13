using CoreAlign.Application.Common;
using CoreAlign.Application.Customers.Commands;
using CoreAlign.Application.Customers.DTOs;
using CoreAlign.Application.Customers.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Customers.Handlers;

internal static class CustomerContactMapper
{
    public static CustomerContactDto MapToDto(CustomerContact contact) => new()
    {
        Id = contact.Id,
        CustomerId = contact.CustomerId,
        Name = contact.Name,
        Role = contact.Role,
        Email = contact.Email,
        Phone = contact.Phone,
        Notes = contact.Notes,
        IsPrimary = contact.IsPrimary,
        CreatedAtUtc = contact.CreatedAtUtc,
        UpdatedAtUtc = contact.UpdatedAtUtc
    };
}

public class GetCustomerContactsQueryHandler : IRequestHandler<GetCustomerContactsQuery, List<CustomerContactDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerContactRepository _contactRepository;

    public GetCustomerContactsQueryHandler(ICustomerRepository customerRepository, ICustomerContactRepository contactRepository)
    {
        _customerRepository = customerRepository;
        _contactRepository = contactRepository;
    }

    public async Task<List<CustomerContactDto>> Handle(GetCustomerContactsQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var items = await _contactRepository.GetByCustomerAsync(customer.Id, cancellationToken);
        return items.Select(CustomerContactMapper.MapToDto).ToList();
    }
}

public class CreateCustomerContactCommandHandler : IRequestHandler<CreateCustomerContactCommand, CustomerContactDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerContactCommandHandler(
        ICustomerRepository customerRepository,
        ICustomerContactRepository contactRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerContactDto> Handle(CreateCustomerContactCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var contact = new CustomerContact(customer.Id, request.Name)
        {
            Role = request.Role,
            Email = request.Email,
            Phone = request.Phone,
            Notes = request.Notes,
            IsPrimary = request.IsPrimary
        };

        if (request.IsPrimary)
        {
            await _contactRepository.ClearPrimaryAsync(customer.Id, null, cancellationToken);
        }

        await _contactRepository.AddAsync(contact, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CustomerContactMapper.MapToDto(contact);
    }
}

public class UpdateCustomerContactCommandHandler : IRequestHandler<UpdateCustomerContactCommand, CustomerContactDto>
{
    private readonly ICustomerContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerContactCommandHandler(ICustomerContactRepository contactRepository, IUnitOfWork unitOfWork)
    {
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerContactDto> Handle(UpdateCustomerContactCommand request, CancellationToken cancellationToken)
    {
        var contact = await _contactRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new CustomerContactNotFoundException();

        if (contact.CustomerId != request.CustomerId)
        {
            throw new CustomerContactNotFoundException();
        }

        if (request.IsPrimary)
        {
            await _contactRepository.ClearPrimaryAsync(contact.CustomerId, contact.Id, cancellationToken);
        }

        contact.Update(
            request.Name,
            request.Role,
            request.Email,
            request.Phone,
            request.Notes,
            request.IsPrimary);

        _contactRepository.Update(contact);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CustomerContactMapper.MapToDto(contact);
    }
}

public class DeleteCustomerContactCommandHandler : IRequestHandler<DeleteCustomerContactCommand, bool>
{
    private readonly ICustomerContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCustomerContactCommandHandler(ICustomerContactRepository contactRepository, IUnitOfWork unitOfWork)
    {
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteCustomerContactCommand request, CancellationToken cancellationToken)
    {
        var contact = await _contactRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new CustomerContactNotFoundException();

        if (contact.CustomerId != request.CustomerId)
        {
            throw new CustomerContactNotFoundException();
        }

        _contactRepository.Remove(contact);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
