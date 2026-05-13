namespace CoreAlign.Domain.Enums;

public enum LedgerEntryType
{
    Debit = 0,
    Credit = 1
}

public enum LedgerSourceType
{
    OpeningBalance = 0,
    Invoice = 1,
    InvoiceVoid = 2,
    CreditNote = 3,
    Payment = 4,
    PaymentReversal = 5,
    Adjustment = 6,
    Refund = 7
}
