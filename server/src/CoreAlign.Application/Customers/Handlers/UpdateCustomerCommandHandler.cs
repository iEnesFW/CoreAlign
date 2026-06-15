using CoreAlign.Application.Customers.Commands;
using CoreAlign.Application.Customers.DTOs;
using CoreAlign.Application.Customers.Mapping;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Customers.Handlers;

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerTagLinkRepository _customerTagLinkRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        ICustomerTagLinkRepository customerTagLinkRepository,
        ITagRepository tagRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _customerTagLinkRepository = customerTagLinkRepository;
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new CustomerNotFoundException();

        if (customer.IsAnonymized)
        {
            throw new CustomerIsAnonymizedException();
        }

        customer.Update(
            type: request.Type,
            name: request.Name,
            legalName: request.LegalName,
            tradeName: request.TradeName,
            nationalId: request.NationalId,
            taxNumber: request.TaxNumber,
            taxOffice: request.TaxOffice,
            email: request.Email,
            phone: request.Phone,
            website: request.Website,
            defaultCurrency: request.DefaultCurrency,
            paymentTermsId: request.PaymentTermsId,
            priceListId: request.PriceListId,
            customerGroupId: request.CustomerGroupId,
            salesRepUserId: request.SalesRepUserId,
            creditLimit: request.CreditLimit,
            defaultDiscountPercent: request.DefaultDiscountPercent,
            classification: request.Classification,
            channel: request.Channel,
            territory: request.Territory,
            languageCode: request.LanguageCode,
            parentCustomerId: request.ParentCustomerId,
            notes: request.Notes);

        switch (request.Status)
        {
            case CustomerStatus.Active:
                customer.Activate();
                break;
            case CustomerStatus.Blocked:
                customer.Block(request.Notes ?? "Blocked by administrator");
                break;
            case CustomerStatus.Archived:
                customer.Archive();
                break;
        }

        _customerRepository.Update(customer);

        if (request.TagIds is not null)
        {
            if (request.TagIds.Count > 0)
            {
                var resolved = await _tagRepository.GetByIdsAsync(request.TagIds, cancellationToken);
                if (resolved.Count != request.TagIds.Count)
                {
                    throw new CustomerNotFoundException();
                }
            }
            await _customerTagLinkRepository.SyncAsync(customer.Id, request.TagIds, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CustomerMapper.ToDto(customer);
    }
}
