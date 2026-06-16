namespace CoreAlign.Domain.Enums;

/// <summary>
/// Identifies the sub-ledger event that produced an automatically generated
/// journal entry. <see cref="Manual"/> covers hand-entered fişler. The source
/// type + document id pair is the idempotency key that prevents a single
/// business event from posting to the GL twice.
/// </summary>
public enum JournalSourceType
{
    Manual = 0,
    SalesInvoice = 1,
    SalesInvoiceReversal = 2,
    CustomerPayment = 3,
    CustomerPaymentReversal = 4,
    VendorBill = 5,
    VendorBillReversal = 6,
    VendorPayment = 7,
    GoodsReceipt = 8,
    CostOfGoodsSold = 9,
    InventoryScrap = 10,
    VendorPaymentReversal = 11,
    CostOfGoodsSoldReversal = 12,
    PurchaseOrderClose = 13,
}
