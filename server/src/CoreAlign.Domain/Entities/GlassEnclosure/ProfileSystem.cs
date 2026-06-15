using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class ProfileSystem : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid BrandId { get; private set; }
    public GlassSystemType SystemType { get; private set; } = GlassSystemType.Sliding;
    public int MaxPanelWidthMm { get; private set; }
    public int MaxPanelHeightMm { get; private set; }
    public decimal MaxPanelWeightKg { get; private set; }
    public string SupportedGlassThicknessesJson { get; private set; } = "[]";
    public string SupportedOpeningsJson { get; private set; } = "[]";
    public string? CertificationClass { get; private set; }
    public string? FireClass { get; private set; }
    public decimal? ThermalUValue { get; private set; }
    public decimal ThermalBreakFactor { get; private set; } = 1m;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<ProfileItem> _items = new();
    public IReadOnlyCollection<ProfileItem> Items => _items;

    protected ProfileSystem() { }

    public ProfileSystem(
        string code,
        string name,
        Guid brandId,
        GlassSystemType systemType,
        int maxPanelWidthMm,
        int maxPanelHeightMm,
        decimal maxPanelWeightKg,
        string supportedGlassThicknessesJson,
        string supportedOpeningsJson,
        string? certificationClass = null,
        string? fireClass = null,
        decimal? thermalUValue = null,
        decimal thermalBreakFactor = 1m,
        string? description = null)
    {
        Code = code;
        Name = name;
        BrandId = brandId;
        SystemType = systemType;
        MaxPanelWidthMm = maxPanelWidthMm;
        MaxPanelHeightMm = maxPanelHeightMm;
        MaxPanelWeightKg = maxPanelWeightKg;
        SupportedGlassThicknessesJson = supportedGlassThicknessesJson;
        SupportedOpeningsJson = supportedOpeningsJson;
        CertificationClass = certificationClass;
        FireClass = fireClass;
        ThermalUValue = thermalUValue;
        ThermalBreakFactor = thermalBreakFactor;
        Description = description;
    }

    public void Update(
        string name,
        Guid brandId,
        GlassSystemType systemType,
        int maxPanelWidthMm,
        int maxPanelHeightMm,
        decimal maxPanelWeightKg,
        string supportedGlassThicknessesJson,
        string supportedOpeningsJson,
        string? certificationClass,
        string? fireClass,
        decimal? thermalUValue,
        decimal thermalBreakFactor,
        string? description,
        bool isActive)
    {
        Name = name;
        BrandId = brandId;
        SystemType = systemType;
        MaxPanelWidthMm = maxPanelWidthMm;
        MaxPanelHeightMm = maxPanelHeightMm;
        MaxPanelWeightKg = maxPanelWeightKg;
        SupportedGlassThicknessesJson = supportedGlassThicknessesJson;
        SupportedOpeningsJson = supportedOpeningsJson;
        CertificationClass = certificationClass;
        FireClass = fireClass;
        ThermalUValue = thermalUValue;
        ThermalBreakFactor = thermalBreakFactor;
        Description = description;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddItem(ProfileItem item)
    {
        _items.Add(item);
    }
}
