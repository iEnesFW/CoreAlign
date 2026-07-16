using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Domain.Entities;

public class Product : TenantEntity, IHasConcurrencyToken
{
    public string Sku { get; private set; } = string.Empty;
    public string? Barcode { get; private set; }
    public string? Mpn { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? ShortDescription { get; private set; }
    public string? Description { get; private set; }
    public string? Slug { get; private set; }

    public Guid? BrandId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Guid? ParentProductId { get; private set; }
    public string? VariantAttributesJson { get; private set; }
    public string? TagsJson { get; private set; }

    public string Unit { get; private set; } = "pcs";
    public Guid? BaseUomId { get; private set; }
    public Guid? PurchaseUomId { get; private set; }
    public Guid? SalesUomId { get; private set; }

    public decimal Price { get; private set; }
    public decimal ListPrice { get; private set; }
    public decimal MinSellingPrice { get; private set; }
    public decimal StandardCost { get; private set; }
    public decimal LastPurchaseCost { get; private set; }
    public decimal AverageCost { get; private set; }
    public string Currency { get; private set; } = "TRY";

    public Guid? TaxRateId { get; private set; }
    public bool IsPriceTaxInclusive { get; private set; }

    public decimal StockQuantity { get; private set; }
    public bool IsStockTracked { get; private set; } = true;
    public bool IsLotTracked { get; private set; }
    public bool IsSerialTracked { get; private set; }
    public bool RequiresInspection { get; private set; }
    public decimal MinStock { get; private set; }
    public decimal MaxStock { get; private set; }
    public decimal ReorderPoint { get; private set; }
    public decimal SafetyStock { get; private set; }
    public int LeadTimeDays { get; private set; }

    public decimal? WeightKg { get; private set; }
    public decimal? WidthCm { get; private set; }
    public decimal? HeightCm { get; private set; }
    public decimal? DepthCm { get; private set; }
    public decimal? VolumeM3 { get; private set; }

    // First-class glass attributes (searchable / filterable columns). Free-form extras still live
    // in VariantAttributesJson; colour + thickness are promoted to columns because they are the
    // primary filter axes for glass.
    public string? Color { get; private set; }
    public decimal? ThicknessMm { get; private set; }

    // WHY: min-remnant fields are optional — null means no minimum, so every usable offcut is kept as a remnant.
    public bool IsPlateTracked { get; private set; }
    public decimal? MinRemnantAreaMm2 { get; private set; }
    public decimal? MinRemnantWidthMm { get; private set; }
    public decimal? MinRemnantHeightMm { get; private set; }
    public int? MinPlateCount { get; private set; }
    public decimal? StandardWidthMm { get; private set; }
    public decimal? StandardHeightMm { get; private set; }

    public decimal? MinOrderQuantity { get; private set; }

    public ProcurementType ProcurementType { get; private set; } = ProcurementType.Buy;
    public CostingMethod CostingMethod { get; private set; } = CostingMethod.WeightedAverage;
    public LotSizingPolicy LotSizingPolicy { get; private set; } = LotSizingPolicy.MinMax;
    public decimal FixedOrderQuantity { get; private set; }
    public decimal OrderMultiple { get; private set; }
    public decimal EoqAnnualDemand { get; private set; }
    public decimal OrderingCost { get; private set; }
    public decimal HoldingCostRate { get; private set; }
    public decimal ServiceLevelTarget { get; private set; }

    public AbcClass AbcClass { get; private set; } = AbcClass.Unclassified;

    public Guid? WorkCenterId { get; private set; }
    public decimal RunTimeMinutesPerUnit { get; private set; }
    public Guid? RoutingId { get; private set; }

    public Guid? PreferredSupplierId { get; private set; }

    public ProductStatus Status { get; private set; } = ProductStatus.Active;
    public DateTime? LaunchDate { get; private set; }
    public DateTime? EndOfLifeDate { get; private set; }
    public bool IsActive => Status == ProductStatus.Active || Status == ProductStatus.New;

    // Optimistic concurrency: the order-confirm availability guard reads/writes StockQuantity (the
    // global sellable rollup), so concurrent confirm/allocation/consume/return would otherwise
    // last-writer-win into an oversell. Bumped automatically on Modified by SaveChangesBehavior.
    public long ConcurrencyToken { get; private set; }
    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    public Brand? Brand { get; set; }
    public ProductCategory? Category { get; set; }
    public TaxRate? TaxRate { get; set; }
    public UnitOfMeasure? BaseUom { get; set; }
    public Product? ParentProduct { get; set; }

    protected Product() { }

    public Product(
        string sku,
        string name,
        string unit = "pcs",
        decimal price = 0m,
        string currency = "TRY",
        decimal initialStock = 0m,
        string? description = null)
    {
        Sku = sku;
        Name = name;
        Unit = unit;
        Price = price;
        ListPrice = price;
        Currency = currency;
        StockQuantity = initialStock;
        Description = description;
    }

    public void Update(
        string sku,
        string? barcode,
        string? mpn,
        string name,
        string? shortDescription,
        string? description,
        string? slug,
        Guid? brandId,
        Guid? categoryId,
        Guid? parentProductId,
        string? variantAttributesJson,
        string? tagsJson,
        string unit,
        Guid? baseUomId,
        Guid? purchaseUomId,
        Guid? salesUomId,
        decimal listPrice,
        decimal price,
        decimal minSellingPrice,
        decimal standardCost,
        string currency,
        Guid? taxRateId,
        bool isPriceTaxInclusive,
        bool isStockTracked,
        bool isLotTracked,
        bool isSerialTracked,
        decimal minStock,
        decimal maxStock,
        decimal reorderPoint,
        decimal safetyStock,
        int leadTimeDays,
        decimal? weightKg,
        decimal? widthCm,
        decimal? heightCm,
        decimal? depthCm,
        decimal? volumeM3,
        ProductStatus status,
        DateTime? launchDate,
        DateTime? endOfLifeDate)
    {
        if (parentProductId == Id)
        {
            throw new ArgumentException("A product cannot reference itself as parent.", nameof(parentProductId));
        }
        Sku = sku;
        Barcode = barcode;
        Mpn = mpn;
        Name = name;
        ShortDescription = shortDescription;
        Description = description;
        Slug = slug;
        BrandId = brandId;
        CategoryId = categoryId;
        ParentProductId = parentProductId;
        VariantAttributesJson = variantAttributesJson;
        TagsJson = tagsJson;
        Unit = unit;
        BaseUomId = baseUomId;
        PurchaseUomId = purchaseUomId;
        SalesUomId = salesUomId;
        ListPrice = listPrice;
        Price = price;
        MinSellingPrice = minSellingPrice;
        StandardCost = standardCost;
        Currency = currency;
        TaxRateId = taxRateId;
        IsPriceTaxInclusive = isPriceTaxInclusive;
        IsStockTracked = isStockTracked;
        IsLotTracked = isLotTracked;
        IsSerialTracked = isSerialTracked;
        MinStock = minStock;
        MaxStock = maxStock;
        ReorderPoint = reorderPoint;
        SafetyStock = safetyStock;
        LeadTimeDays = leadTimeDays;
        WeightKg = weightKg;
        WidthCm = widthCm;
        HeightCm = heightCm;
        DepthCm = depthCm;
        VolumeM3 = volumeM3;
        Status = status;
        LaunchDate = launchDate;
        EndOfLifeDate = endOfLifeDate;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AdjustStock(decimal delta)
    {
        if (!IsStockTracked)
        {
            return;
        }
        var next = StockQuantity + delta;
        if (next < 0m)
        {
            throw new InsufficientStockException(Name, StockQuantity, Math.Abs(delta));
        }
        StockQuantity = next;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ChangeStatus(ProductStatus status)
    {
        Status = status;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetRequiresInspection(bool value)
    {
        RequiresInspection = value;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetSerialTracked(bool value)
    {
        IsSerialTracked = value;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetGlassAttributes(string? color, decimal? thicknessMm)
    {
        if (thicknessMm is < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(thicknessMm), "Thickness cannot be negative.");
        }
        Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
        ThicknessMm = thicknessMm;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetPlateTracking(
        bool isPlateTracked,
        decimal? minRemnantAreaMm2,
        decimal? minRemnantWidthMm,
        decimal? minRemnantHeightMm,
        int? minPlateCount,
        decimal? standardWidthMm,
        decimal? standardHeightMm)
    {
        if (minRemnantAreaMm2 is < 0m) throw new ArgumentOutOfRangeException(nameof(minRemnantAreaMm2), "Minimum remnant area cannot be negative.");
        if (minRemnantWidthMm is < 0m) throw new ArgumentOutOfRangeException(nameof(minRemnantWidthMm), "Minimum remnant width cannot be negative.");
        if (minRemnantHeightMm is < 0m) throw new ArgumentOutOfRangeException(nameof(minRemnantHeightMm), "Minimum remnant height cannot be negative.");
        if (minPlateCount is < 0) throw new ArgumentOutOfRangeException(nameof(minPlateCount), "Minimum plate count cannot be negative.");
        if (standardWidthMm is < 0m) throw new ArgumentOutOfRangeException(nameof(standardWidthMm), "Standard width cannot be negative.");
        if (standardHeightMm is < 0m) throw new ArgumentOutOfRangeException(nameof(standardHeightMm), "Standard height cannot be negative.");

        IsPlateTracked = isPlateTracked;
        MinRemnantAreaMm2 = minRemnantAreaMm2;
        MinRemnantWidthMm = minRemnantWidthMm;
        MinRemnantHeightMm = minRemnantHeightMm;
        MinPlateCount = minPlateCount;
        StandardWidthMm = standardWidthMm;
        StandardHeightMm = standardHeightMm;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate() => ChangeStatus(ProductStatus.Active);

    public void Deactivate() => ChangeStatus(ProductStatus.Discontinued);

    public void SetMinOrderQuantity(decimal? value)
    {
        if (value is < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Minimum order quantity cannot be negative.");
        }
        MinOrderQuantity = value;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetPreferredSupplier(Guid? vendorId)
    {
        PreferredSupplierId = vendorId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetProcurementType(ProcurementType procurementType)
    {
        ProcurementType = procurementType;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetCostingMethod(CostingMethod costingMethod)
    {
        CostingMethod = costingMethod;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetAbcClass(AbcClass abcClass)
    {
        AbcClass = abcClass;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AssignRouting(Guid? routingId)
    {
        RoutingId = routingId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetRouting(Guid? workCenterId, decimal runTimeMinutesPerUnit)
    {
        if (runTimeMinutesPerUnit < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(runTimeMinutesPerUnit), "Run time minutes per unit cannot be negative.");
        }
        WorkCenterId = workCenterId;
        RunTimeMinutesPerUnit = runTimeMinutesPerUnit;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetPlanningPolicy(
        LotSizingPolicy lotSizingPolicy,
        decimal fixedOrderQuantity,
        decimal orderMultiple,
        decimal eoqAnnualDemand,
        decimal orderingCost,
        decimal holdingCostRate,
        decimal serviceLevelTarget)
    {
        if (fixedOrderQuantity < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(fixedOrderQuantity), "Fixed order quantity cannot be negative.");
        }
        if (orderMultiple < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(orderMultiple), "Order multiple cannot be negative.");
        }
        if (eoqAnnualDemand < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(eoqAnnualDemand), "Annual demand cannot be negative.");
        }
        if (orderingCost < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(orderingCost), "Ordering cost cannot be negative.");
        }
        if (holdingCostRate < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(holdingCostRate), "Holding cost rate cannot be negative.");
        }
        if (serviceLevelTarget is < 0m or >= 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(serviceLevelTarget), "Service level target must be within [0, 1).");
        }

        LotSizingPolicy = lotSizingPolicy;
        FixedOrderQuantity = fixedOrderQuantity;
        OrderMultiple = orderMultiple;
        EoqAnnualDemand = eoqAnnualDemand;
        OrderingCost = orderingCost;
        HoldingCostRate = holdingCostRate;
        ServiceLevelTarget = serviceLevelTarget;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
