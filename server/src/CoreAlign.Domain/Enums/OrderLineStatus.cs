namespace CoreAlign.Domain.Enums;

public enum OrderLineStatus
{
    Pending = 0,
    Allocated = 1,
    PartiallyShipped = 2,
    Shipped = 3,
    Invoiced = 4,
    PartiallyReturned = 5,
    Returned = 6,
    Cancelled = 7
}
