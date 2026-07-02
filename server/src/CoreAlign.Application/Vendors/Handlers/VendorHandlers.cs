using CoreAlign.Application.Common;
using CoreAlign.Application.Vendors.Commands;
using CoreAlign.Application.Vendors.DTOs;
using CoreAlign.Application.Vendors.Mapping;
using CoreAlign.Application.Vendors.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Vendors.Handlers;

internal static class VendorTypeParser
{
    public static VendorType Parse(string raw)
    {
        if (Enum.TryParse<VendorType>(raw, ignoreCase: true, out var t)) return t;
        throw new InvalidVendorTypeException(raw);
    }
}

public class CreateVendorHandler : IRequestHandler<CreateVendorCommand, VendorDto>
{
    private readonly IVendorRepository _vendors;
    private readonly IUnitOfWork _uow;

    public CreateVendorHandler(IVendorRepository vendors, IUnitOfWork uow)
    {
        _vendors = vendors;
        _uow = uow;
    }

    public async Task<VendorDto> Handle(CreateVendorCommand c, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(c.Code) && await _vendors.CodeExistsAsync(c.Code, null, ct))
        {
            throw new VendorCodeConflictException(c.Code);
        }
        if (!string.IsNullOrWhiteSpace(c.TaxNumber) && await _vendors.TaxNumberExistsAsync(c.TaxNumber, null, ct))
        {
            throw new VendorTaxNumberConflictException(c.TaxNumber);
        }

        var type = VendorTypeParser.Parse(c.Type);
        var vendor = new Vendor(
            c.Name,
            type,
            c.Code,
            c.LegalName,
            c.TradeName,
            c.Email,
            c.Phone,
            c.TaxNumber,
            c.TaxOffice,
            c.Notes,
            c.DefaultCurrency);

        // Pre-fill optional fields the constructor didn't capture.
        vendor.Update(
            type,
            c.Name,
            c.LegalName,
            c.TradeName,
            c.NationalId,
            c.TaxNumber,
            c.TaxOffice,
            c.Email,
            c.Phone,
            c.Website,
            c.DefaultCurrency,
            c.PaymentTermsId,
            c.BuyerUserId,
            c.Classification,
            c.Territory,
            c.LanguageCode,
            c.ParentVendorId,
            c.Notes);
        vendor.SetDefaultLeadTime(c.DefaultLeadTimeDays);

        await _vendors.AddAsync(vendor, ct);
        await _uow.SaveChangesAsync(ct);
        return VendorMapper.ToDto(vendor);
    }
}

public class UpdateVendorHandler : IRequestHandler<UpdateVendorCommand, VendorDto>
{
    private readonly IVendorRepository _vendors;
    private readonly IUnitOfWork _uow;

    public UpdateVendorHandler(IVendorRepository vendors, IUnitOfWork uow)
    {
        _vendors = vendors;
        _uow = uow;
    }

    public async Task<VendorDto> Handle(UpdateVendorCommand c, CancellationToken ct)
    {
        var vendor = await _vendors.GetByIdAsync(c.Id, ct) ?? throw new VendorNotFoundException(c.Id);
        if (!string.IsNullOrWhiteSpace(c.TaxNumber) && await _vendors.TaxNumberExistsAsync(c.TaxNumber, c.Id, ct))
        {
            throw new VendorTaxNumberConflictException(c.TaxNumber);
        }
        var type = VendorTypeParser.Parse(c.Type);
        vendor.Update(
            type,
            c.Name,
            c.LegalName,
            c.TradeName,
            c.NationalId,
            c.TaxNumber,
            c.TaxOffice,
            c.Email,
            c.Phone,
            c.Website,
            c.DefaultCurrency,
            c.PaymentTermsId,
            c.BuyerUserId,
            c.Classification,
            c.Territory,
            c.LanguageCode,
            c.ParentVendorId,
            c.Notes);
        vendor.SetDefaultLeadTime(c.DefaultLeadTimeDays);
        _vendors.Update(vendor);
        await _uow.SaveChangesAsync(ct);
        return VendorMapper.ToDto(vendor);
    }
}

public class ApproveVendorHandler : IRequestHandler<ApproveVendorCommand, VendorDto>
{
    private readonly IVendorRepository _vendors;
    private readonly IUnitOfWork _uow;
    public ApproveVendorHandler(IVendorRepository vendors, IUnitOfWork uow) { _vendors = vendors; _uow = uow; }

