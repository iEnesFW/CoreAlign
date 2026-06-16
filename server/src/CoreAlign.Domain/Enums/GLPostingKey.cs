namespace CoreAlign.Domain.Enums;

/// <summary>
/// Logical posting roles that map to a concrete GL account code. Auto-posting
/// resolves each key to an account through the tenant's
/// <c>GLPostingMapping</c> overrides, falling back to the standard TDHP code
/// when the tenant has not customized it. Decoupling postings from hardcoded
/// codes lets a tenant remap (e.g. merchandise 153 → manufactured 152) without
/// touching code.
/// </summary>
public enum GLPostingKey
{
    AccountsReceivable = 0,
    SalesRevenue = 1,
    OutputVat = 2,
    Cash = 3,
    Bank = 4,
    AccountsPayable = 5,
    InputVat = 6,
    Inventory = 7,
    CostOfGoodsSold = 8,
    GoodsReceiptClearing = 9,
    PurchaseExpense = 10,
    InventoryWriteOff = 11,
    WithholdingReceivable = 12,
    PurchasePriceVariance = 13,
    FxGain = 14,
    FxLoss = 15,
}
