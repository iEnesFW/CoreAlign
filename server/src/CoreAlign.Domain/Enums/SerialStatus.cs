namespace CoreAlign.Domain.Enums;

// Lifecycle of a single serialized unit. Terminal states are Scrapped (permanent) — a Returned unit
// can be shipped again or scrapped.
public enum SerialStatus
{
    InStock = 0,
    Shipped = 1,
    Returned = 2,
    Scrapped = 3,
}
