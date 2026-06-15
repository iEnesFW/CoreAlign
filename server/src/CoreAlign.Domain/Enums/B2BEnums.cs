namespace CoreAlign.Domain.Enums;

public enum CustomerMembershipRole
{
    CustomerOwner = 1,
    CustomerStaff = 2,
}

public enum DealerMembershipRole
{
    DealerOwner = 1,
    DealerStaff = 2,
}

public enum MembershipStatus
{
    Active = 1,
    Suspended = 2,
    Archived = 3,
}

public enum DealerAccountStatus
{
    Active = 1,
    Suspended = 2,
    Archived = 3,
}

public enum DealerCustomerLinkStatus
{
    Active = 1,
    Suspended = 2,
    Archived = 3,
}

public enum DealerCommissionStatus
{
    Accrued = 1,
    Paid = 2,
    Cancelled = 3,
}
