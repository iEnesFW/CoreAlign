using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

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
    public decimal QuantityScrapped { get; private set; }
    public string? ScrapReason { get; private set; }

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
    public Guid? WithholdingTaxCodeId { get; private set; }
    public string? WithholdingCode { get; private set; }
    public int? WithholdingNumerator { get; private set; }
    public int? WithholdingDenominator { get; private set; }

    public decimal LineSubtotal { get; private set; }
    public decimal LineNetAmount { get; private set; }
    public decimal LineTotal { get; private set; }

    public decimal UnitCostSnapshot { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public OrderLineStatus Status { get; private set; } = OrderLineStatus.Pending;
    public string? LineNotes { get; private set; }
    public Guid? ParentLineId { get; private set; }
    public bool IsKitComponent { get; private set; }

    public Guid? SourceBomLineId { get; private set; }
    public Guid? SourceProjectId { get; private set; }
    public Guid? SubstituteFromProductId { get; private set; }
    public bool IsService { get; private set; }

    // Glass area-based lines: when width/height/pieces are set the line Quantity is DERIVED as the
    // total m² (pieces × width × height), so pricing, cost and stock all run off the cut area.
    // Nullable — a normal quantity-based line leaves these null and is unchanged.
    public decimal? WidthMm { get; private set; }
    public decimal? HeightMm { get; private set; }
    public decimal? Pieces { get; private set; }

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public decimal QuantityRemainingToShip => Math.Max(0m, Quantity - QuantityShipped - QuantityCancelled - QuantityScrapped);
    // WHY a single predicate: three call sites tested QuantityShipped + QuantityCancelled >= Quantity
    // while QuantityRemainingToShip also subtracts scrap, so a line whose remainder was scrapped
    // never counted as shipped and stranded its order in PartiallyShipped forever.
    public bool IsFullyShipped => QuantityRemainingToShip <= 0m;

    public decimal QuantityRemainingToInvoice => Math.Max(0m, Quantity - QuantityInvoiced - QuantityCancelled - QuantityScrapped);

    protected OrderLine() { }

    public OrderLine(
        Guid productId,
        string productSku,
        string productName,
        decimal quantity,
        decimal unitPrice,
        Guid? sourceBomLineId = null,
        Guid? sourceProjectId = null,
        bool isService = false,
        Guid? substituteFromProductId = null)
    {
        ProductId = productId;
        ProductSku = productSku;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        ListPriceSnapshot = unitPrice;
        SourceBomLineId = sourceBomLineId;
        SourceProjectId = sourceProjectId;
        SubstituteFromProductId = substituteFromProductId;
        IsService = isService;
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
        string? productDescriptionSnapshot,
        Guid? withholdingTaxCodeId = null,
        string? withholdingCode = null,
        int? withholdingNumerator = null,
        int? withholdingDenominator = null)
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
        WithholdingTaxCodeId = withholdingTaxCodeId;
        WithholdingCode = withholdingCode;
        WithholdingNumerator = withholdingNumerator;
        WithholdingDenominator = withholdingDenominator;
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

    // Sets the cut dimensions and, when the unit is a square unit (m²/cm²/…) and all three are
    // positive, DERIVES Quantity as the total area in that unit (pieces × width × height). Call
    // AFTER ApplyPricing so the derived area overrides the base quantity for glass lines; a normal
    // (non-area) line, or one with missing dimensions, keeps its quantity unchanged.
    public void SetGlassDimensions(decimal? widthMm, decimal? heightMm, decimal? pieces, string? unitCode)
    {
        WidthMm = widthMm is > 0m ? widthMm : null;
        HeightMm = heightMm is > 0m ? heightMm : null;
        Pieces = pieces is > 0m ? pieces : null;
        var area = GlassLineMath.Area(unitCode, WidthMm, HeightMm, Pieces);
        if (area is > 0m)
        {
            Quantity = area.Value;
            Recalculate();
        }
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

        // WHY: GİB tevkifatı KDV tutarının pay/payda kesridir; kod yoksa eski brüt-yüzde davranışı korunur.
        if (WithholdingNumerator is > 0 && WithholdingDenominator is > 0)
        {
            WithholdingAmount = Math.Round(TaxAmount * WithholdingNumerator.Value / WithholdingDenominator.Value, 4);
        }
        else
        {
            WithholdingAmount = WithholdingRatePercent > 0
                ? Math.Round((net + (IsTaxInclusive ? 0m : TaxAmount)) * (WithholdingRatePercent / 100m), 4)
                : 0m;
        }
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
        if (qty <= 0m)
        {
            throw new InvalidOrderLineException("Shipment quantity must be positive.");
        }
        if (qty > QuantityRemainingToShip)
        {
            throw new InvalidOrderLineException(
                $"Shipment quantity {qty} exceeds remaining-to-ship {QuantityRemainingToShip} for line {Id}.");
        }
        QuantityShipped += qty;
        if (IsFullyShipped)
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
        if (qty <= 0m) return;
        // Last line of defence: the goods coming back cannot exceed the goods that went out.
        // Receiving past it would put phantom stock away and reverse COGS a second time.
        if (QuantityReturned + qty > QuantityShipped)
        {
            throw new ReturnExceedsShippedException(ProductSku, QuantityShipped - QuantityReturned, qty);
        }
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

    public void RecordScrap(decimal qty, string? reason = null)
    {
        if (qty <= 0m)
        {
            throw new InvalidOrderLineException("Scrap quantity must be positive.");
        }
        var remaining = QuantityRemainingToShip;
        if (qty > remaining)
        {
            throw new InvalidOrderLineException(
                $"Scrap quantity {qty} exceeds remaining-to-ship {remaining} for line {Id}.");
        }
        QuantityScrapped += qty;

        // WHY appended and not assigned: a line can be scrapped more than once and each write-off
        // has its own reason; overwriting would erase why the earlier units were written off.
        var trimmed = reason?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;
        var entry = $"{qty:0.####}: {trimmed}";
        ScrapReason = string.IsNullOrEmpty(ScrapReason) ? entry : $"{ScrapReason} | {entry}";
        if (ScrapReason.Length > 500)
        {
            ScrapReason = ScrapReason[^500..];
        }
    }
}
