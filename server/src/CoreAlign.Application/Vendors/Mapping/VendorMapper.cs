using CoreAlign.Application.Vendors.DTOs;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Vendors.Mapping;

public static class VendorMapper
{
    public static VendorDto ToDto(Vendor v, string? paymentTermsName = null) => new()
    {
        Id = v.Id,
        Code = v.Code,
        Type = v.Type,
        Name = v.Name,
        LegalName = v.LegalName,
        TradeName = v.TradeName,
        NationalId = v.NationalId,
        TaxNumber = v.TaxNumber,
        TaxOffice = v.TaxOffice,
        Email = v.Email,
        Phone = v.Phone,
        Website = v.Website,
        DefaultCurrency = v.DefaultCurrency,
        PaymentTermsId = v.PaymentTermsId,
        PaymentTermsName = paymentTermsName,
        BuyerUserId = v.BuyerUserId,
        Classification = v.Classification,
        Territory = v.Territory,
        LanguageCode = v.LanguageCode,
        ParentVendorId = v.ParentVendorId,
        Status = v.Status,
        BlockReason = v.BlockReason,
        Notes = v.Notes,
        Rating = v.Rating,
        DefaultLeadTimeDays = v.DefaultLeadTimeDays,
        CurrentBalance = v.CurrentBalance,
        OverdueAmount = v.OverdueAmount,
        TotalPayable = v.TotalPayable,
        ApprovedAtUtc = v.ApprovedAtUtc,
        CreatedAtUtc = v.CreatedAtUtc,
        UpdatedAtUtc = v.UpdatedAtUtc,
    };

    public static VendorSummaryDto ToDto(VendorSearchRow r) => new()
    {
        Id = r.Id,
        Code = r.Code,
        Name = r.Name,
        LegalName = r.LegalName,
        TaxNumber = r.TaxNumber,
        Email = r.Email,
        Phone = r.Phone,
        Type = r.Type,
        Status = r.Status,
        DefaultCurrency = r.DefaultCurrency,
        CurrentBalance = r.CurrentBalance,
        OverdueAmount = r.OverdueAmount,
    };

    public static VendorAddressDto ToDto(VendorAddress a) => new()
    {
        Id = a.Id,
        VendorId = a.VendorId,
        Label = a.Label,
        Line1 = a.Line1,
        Line2 = a.Line2,
        City = a.City,
        State = a.State,
        PostalCode = a.PostalCode,
        Country = a.Country,
        IsPrimary = a.IsPrimary,
    };

    public static VendorContactDto ToDto(VendorContact c) => new()
    {
        Id = c.Id,
        VendorId = c.VendorId,
        Name = c.Name,
        Role = c.Role,
        Email = c.Email,
        Phone = c.Phone,
        Notes = c.Notes,
        IsPrimary = c.IsPrimary,
    };

    public static VendorBankAccountDto ToDto(VendorBankAccount b) => new()
    {
        Id = b.Id,
        VendorId = b.VendorId,
        BankName = b.BankName,
        BranchName = b.BranchName,
        AccountHolder = b.AccountHolder,
        Iban = b.Iban,
        Swift = b.Swift,
        Currency = b.Currency,
        AccountNumber = b.AccountNumber,
        IsPrimary = b.IsPrimary,
        Notes = b.Notes,
    };

    public static VendorLedgerEntryDto ToDto(VendorLedgerEntry e) => new()
    {
        Id = e.Id,
        VendorId = e.VendorId,
        OccurredAtUtc = e.OccurredAtUtc,
        PostingDate = e.PostingDate,
        EntryType = e.EntryType,
        Amount = e.Amount,
        Currency = e.Currency,
        ExchangeRate = e.ExchangeRate,
        AmountInBase = e.AmountInBase,
        SourceType = e.SourceType,
        SourceDocumentId = e.SourceDocumentId,
        SourceDocumentNumber = e.SourceDocumentNumber,
        RunningBalanceAfter = e.RunningBalanceAfter,
        Description = e.Description,
    };
}
