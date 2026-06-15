using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Purchasing;

public class PurchaseRequisitionLine : TenantEntity
{
    public Guid RequisitionId { get; internal set; }
    public int LineNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public decimal QuantityRequested { get; private set; }
    public decimal EstimatedUnitCost { get; private set; }
    public Guid? PreferredSupplierId { get; private set; }
    public DateTime? ExpectedDeliveryDate { get; private set; }
    public string? Notes { get; private set; }

    public PurchaseRequisition Requisition { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public decimal EstimatedLineTotal => Math.Round(QuantityRequested * EstimatedUnitCost, 4);

    protected PurchaseRequisitionLine() { }

    public PurchaseRequisitionLine(
        Guid productId,
        string productSku,
        string productName,
        decimal quantityRequested,
        decimal estimatedUnitCost,
        Guid? preferredSupplierId = null,
        DateTime? expectedDeliveryDate = null,
        string? notes = null)
    {
        ProductId = productId;
        ProductSku = productSku;
        ProductName = productName;
        QuantityRequested = quantityRequested;
        EstimatedUnitCost = estimatedUnitCost;
        PreferredSupplierId = preferredSupplierId;
        ExpectedDeliveryDate = expectedDeliveryDate;
        Notes = notes;
    }

    internal void SetLineNumber(int lineNumber) => LineNumber = lineNumber;
}
