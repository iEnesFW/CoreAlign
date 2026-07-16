using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Products.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? Mpn { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ParentProductId { get; set; }
    public string? VariantAttributesJson { get; set; }
    public string? Color { get; set; }
    public decimal? ThicknessMm { get; set; }
    public string? TagsJson { get; set; }
    public string Unit { get; set; } = string.Empty;
    public Guid? BaseUomId { get; set; }
    public Guid? PurchaseUomId { get; set; }
    public Guid? SalesUomId { get; set; }
    public decimal Price { get; set; }
    public decimal ListPrice { get; set; }
    public decimal MinSellingPrice { get; set; }
    public decimal StandardCost { get; set; }
    public decimal LastPurchaseCost { get; set; }
    public decimal AverageCost { get; set; }
    public string Currency { get; set; } = string.Empty;
    public Guid? TaxRateId { get; set; }
    public bool IsPriceTaxInclusive { get; set; }
    public decimal StockQuantity { get; set; }
    public bool IsStockTracked { get; set; }
    public bool IsLotTracked { get; set; }
    public bool IsSerialTracked { get; set; }
    public bool RequiresInspection { get; set; }
    public bool IsPlateTracked { get; set; }
    public decimal? MinRemnantAreaMm2 { get; set; }
    public decimal? MinRemnantWidthMm { get; set; }
    public decimal? MinRemnantHeightMm { get; set; }
    public int? MinPlateCount { get; set; }
    public decimal? StandardWidthMm { get; set; }
    public decimal? StandardHeightMm { get; set; }
    public decimal MinStock { get; set; }
    public decimal MaxStock { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal? MinOrderQuantity { get; set; }
    public ProcurementType ProcurementType { get; set; }
    public CostingMethod CostingMethod { get; set; }
    public int LeadTimeDays { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? DepthCm { get; set; }
    public decimal? VolumeM3 { get; set; }
    public ProductStatus Status { get; set; }
    public DateTime? LaunchDate { get; set; }
    public DateTime? EndOfLifeDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }
}
