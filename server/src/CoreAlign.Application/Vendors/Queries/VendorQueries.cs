using CoreAlign.Application.Common;
using CoreAlign.Application.Vendors.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Vendors.Queries;

public record GetVendorByIdQuery(Guid Id) : IRequest<VendorDto?>;

public record SearchVendorsQuery(
    string? Search = null,
    VendorStatus? Status = null,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<VendorSummaryDto>>;

public record GetVendorAddressesQuery(Guid VendorId) : IRequest<IReadOnlyList<VendorAddressDto>>;
public record GetVendorContactsQuery(Guid VendorId) : IRequest<IReadOnlyList<VendorContactDto>>;
public record GetVendorBankAccountsQuery(Guid VendorId) : IRequest<IReadOnlyList<VendorBankAccountDto>>;

public record GetVendorLedgerQuery(
    Guid VendorId,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<VendorLedgerEntryDto>>;
