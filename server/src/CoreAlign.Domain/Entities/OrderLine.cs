using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class OrderLine : TenantEntity
{
    public Guid OrderId { get; internal set; }
    public int LineNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public string? ProductDescriptionSnapshot { get; private set; }

    public Guid? UomId { get; private set; }
    public string? UomCode { get; private set; }
    public decimal UomConversionFactor { get; private set; } = 1m;

    public decimal Quantity { get; private set; }
    public decimal QuantityAllocated { get; private set; }
    public decimal QuantityShipped { get; private set; }
    public decimal QuantityInvoiced { get; private set; }
    public decimal QuantityReturned { get; private set; }
    public decimal QuantityCancelled { get; private set; }

    public decimal ListPriceSnapshot { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineDiscountPercent { get; private set; }
    public decimal LineDiscountAmount { get; private set; }
    public bool IsManualPriceOverride { get; private set; }

    public Guid? TaxRateId { get; private set; }
    public decimal TaxRatePercent { get; private set; }
    public decimal TaxAmount { get; private set; }
    public bool IsTaxInclusive { get; private set; }

    public decimal WithholdingRatePercent { get; private set; }
    public decimal WithholdingAmount { get; private set; }

    public decimal LineSubtotal { get; private set; }
    public decimal LineNetAmount { get; private set; }
    public decimal LineTotal { get; private set; }

    public decimal UnitCostSnapshot { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public OrderLineStatus Status { get; private set; } = OrderLineStatus.Pending;
    public string? LineNotes { get; private set; }
    public Guid? ParentLineId { get; private set; }
    public bool IsKitComponent { get; private set; }

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public decimal QuantityRemainingToShip => Math.Max(0m, Quantity - QuantityShipped - QuantityCancelled);
    public decimal QuantityRemainingToInvoice => Math.Max(0m, Quantity - QuantityInvoiced - QuantityCancelled);

    protected OrderLine() { }

    public OrderLine(Guid productId, string productSku, string productName, decimal quantity, decimal unitPrice)
    {
        ProductId = productId;
        ProductSku = productSku;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        ListPriceSnapshot = unitPrice;
        Recalculate();
    }

    public void SetLineNumber(int lineNumber) => LineNumber = lineNumber;

    public void ApplyPricing(
        decimal quantity,
        decimal listPriceSnapshot,
        decimal unitPrice,
        decimal lineDiscountPercent,
        decimal lineDiscountAmount,
        bool isManualPriceOverride,
        decimal taxRatePercent,
        Guid? taxRateId,
        bool isTaxInclusive,
        decimal withholdingRatePercent,
        decimal unitCostSnapshot,
        Guid? uomId,
        string? uomCode,
        decimal uomConversionFactor,
        Guid? warehouseId,
        string? lineNotes,
        Guid? parentLineId,
        bool isKitComponent,
        string? productDescriptionSnapshot)
    {
        Quantity = quantity;
        ListPriceSnapshot = listPriceSnapshot;
        UnitPrice = unitPrice;
        LineDiscountPercent = lineDiscountPercent;
        LineDiscountAmount = lineDiscountAmount;
        IsManualPriceOverride = isManualPriceOverride;
        TaxRatePercent = taxRatePercent;
        TaxRateId = taxRateId;
        IsTaxInclusive = isTaxInclusive;
        WithholdingRatePercent = withholdingRatePercent;
        UnitCostSnapshot = unitCostSnapshot;
        UomId = uomId;
        UomCode = uomCode;
        UomConversionFactor = uomConversionFactor > 0 ? uomConversionFactor : 1m;
        WarehouseId = warehouseId;
        LineNotes = lineNotes;
        ParentLineId = parentLineId;
        IsKitComponent = isKitComponent;
        ProductDescriptionSnapshot = productDescriptionSnapshot;
        Recalculate();
    }

    public void Recalculate()
    {
        var gross = Math.Round(Quantity * UnitPrice, 4);
        LineSubtotal = gross;

        var pctDiscount = LineDiscountPercent > 0 ? gross * (LineDiscountPercent / 100m) : 0m;
        var totalDiscount = pctDiscount + LineDiscountAmount;
        if (totalDiscount > gross) totalDiscount = gross;

        var net = Math.Round(gross - totalDiscount, 4);
        LineNetAmount = net;

        if (IsTaxInclusive)
        {
            var taxBase = TaxRatePercent > 0 ? net / (1 + (TaxRatePercent / 100m)) : net;
            TaxAmount = Math.Round(net - taxBase, 4);
            LineTotal = net;
        }
        else
        {
            TaxAmount = TaxRatePercent > 0 ? Math.Round(net * (TaxRatePercent / 100m), 4) : 0m;
            LineTotal = Math.Round(net + TaxAmount, 4);
        }

        WithholdingAmount = WithholdingRatePercent > 0
            ? Math.Round((net + (IsTaxInclusive ? 0m : TaxAmount)) * (WithholdingRatePercent / 100m), 4)
            : 0m;
    }

    public decimal LineTaxAmount => TaxAmount;
    public decimal LineWithholdingAmount => WithholdingAmount;

    public void RecordAllocation(decimal qty)
    {
        QuantityAllocated += qty;
        if (QuantityAllocated >= Quantity)
        {
            Status = OrderLineStatus.Allocated;
        }
    }

    public void ReleaseAllocation(decimal qty)
    {
        QuantityAllocated = Math.Max(0m, QuantityAllocated - qty);
        if (QuantityAllocated == 0m && Status == OrderLineStatus.Allocated)
        {
            Status = OrderLineStatus.Pending;
        }
    }

    public void RecordShipment(decimal qty)
    {
        QuantityShipped += qty;
        if (QuantityShipped + QuantityCancelled >= Quantity)
        {
            Status = OrderLineStatus.Shipped;
        }
        else if (QuantityShipped > 0)
        {
            Status = OrderLineStatus.PartiallyShipped;
        }
    }

    public void RecordInvoice(decimal qty)
    {
        QuantityInvoiced += qty;
        if (QuantityInvoiced >= Quantity)
        {
            Status = OrderLineStatus.Invoiced;
        }
    }

    public void RecordReturn(decimal qty)
    {
        QuantityReturned += qty;
        Status = QuantityReturned >= QuantityShipped ? OrderLineStatus.Returned : OrderLineStatus.PartiallyReturned;
    }

    public void Cancel(decimal qty)
    {
        QuantityCancelled += qty;
        if (QuantityCancelled >= Quantity)
        {
            Status = OrderLineStatus.Cancelled;
        }
    }
}
