namespace CoreAlign.Domain.Enums;

public enum OrderStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Allocated = 3,
    Picking = 4,
    Packed = 5,
    PartiallyShipped = 6,
    Shipped = 7,
    Delivered = 8,
    Closed = 9,
    Returned = 10,
    Cancelled = 11,
    Confirmed = 12
}
