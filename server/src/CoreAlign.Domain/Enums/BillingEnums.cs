namespace CoreAlign.Domain.Enums;

public enum TenantModuleSource
{
    Trial = 0,
    Paid = 1,
    Granted = 2,
    Comp = 3,
}

public enum SubscriptionOrderStatus
{
    Draft = 0,
    PendingPayment = 1,
    Paid = 2,
    Failed = 3,
    Cancelled = 4,
    Expired = 5,
}

public enum PaymentAttemptStatus
{
    Initiated = 0,
    Succeeded = 1,
    Failed = 2,
    Cancelled = 3,
    Refunded = 4,
}
