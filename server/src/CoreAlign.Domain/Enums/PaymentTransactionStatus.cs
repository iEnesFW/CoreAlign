namespace CoreAlign.Domain.Enums;

/// <summary>
/// Lifecycle of a single provider-side payment transaction tracked in our
/// ledger. Drives reconciliation, refund eligibility, and outbox events.
/// </summary>
public enum PaymentTransactionStatus
{
    Pending = 0,
    Authorized = 1,
    Captured = 2,
    Failed = 3,
    Refunded = 4,
    PartiallyRefunded = 5,
    Voided = 6,
}
