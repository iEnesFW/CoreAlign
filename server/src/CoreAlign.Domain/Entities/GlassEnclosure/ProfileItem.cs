using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class ProfileItem : TenantEntity, ICatalogLinkable
{
    public Guid SystemId { get; private set; }
    public ProfileRole Role { get; private set; } = ProfileRole.Top;
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int StockBarLengthMm { get; private set; } = 6000;
    public decimal WeightKgPerMeter { get; private set; }
    public string? CrossSectionSvg { get; private set; }
    public string? CrossSectionDxfUrl { get; private set; }
    public string? ParametricDescriptionJson { get; private set; }
    public Guid? DefaultColorId { get; private set; }
    public Guid? PreferredVendorId { get; private set; }
    public string? VendorPartNumber { get; private set; }
    public int LeadTimeDays { get; private set; }
    public decimal ReorderPointMeters { get; private set; }
    public decimal PricePerKg { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public Guid? LinkedProductId { get; set; }
    public bool IsActive { get; private set; } = true;

    string ICatalogLinkable.Unit => "Kg";
    decimal ICatalogLinkable.UnitCost => PricePerKg;

    protected ProfileItem() { }

    public ProfileItem(
        Guid systemId,
        ProfileRole role,
        string code,
        string name,
        int stockBarLengthMm,
        decimal weightKgPerMeter,
        decimal pricePerKg,
        string? crossSectionSvg = null,
        string? crossSectionDxfUrl = null,
        string? parametricDescriptionJson = null,
        Guid? defaultColorId = null,
        Guid? preferredVendorId = null,
        string? vendorPartNumber = null,
        int leadTimeDays = 0,
        decimal reorderPointMeters = 0m,
        string currency = "TRY",
        Guid? linkedProductId = null)
    {
        SystemId = systemId;
        Role = role;
        Code = code;
        Name = name;
        StockBarLengthMm = stockBarLengthMm;
        WeightKgPerMeter = weightKgPerMeter;
        PricePerKg = pricePerKg;
        CrossSectionSvg = crossSectionSvg;
        CrossSectionDxfUrl = crossSectionDxfUrl;
        ParametricDescriptionJson = parametricDescriptionJson;
        DefaultColorId = defaultColorId;
        PreferredVendorId = preferredVendorId;
        VendorPartNumber = vendorPartNumber;
        LeadTimeDays = leadTimeDays;
        ReorderPointMeters = reorderPointMeters;
        Currency = currency;
        LinkedProductId = linkedProductId;
    }

    public void UpdatePricePerKg(decimal pricePerKg)
    {
        PricePerKg = pricePerKg;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Update(
        ProfileRole role,
        string name,
        int stockBarLengthMm,
        decimal weightKgPerMeter,
        decimal pricePerKg,
        string? crossSectionSvg,
        string? crossSectionDxfUrl,
        string? parametricDescriptionJson,
        Guid? defaultColorId,
        Guid? preferredVendorId,
        string? vendorPartNumber,
        int leadTimeDays,
        decimal reorderPointMeters,
        string currency,
        Guid? linkedProductId,
        bool isActive)
    {
        Role = role;
        Name = name;
        StockBarLengthMm = stockBarLengthMm;
        WeightKgPerMeter = weightKgPerMeter;
        PricePerKg = pricePerKg;
        CrossSectionSvg = crossSectionSvg;
        CrossSectionDxfUrl = crossSectionDxfUrl;
        ParametricDescriptionJson = parametricDescriptionJson;
        DefaultColorId = defaultColorId;
        PreferredVendorId = preferredVendorId;
        VendorPartNumber = vendorPartNumber;
        LeadTimeDays = leadTimeDays;
        ReorderPointMeters = reorderPointMeters;
        Currency = currency;
        LinkedProductId = linkedProductId;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