    public async Task<VendorDto> Handle(ApproveVendorCommand c, CancellationToken ct)
    {
        var vendor = await _vendors.GetByIdAsync(c.Id, ct) ?? throw new VendorNotFoundException(c.Id);
        vendor.Approve(c.ApprovedByUserId ?? Guid.Empty);
        _vendors.Update(vendor);
        await _uow.SaveChangesAsync(ct);
        return VendorMapper.ToDto(vendor);
    }
}

public class BlockVendorHandler : IRequestHandler<BlockVendorCommand, VendorDto>
{
    private readonly IVendorRepository _vendors;
    private readonly IUnitOfWork _uow;
    public BlockVendorHandler(IVendorRepository vendors, IUnitOfWork uow) { _vendors = vendors; _uow = uow; }

    public async Task<VendorDto> Handle(BlockVendorCommand c, CancellationToken ct)
    {
        var vendor = await _vendors.GetByIdAsync(c.Id, ct) ?? throw new VendorNotFoundException(c.Id);
        vendor.Block(c.Reason);
        _vendors.Update(vendor);
        await _uow.SaveChangesAsync(ct);
        return VendorMapper.ToDto(vendor);
    }
}

public class ArchiveVendorHandler : IRequestHandler<ArchiveVendorCommand, VendorDto>
{
    private readonly IVendorRepository _vendors;
    private readonly IUnitOfWork _uow;
    public ArchiveVendorHandler(IVendorRepository vendors, IUnitOfWork uow) { _vendors = vendors; _uow = uow; }

    public async Task<VendorDto> Handle(ArchiveVendorCommand c, CancellationToken ct)
    {
        var vendor = await _vendors.GetByIdAsync(c.Id, ct) ?? throw new VendorNotFoundException(c.Id);
        vendor.Archive();
        _vendors.Update(vendor);
        await _uow.SaveChangesAsync(ct);
        return VendorMapper.ToDto(vendor);
    }
}

public class SetVendorRatingHandler : IRequestHandler<SetVendorRatingCommand, VendorDto>
{
    private readonly IVendorRepository _vendors;
    private readonly IUnitOfWork _uow;
    public SetVendorRatingHandler(IVendorRepository vendors, IUnitOfWork uow) { _vendors = vendors; _uow = uow; }

    public async Task<VendorDto> Handle(SetVendorRatingCommand c, CancellationToken ct)
    {
        var vendor = await _vendors.GetByIdAsync(c.Id, ct) ?? throw new VendorNotFoundException(c.Id);
        vendor.SetRating(c.Rating);
        _vendors.Update(vendor);
        await _uow.SaveChangesAsync(ct);
        return VendorMapper.ToDto(vendor);
    }
}

public class DeleteVendorHandler : IRequestHandler<DeleteVendorCommand, bool>
{
    private readonly IVendorRepository _vendors;
    private readonly IUnitOfWork _uow;
    public DeleteVendorHandler(IVendorRepository vendors, IUnitOfWork uow) { _vendors = vendors; _uow = uow; }

