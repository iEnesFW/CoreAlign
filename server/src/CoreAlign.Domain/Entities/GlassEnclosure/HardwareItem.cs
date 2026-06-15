using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class HardwareItem : TenantEntity, ICatalogLinkable
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public HardwareCategoryKind Category { get; private set; } = HardwareCategoryKind.Other;
    public Guid BrandId { get; private set; }
    public string CompatibleSystemIdsJson { get; private set; } = "[]";
    public string Unit { get; private set; } = "Piece";
    public decimal UnitPrice { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public decimal? MaxLoadKg { get; private set; }
    public string? ModelGlbUrl { get; private set; }
    public Guid? PreferredVendorId { get; private set; }
    public string? VendorPartNumber { get; private set; }
    public int LeadTimeDays { get; private set; }
    public decimal ReorderPointQuantity { get; private set; }
    public Guid? LinkedProductId { get; set; }
    public bool IsActive { get; private set; } = true;

    decimal ICatalogLinkable.UnitCost => UnitPrice;

    protected HardwareItem() { }

    public HardwareItem(
        string code,
        string name,
        HardwareCategoryKind category,
        Guid brandId,
        string unit,
        decimal unitPrice,
        string compatibleSystemIdsJson = "[]",
        decimal? maxLoadKg = null,
        string? modelGlbUrl = null,
        Guid? preferredVendorId = null,
        string? vendorPartNumber = null,
        int leadTimeDays = 0,
        decimal reorderPointQuantity = 0m,
        string currency = "TRY",
        Guid? linkedProductId = null)
    {
        Code = code;
        Name = name;
        Category = category;
        BrandId = brandId;
        Unit = unit;
        UnitPrice = unitPrice;
        CompatibleSystemIdsJson = compatibleSystemIdsJson;
        MaxLoadKg = maxLoadKg;
        ModelGlbUrl = modelGlbUrl;
        PreferredVendorId = preferredVendorId;
        VendorPartNumber = vendorPartNumber;
        LeadTimeDays = leadTimeDays;
        ReorderPointQuantity = reorderPointQuantity;
        Currency = currency;
        LinkedProductId = linkedProductId;
    }

    public void UpdateUnitPrice(decimal unitPrice)
    {
        UnitPrice = unitPrice;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Update(
        string name,
        HardwareCategoryKind category,
        Guid brandId,
        string unit,
        decimal unitPrice,
        string compatibleSystemIdsJson,
        decimal? maxLoadKg,
        string? modelGlbUrl,
        Guid? preferredVendorId,
        string? vendorPartNumber,
        int leadTimeDays,
        decimal reorderPointQuantity,
        string currency,
        Guid? linkedProductId,
        bool isActive)
    {
        Name = name;
        Category = category;
        BrandId = brandId;
        Unit = unit;
        UnitPrice = unitPrice;
        CompatibleSystemIdsJson = compatibleSystemIdsJson;
        MaxLoadKg = maxLoadKg;
        ModelGlbUrl = modelGlbUrl;
        PreferredVendorId = preferredVendorId;
        VendorPartNumber = vendorPartNumber;
        LeadTimeDays = leadTimeDays;
        ReorderPointQuantity = reorderPointQuantity;
        Currency = currency;
        LinkedProductId = linkedProductId;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
