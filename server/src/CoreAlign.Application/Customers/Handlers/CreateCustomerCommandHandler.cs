using CoreAlign.Application.Customers.Commands;
using CoreAlign.Application.Customers.DTOs;
using CoreAlign.Application.Customers.Mapping;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Customers.Handlers;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IDocumentSequenceRepository _sequenceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IDocumentSequenceRepository sequenceRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _sequenceRepository = sequenceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = new Customer(
            name: request.Name,
            type: request.Type,
            code: request.Code,
            legalName: request.LegalName,
            tradeName: request.TradeName,
            email: request.Email,
            phone: request.Phone,
            taxNumber: request.TaxNumber,
            taxOffice: request.TaxOffice,
            notes: request.Notes,
            defaultCurrency: request.DefaultCurrency);

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

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            var nextCode = await _sequenceRepository.ConsumeAsync(
                Domain.Enums.DocumentSequenceType.CustomerCode,
                DateTime.UtcNow,
                cancellationToken);
            customer.AssignCode(nextCode);
        }

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CustomerMapper.ToDto(customer);
    }
}
