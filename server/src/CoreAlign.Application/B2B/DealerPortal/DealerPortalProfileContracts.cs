using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.B2B.DealerPortal;

public record DealerPortalProfileDto(
    Guid UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string TenantName,
    Guid DealerAccountId,
    string DealerName,
    string DealerCode,
    DealerMembershipRole MembershipRole,
    DateTime? LastLoginAtUtc);

public record GetDealerPortalProfileQuery() : IRequest<DealerPortalProfileDto>;
