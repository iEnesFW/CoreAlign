using CoreAlign.Application.Common;
using CoreAlign.Application.Customers.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Customers.Commands;

public record CreateCustomerCommand(
    string Name,
    CustomerType Type = CustomerType.Business,
    string? Code = null,
    string? LegalName = null,
    string? TradeName = null,
    string? NationalId = null,
    string? TaxNumber = null,
    string? TaxOffice = null,
    string? Email = null,
    string? Phone = null,
    string? Website = null,
    string DefaultCurrency = "TRY",
    Guid? PaymentTermsId = null,
    Guid? PriceListId = null,
    Guid? CustomerGroupId = null,
    Guid? SalesRepUserId = null,
    decimal CreditLimit = 0m,
    decimal DefaultDiscountPercent = 0m,
    string? Classification = null,
    string? Channel = null,
    string? Territory = null,
    string? LanguageCode = null,
    Guid? ParentCustomerId = null,
    string? Notes = null
) : IRequest<CustomerDto>, ITransactionalRequest;

public record UpdateCustomerCommand(
    Guid Id,
    string Name,
    CustomerType Type,
    string? LegalName,
    string? TradeName,
    string? NationalId,
    string? TaxNumber,
    string? TaxOffice,
    string? Email,
    string? Phone,
    string? Website,
    string DefaultCurrency,
    Guid? PaymentTermsId,
    Guid? PriceListId,
    Guid? CustomerGroupId,
    Guid? SalesRepUserId,
    decimal CreditLimit,
    decimal DefaultDiscountPercent,
    string? Classification,
    string? Channel,
    string? Territory,
    string? LanguageCode,
    Guid? ParentCustomerId,
    string? Notes,
    CustomerStatus Status
) : IRequest<CustomerDto>, ITransactionalRequest;

public record DeleteCustomerCommand(
    Guid Id
) : IRequest<bool>, ITransactionalRequest;
