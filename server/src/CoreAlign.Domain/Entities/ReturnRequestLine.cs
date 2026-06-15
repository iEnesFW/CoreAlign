using CoreAlign.Domain.Common;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class ReturnRequestLine : TenantEntity
{
    public Guid ReturnRequestId { get; internal set; }
    public int LineNumber { get; private set; }

    public Guid OrderLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;

    public Guid? UomId { get; private set; }
    public string? UomCode { get; private set; }

    public decimal QuantityReturned { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal UnitCostSnapshot { get; private set; }
    public decimal TaxRatePercent { get; private set; }
    public Guid? TaxRateId { get; private set; }
    public bool IsTaxInclusive { get; private set; }

    public decimal LineSubtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal LineTotal { get; private set; }

    public string? LineNotes { get; private set; }
    public bool Restockable { get; private set; } = true;

    public ReturnRequest ReturnRequest { get; set; } = null!;
    public OrderLine OrderLine { get; set; } = null!;

    protected ReturnRequestLine() { }

    public ReturnRequestLine(
        OrderLine orderLine,
        decimal quantityReturned,
        bool restockable,
        string? lineNotes)
    {
        ArgumentNullException.ThrowIfNull(orderLine);
        if (quantityReturned <= 0m)
        {
            throw new InvalidReturnRequestStateException("Return quantity must be positive.");
        }
        var remaining = Math.Max(0m, orderLine.QuantityShipped - orderLine.QuantityReturned);
        if (quantityReturned > remaining)
        {
            throw new InvalidReturnRequestStateException(
                $"Return quantity {quantityReturned} exceeds remaining returnable quantity {remaining} for line '{orderLine.ProductSku}'.");
        }

        OrderLineId = orderLine.Id;
        ProductId = orderLine.ProductId;
        ProductSku = orderLine.ProductSku;
        ProductName = orderLine.ProductName;
        UomId = orderLine.UomId;
        UomCode = orderLine.UomCode;
        QuantityReturned = quantityReturned;
        UnitPrice = orderLine.UnitPrice;
        UnitCostSnapshot = orderLine.UnitCostSnapshot;
        TaxRatePercent = orderLine.TaxRatePercent;
        TaxRateId = orderLine.TaxRateId;
        IsTaxInclusive = orderLine.IsTaxInclusive;
        Restockable = restockable;
        LineNotes = lineNotes;
        Recalculate();
    }

    public void SetLineNumber(int lineNumber) => LineNumber = lineNumber;

    private void Recalculate()
    {
        LineSubtotal = Math.Round(QuantityReturned * UnitPrice, 4);
        if (IsTaxInclusive)
        {
            var taxBase = TaxRatePercent > 0
                ? LineSubtotal / (1 + (TaxRatePercent / 100m))
                : LineSubtotal;
            TaxAmount = Math.Round(LineSubtotal - taxBase, 4);
            LineTotal = LineSubtotal;
        }
        else
        {
            TaxAmount = TaxRatePercent > 0
                ? Math.Round(LineSubtotal * (TaxRatePercent / 100m), 4)
                : 0m;
            LineTotal = Math.Round(LineSubtotal + TaxAmount, 4);
        }
    }
}
