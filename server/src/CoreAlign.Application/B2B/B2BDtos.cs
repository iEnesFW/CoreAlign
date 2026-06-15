using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.B2B;

public record CustomerUserDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid UserId,
    string UserEmail,
    string? UserFirstName,
    string? UserLastName,
    CustomerMembershipRole MembershipRole,
    MembershipStatus Status,
    Guid? InvitedByUserId,
    DateTime InvitedAtUtc,
    DateTime? AcceptedAtUtc,
    DateTime? LastLoginAtUtc,
    string? SuspensionReason,
    DateTime CreatedAtUtc);

public record DealerAccountDto(
    Guid Id,
    string Code,
    string Name,
    string? LegalName,
    string? TaxNumber,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes,
    DealerAccountStatus Status,
    Guid? CreatedByUserId,
    string? SuspensionReason,
    DateTime CreatedAtUtc);

public record DealerUserDto(
    Guid Id,
    Guid DealerAccountId,
    string DealerAccountName,
    Guid UserId,
    string UserEmail,
    string? UserFirstName,
    string? UserLastName,
    DealerMembershipRole MembershipRole,
    MembershipStatus Status,
    Guid? InvitedByUserId,
    DateTime InvitedAtUtc,
    DateTime? AcceptedAtUtc,
    DateTime? LastLoginAtUtc,
    string? SuspensionReason,
    DateTime CreatedAtUtc);

public record DealerCustomerLinkDto(
    Guid Id,
    Guid DealerAccountId,
    string DealerAccountName,
    Guid CustomerId,
    string CustomerName,
    DealerCustomerLinkStatus Status,
    Guid? AssignedByUserId,
    DateTime AssignedAtUtc,
    DateTime? RevokedAtUtc,
    Guid? RevokedByUserId,
    string? RevokeReason,
    string? Notes,
    DateTime CreatedAtUtc);
