using CoreAlign.Application.Common;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.B2B;

public record InviteCustomerUserCommand(
    Guid CustomerId,
    string Email,
    string? FirstName,
    string? LastName,
    CustomerMembershipRole Role = CustomerMembershipRole.CustomerStaff,
    Guid CurrentUserId = default,
    IReadOnlyList<string>? CurrentUserRoles = null) : IRequest<CustomerUserDto>, ITransactionalRequest;

public record UpdateCustomerUserStatusCommand(
    Guid Id,
    MembershipStatus Status,
    string? Reason = null,
    Guid CurrentUserId = default,
    IReadOnlyList<string>? CurrentUserRoles = null) : IRequest<CustomerUserDto>, ITransactionalRequest;

public record CreateDealerAccountCommand(
    string Code,
    string Name,
    Guid? PrimaryCustomerId = null,
    string? LegalName = null,
    string? TaxNumber = null,
    string? Email = null,
    string? Phone = null,
    string? Address = null,
    string? Notes = null,
    Guid CurrentUserId = default,
    IReadOnlyList<string>? CurrentUserRoles = null) : IRequest<DealerAccountDto>, ITransactionalRequest;

public record UpdateDealerAccountCommand(
    Guid Id,
    string Name,
    string? LegalName,
    string? TaxNumber,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes,
    Guid CurrentUserId = default,
    IReadOnlyList<string>? CurrentUserRoles = null) : IRequest<DealerAccountDto>, ITransactionalRequest;

public record InviteDealerUserCommand(
    Guid DealerAccountId,
    string Email,
    string? FirstName,
    string? LastName,
    DealerMembershipRole Role = DealerMembershipRole.DealerStaff,
    Guid CurrentUserId = default,
    IReadOnlyList<string>? CurrentUserRoles = null) : IRequest<DealerUserDto>, ITransactionalRequest;

public record UpdateDealerUserStatusCommand(
    Guid Id,
    MembershipStatus Status,
    string? Reason = null,
    Guid CurrentUserId = default,
    IReadOnlyList<string>? CurrentUserRoles = null) : IRequest<DealerUserDto>, ITransactionalRequest;

public record LinkDealerToCustomerCommand(
    Guid DealerAccountId,
    Guid CustomerId,
    string? Notes = null,
    Guid CurrentUserId = default,
    IReadOnlyList<string>? CurrentUserRoles = null) : IRequest<DealerCustomerLinkDto>, ITransactionalRequest;

public record UnlinkDealerFromCustomerCommand(
    Guid LinkId,
    string? Reason = null,
    Guid CurrentUserId = default,
    IReadOnlyList<string>? CurrentUserRoles = null) : IRequest<DealerCustomerLinkDto>, ITransactionalRequest;

public record ListCustomerUsersQuery(
    Guid? CustomerId = null,
    Guid CurrentUserId = default,
    IReadOnlyList<string>? CurrentUserRoles = null) : IRequest<IReadOnlyList<CustomerUserDto>>;

public record ListDealerAccountsQuery(
    Guid? CustomerId = null,
    Guid CurrentUserId = default,
    IReadOnlyList<string>? CurrentUserRoles = null) : IRequest<IReadOnlyList<DealerAccountDto>>;

public record ListDealerUsersQuery(
    Guid DealerAccountId,
    Guid CurrentUserId = default,
    IReadOnlyList<string>? CurrentUserRoles = null) : IRequest<IReadOnlyList<DealerUserDto>>;

public record ListDealerCustomerLinksQuery(
    Guid? DealerAccountId = null,
    Guid? CustomerId = null,
    Guid CurrentUserId = default,
    IReadOnlyList<string>? CurrentUserRoles = null) : IRequest<IReadOnlyList<DealerCustomerLinkDto>>;
