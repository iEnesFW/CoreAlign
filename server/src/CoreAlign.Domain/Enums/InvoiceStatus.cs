namespace CoreAlign.Domain.Enums;

public enum InvoiceStatus
{
    Draft = 0,
    Issued = 1,
    Sent = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Overdue = 5,
    Void = 6,
    Cancelled = 7,
    WrittenOff = 8
}
