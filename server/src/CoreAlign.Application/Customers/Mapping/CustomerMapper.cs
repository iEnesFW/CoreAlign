using CoreAlign.Application.Customers.DTOs;
using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Customers.Mapping;

public static class CustomerMapper
{
    public static CustomerDto ToDto(Customer customer) => new()
    {
        Id = customer.Id,
        Code = customer.Code,
        Type = customer.Type,
        Name = customer.Name,
        LegalName = customer.LegalName,
        TradeName = customer.TradeName,
        NationalId = customer.NationalId,
        TaxNumber = customer.TaxNumber,
        TaxOffice = customer.TaxOffice,
        Email = customer.Email,
        Phone = customer.Phone,
        Website = customer.Website,
        DefaultCurrency = customer.DefaultCurrency,
        PaymentTermsId = customer.PaymentTermsId,
        PriceListId = customer.PriceListId,
        CustomerGroupId = customer.CustomerGroupId,
        SalesRepUserId = customer.SalesRepUserId,
        CreditLimit = customer.CreditLimit,
        CurrentBalance = customer.CurrentBalance,
        OverdueAmount = customer.OverdueAmount,
        DefaultDiscountPercent = customer.DefaultDiscountPercent,
        Classification = customer.Classification,
        Channel = customer.Channel,
        Territory = customer.Territory,
        LanguageCode = customer.LanguageCode,
        ParentCustomerId = customer.ParentCustomerId,
        Status = customer.Status,
        BlockReason = customer.BlockReason,
        Notes = customer.Notes,
        IsActive = customer.IsActive,
        CreatedAtUtc = customer.CreatedAtUtc,
        UpdatedAtUtc = customer.UpdatedAtUtc
    };
}
