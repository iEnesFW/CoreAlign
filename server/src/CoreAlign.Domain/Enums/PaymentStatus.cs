namespace CoreAlign.Domain.Enums;

public enum PaymentStatus
{
    Draft = 0,
    Confirmed = 1,
    PartiallyApplied = 2,
    FullyApplied = 3,
    Refunded = 4,
    Void = 5
}
