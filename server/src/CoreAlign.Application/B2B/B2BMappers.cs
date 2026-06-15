using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.B2B;

internal static class B2BMappers
{
    public static CustomerUserDto ToDto(CustomerUser cu, User user, Customer customer) => new(
        cu.Id,
        cu.CustomerId,
        customer.Name,
        cu.UserId,
        user.Email,
        user.FirstName,
        user.LastName,
        cu.MembershipRole,
        cu.Status,
        cu.InvitedByUserId,
        cu.InvitedAtUtc,
        cu.AcceptedAtUtc,
        cu.LastLoginAtUtc,
        cu.SuspensionReason,
        cu.CreatedAtUtc);

    public static DealerAccountDto ToDto(DealerAccount d) => new(
        d.Id,
        d.Code,
        d.Name,
        d.LegalName,
        d.TaxNumber,
        d.Email,
        d.Phone,
        d.Address,
        d.Notes,
        d.Status,
        d.CreatedByUserId,
        d.SuspensionReason,
        d.CreatedAtUtc);

    public static DealerUserDto ToDto(DealerUser du, User user, DealerAccount dealer) => new(
        du.Id,
        du.DealerAccountId,
        dealer.Name,
        du.UserId,
        user.Email,
        user.FirstName,
        user.LastName,
        du.MembershipRole,
        du.Status,
        du.InvitedByUserId,
        du.InvitedAtUtc,
        du.AcceptedAtUtc,
        du.LastLoginAtUtc,
        du.SuspensionReason,
        du.CreatedAtUtc);

    public static DealerCustomerLinkDto ToDto(DealerCustomerLink l, DealerAccount dealer, Customer customer) => new(
        l.Id,
        l.DealerAccountId,
        dealer.Name,
        l.CustomerId,
        customer.Name,
        l.Status,
        l.AssignedByUserId,
        l.AssignedAtUtc,
        l.RevokedAtUtc,
        l.RevokedByUserId,
        l.RevokeReason,
        l.Notes,
        l.CreatedAtUtc);
}
