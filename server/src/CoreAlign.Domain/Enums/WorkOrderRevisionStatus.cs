namespace CoreAlign.Domain.Enums;

public enum WorkOrderRevisionStatus
{
    SilentSnapshot = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3,
    Blocked = 4
}