    public async Task<bool> Handle(DeleteVendorCommand c, CancellationToken ct)
    {
        var vendor = await _vendors.GetByIdAsync(c.Id, ct) ?? throw new VendorNotFoundException(c.Id);
        // Future: reject delete if ledger entries / POs / invoices exist; for now
        // child entities cascade-delete via FK, and an archived vendor is the
        // soft-delete equivalent.
        _vendors.Remove(vendor);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

// ---------- Queries ----------

public class GetVendorByIdHandler : IRequestHandler<GetVendorByIdQuery, VendorDto?>
{
    private readonly IVendorRepository _vendors;
    public GetVendorByIdHandler(IVendorRepository vendors) => _vendors = vendors;

    public async Task<VendorDto?> Handle(GetVendorByIdQuery q, CancellationToken ct)
    {
        var vendor = await _vendors.GetByIdAsync(q.Id, ct);
        return vendor is null ? null : VendorMapper.ToDto(vendor);
    }
}

public class SearchVendorsHandler : IRequestHandler<SearchVendorsQuery, PagedResult<VendorSummaryDto>>
{
    private readonly IVendorRepository _vendors;
    public SearchVendorsHandler(IVendorRepository vendors) => _vendors = vendors;

    public async Task<PagedResult<VendorSummaryDto>> Handle(SearchVendorsQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        var (items, total) = await _vendors.SearchAsync(q.Search, q.Status, page, pageSize, ct);
        return new PagedResult<VendorSummaryDto>
        {
            Items = items.Select(VendorMapper.ToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}

public class GetVendorAddressesHandler : IRequestHandler<GetVendorAddressesQuery, IReadOnlyList<VendorAddressDto>>
{
    private readonly IVendorAddressRepository _repo;
    public GetVendorAddressesHandler(IVendorAddressRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<VendorAddressDto>> Handle(GetVendorAddressesQuery q, CancellationToken ct) =>
        (await _repo.GetByVendorAsync(q.VendorId, ct)).Select(VendorMapper.ToDto).ToList();
}

public class GetVendorContactsHandler : IRequestHandler<GetVendorContactsQuery, IReadOnlyList<VendorContactDto>>
{
    private readonly IVendorContactRepository _repo;
    public GetVendorContactsHandler(IVendorContactRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<VendorContactDto>> Handle(GetVendorContactsQuery q, CancellationToken ct) =>
        (await _repo.GetByVendorAsync(q.VendorId, ct)).Select(VendorMapper.ToDto).ToList();
}

public class GetVendorBankAccountsHandler : IRequestHandler<GetVendorBankAccountsQuery, IReadOnlyList<VendorBankAccountDto>>
{
    private readonly IVendorBankAccountRepository _repo;
    public GetVendorBankAccountsHandler(IVendorBankAccountRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<VendorBankAccountDto>> Handle(GetVendorBankAccountsQuery q, CancellationToken ct) =>
        (await _repo.GetByVendorAsync(q.VendorId, ct)).Select(VendorMapper.ToDto).ToList();
}

public class GetVendorLedgerHandler : IRequestHandler<GetVendorLedgerQuery, PagedResult<VendorLedgerEntryDto>>
{
    private readonly IVendorLedgerRepository _repo;
    public GetVendorLedgerHandler(IVendorLedgerRepository repo) => _repo = repo;

    public async Task<PagedResult<VendorLedgerEntryDto>> Handle(GetVendorLedgerQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 200);
        var (items, total) = await _repo.SearchByVendorAsync(q.VendorId, q.FromUtc, q.ToUtc, page, pageSize, ct);
        return new PagedResult<VendorLedgerEntryDto>
        {
            Items = items.Select(VendorMapper.ToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}

// ---------- Address / Contact / BankAccount sub-handlers ----------

public class CreateVendorAddressHandler : IRequestHandler<CreateVendorAddressCommand, VendorAddressDto>
{
    private readonly IVendorRepository _vendors;
    private readonly IVendorAddressRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateVendorAddressHandler(IVendorRepository vendors, IVendorAddressRepository repo, IUnitOfWork uow)
    {
        _vendors = vendors;
        _repo = repo;
        _uow = uow;
    }

    public async Task<VendorAddressDto> Handle(CreateVendorAddressCommand c, CancellationToken ct)
    {
        _ = await _vendors.GetByIdAsync(c.VendorId, ct) ?? throw new VendorNotFoundException(c.VendorId);
        var address = new VendorAddress(c.VendorId, c.Label, c.Line1);
        address.Update(c.Label, c.Line1, c.Line2, c.City, c.State, c.PostalCode, c.Country, c.IsPrimary);
        await _repo.AddAsync(address, ct);
        await _uow.SaveChangesAsync(ct);
        return VendorMapper.ToDto(address);
    }
}

public class UpdateVendorAddressHandler : IRequestHandler<UpdateVendorAddressCommand, VendorAddressDto>
{
    private readonly IVendorAddressRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdateVendorAddressHandler(IVendorAddressRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<VendorAddressDto> Handle(UpdateVendorAddressCommand c, CancellationToken ct)
    {
        var address = await _repo.GetByIdAsync(c.Id, ct) ?? throw new VendorChildNotFoundException("Address");
        address.Update(c.Label, c.Line1, c.Line2, c.City, c.State, c.PostalCode, c.Country, c.IsPrimary);
        _repo.Update(address);
        await _uow.SaveChangesAsync(ct);
        return VendorMapper.ToDto(address);
    }
}

public class DeleteVendorAddressHandler : IRequestHandler<DeleteVendorAddressCommand, bool>
{
    private readonly IVendorAddressRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeleteVendorAddressHandler(IVendorAddressRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<bool> Handle(DeleteVendorAddressCommand c, CancellationToken ct)
    {
        var address = await _repo.GetByIdAsync(c.Id, ct) ?? throw new VendorChildNotFoundException("Address");
        _repo.Remove(address);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

public class CreateVendorContactHandler : IRequestHandler<CreateVendorContactCommand, VendorContactDto>
{
    private readonly IVendorRepository _vendors;
    private readonly IVendorContactRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateVendorContactHandler(IVendorRepository vendors, IVendorContactRepository repo, IUnitOfWork uow)
    {
        _vendors = vendors;
        _repo = repo;
        _uow = uow;
    }

    public async Task<VendorContactDto> Handle(CreateVendorContactCommand c, CancellationToken ct)
    {
        _ = await _vendors.GetByIdAsync(c.VendorId, ct) ?? throw new VendorNotFoundException(c.VendorId);
        var contact = new VendorContact(c.VendorId, c.Name);
        contact.Update(c.Name, c.Role, c.Email, c.Phone, c.Notes, c.IsPrimary);
        await _repo.AddAsync(contact, ct);
        await _uow.SaveChangesAsync(ct);
        return VendorMapper.ToDto(contact);
    }
}

public class UpdateVendorContactHandler : IRequestHandler<UpdateVendorContactCommand, VendorContactDto>
{
    private readonly IVendorContactRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdateVendorContactHandler(IVendorContactRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<VendorContactDto> Handle(UpdateVendorContactCommand c, CancellationToken ct)
    {
        var contact = await _repo.GetByIdAsync(c.Id, ct) ?? throw new VendorChildNotFoundException("Contact");
        contact.Update(c.Name, c.Role, c.Email, c.Phone, c.Notes, c.IsPrimary);
        _repo.Update(contact);
        await _uow.SaveChangesAsync(ct);
        return VendorMapper.ToDto(contact);
    }
}

public class DeleteVendorContactHandler : IRequestHandler<DeleteVendorContactCommand, bool>
{
    private readonly IVendorContactRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeleteVendorContactHandler(IVendorContactRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<bool> Handle(DeleteVendorContactCommand c, CancellationToken ct)
    {
        var contact = await _repo.GetByIdAsync(c.Id, ct) ?? throw new VendorChildNotFoundException("Contact");
        _repo.Remove(contact);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

public class CreateVendorBankAccountHandler : IRequestHandler<CreateVendorBankAccountCommand, VendorBankAccountDto>
{
    private readonly IVendorRepository _vendors;
    private readonly IVendorBankAccountRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateVendorBankAccountHandler(IVendorRepository vendors, IVendorBankAccountRepository repo, IUnitOfWork uow)
    {
        _vendors = vendors;
        _repo = repo;
        _uow = uow;
    }

    public async Task<VendorBankAccountDto> Handle(CreateVendorBankAccountCommand c, CancellationToken ct)
    {
        _ = await _vendors.GetByIdAsync(c.VendorId, ct) ?? throw new VendorNotFoundException(c.VendorId);
        var account = new VendorBankAccount(c.VendorId, c.BankName, c.AccountHolder, c.Iban, c.Currency);
        account.Update(c.BankName, c.BranchName, c.AccountHolder, c.Iban, c.Swift, c.Currency, c.AccountNumber, c.IsPrimary, c.Notes);
        await _repo.AddAsync(account, ct);
        await _uow.SaveChangesAsync(ct);
        return VendorMapper.ToDto(account);
    }
}

public class UpdateVendorBankAccountHandler : IRequestHandler<UpdateVendorBankAccountCommand, VendorBankAccountDto>
{
    private readonly IVendorBankAccountRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdateVendorBankAccountHandler(IVendorBankAccountRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<VendorBankAccountDto> Handle(UpdateVendorBankAccountCommand c, CancellationToken ct)
    {
        var account = await _repo.GetByIdAsync(c.Id, ct) ?? throw new VendorChildNotFoundException("Bank account");
        account.Update(c.BankName, c.BranchName, c.AccountHolder, c.Iban, c.Swift, c.Currency, c.AccountNumber, c.IsPrimary, c.Notes);
        _repo.Update(account);
        await _uow.SaveChangesAsync(ct);
        return VendorMapper.ToDto(account);
    }
}

public class DeleteVendorBankAccountHandler : IRequestHandler<DeleteVendorBankAccountCommand, bool>
{
    private readonly IVendorBankAccountRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeleteVendorBankAccountHandler(IVendorBankAccountRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<bool> Handle(DeleteVendorBankAccountCommand c, CancellationToken ct)
    {
        var account = await _repo.GetByIdAsync(c.Id, ct) ?? throw new VendorChildNotFoundException("Bank account");
        _repo.Remove(account);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
