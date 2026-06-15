using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class DealerUser : TenantEntity
{
    public Guid UserId { get; private set; }
    public Guid DealerAccountId { get; private set; }
    public DealerMembershipRole MembershipRole { get; private set; }
    public MembershipStatus Status { get; private set; } = MembershipStatus.Active;
    public Guid? InvitedByUserId { get; private set; }
    public DateTime InvitedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? AcceptedAtUtc { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }
    public string? SuspensionReason { get; private set; }

    public User User { get; set; } = null!;
    public DealerAccount DealerAccount { get; set; } = null!;

    protected DealerUser() { }

    public DealerUser(
        Guid userId,
        Guid dealerAccountId,
        DealerMembershipRole role,
        Guid? invitedByUserId)
    {
        UserId = userId;
        DealerAccountId = dealerAccountId;
        MembershipRole = role;
        InvitedByUserId = invitedByUserId;
        Status = MembershipStatus.Active;
        InvitedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = MembershipStatus.Active;
        SuspensionReason = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Suspend(string? reason)
    {
        Status = MembershipStatus.Suspended;
        SuspensionReason = reason;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = MembershipStatus.Archived;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ChangeRole(DealerMembershipRole role)
    {
        MembershipRole = role;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordAcceptance()
    {
        AcceptedAtUtc ??= DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordLogin()
    {
        LastLoginAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
