using CoreAlign.Application.Common;
using CoreAlign.Application.Vendors.DTOs;
using MediatR;

namespace CoreAlign.Application.Vendors.Commands;

public record CreateVendorCommand(
    string Name,
    string Type = "Business",
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
    Guid? BuyerUserId = null,
    string? Classification = null,
    string? Territory = null,
    string? LanguageCode = null,
    Guid? ParentVendorId = null,
    string? Notes = null) : IRequest<VendorDto>, ITransactionalRequest;

public record UpdateVendorCommand(
    Guid Id,
    string Type,
    string Name,
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
    Guid? BuyerUserId,
    string? Classification,
    string? Territory,
    string? LanguageCode,
    Guid? ParentVendorId,
    string? Notes) : IRequest<VendorDto>, ITransactionalRequest;

public record ApproveVendorCommand(Guid Id, Guid? ApprovedByUserId = null) : IRequest<VendorDto>, ITransactionalRequest;
public record BlockVendorCommand(Guid Id, string Reason) : IRequest<VendorDto>, ITransactionalRequest;
public record ArchiveVendorCommand(Guid Id) : IRequest<VendorDto>, ITransactionalRequest;
public record SetVendorRatingCommand(Guid Id, int Rating) : IRequest<VendorDto>, ITransactionalRequest;
public record DeleteVendorCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;

// ----- Address / Contact / BankAccount sub-commands -----

public record CreateVendorAddressCommand(
    Guid VendorId,
    string Label,
    string Line1,
    string? Line2 = null,
    string? City = null,
    string? State = null,
    string? PostalCode = null,
    string? Country = null,
    bool IsPrimary = false) : IRequest<VendorAddressDto>, ITransactionalRequest;

public record UpdateVendorAddressCommand(
    Guid Id,
    string Label,
    string Line1,
    string? Line2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    bool IsPrimary) : IRequest<VendorAddressDto>, ITransactionalRequest;

public record DeleteVendorAddressCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;

public record CreateVendorContactCommand(
    Guid VendorId,
    string Name,
    string? Role = null,
    string? Email = null,
    string? Phone = null,
    string? Notes = null,
    bool IsPrimary = false) : IRequest<VendorContactDto>, ITransactionalRequest;

public record UpdateVendorContactCommand(
    Guid Id,
    string Name,
    string? Role,
    string? Email,
    string? Phone,
    string? Notes,
    bool IsPrimary) : IRequest<VendorContactDto>, ITransactionalRequest;

public record DeleteVendorContactCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;

public record CreateVendorBankAccountCommand(
    Guid VendorId,
    string BankName,
    string AccountHolder,
    string Iban,
    string Currency = "TRY",
    string? BranchName = null,
    string? Swift = null,
    string? AccountNumber = null,
    bool IsPrimary = false,
    string? Notes = null) : IRequest<VendorBankAccountDto>, ITransactionalRequest;

public record UpdateVendorBankAccountCommand(
    Guid Id,
    string BankName,
    string? BranchName,
    string AccountHolder,
    string Iban,
    string? Swift,
    string Currency,
    string? AccountNumber,
    bool IsPrimary,
    string? Notes) : IRequest<VendorBankAccountDto>, ITransactionalRequest;

public record DeleteVendorBankAccountCommand(Guid Id) : IRequest<bool>, ITransactionalRequest;
