namespace CoreAlign.Domain.Enums;

public static class DocumentSequenceDefaults
{
    public const int PadLength = 5;

    public static readonly IReadOnlyDictionary<DocumentSequenceType, string> Prefixes =
        new Dictionary<DocumentSequenceType, string>
        {
            [DocumentSequenceType.CustomerCode] = "CUS",
            [DocumentSequenceType.ProductSku] = "PRD",
            [DocumentSequenceType.OrderNumber] = "ORD",
            [DocumentSequenceType.InvoiceNumber] = "INV",
            [DocumentSequenceType.CreditNoteNumber] = "CN",
            [DocumentSequenceType.DebitNoteNumber] = "DN",
            [DocumentSequenceType.PaymentNumber] = "PAY",
            [DocumentSequenceType.ShipmentNumber] = "SHP",
            [DocumentSequenceType.JournalNumber] = "JRN",
            [DocumentSequenceType.SubscriptionOrderNumber] = "SUB",
            [DocumentSequenceType.QuoteNumber] = "QUO",
            [DocumentSequenceType.ReturnRequestNumber] = "RTN",
            [DocumentSequenceType.PurchaseOrderNumber] = "PO",
            [DocumentSequenceType.VendorPaymentNumber] = "VP",
            [DocumentSequenceType.GlassProjectCode] = "GP",
            [DocumentSequenceType.StockCountNumber] = "STC",
            [DocumentSequenceType.PurchaseRequisitionNumber] = "PR",
            [DocumentSequenceType.MrpPlanRunNumber] = "MRP",
            [DocumentSequenceType.GoodsReceiptNumber] = "GRN",
            [DocumentSequenceType.EmployeeNumber] = "PER",
            [DocumentSequenceType.PayrollRunNumber] = "BORD",
            [DocumentSequenceType.PayslipNumber] = "UCRET",
            [DocumentSequenceType.ProductionJobNumber] = "JOB",
        };

    public static string PrefixFor(DocumentSequenceType type) =>
        Prefixes.TryGetValue(type, out var prefix) ? prefix : type.ToString();
}
