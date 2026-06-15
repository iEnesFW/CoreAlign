using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IWindZoneRepository
{
    Task<WindZone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WindZone?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WindZone>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task AddAsync(WindZone zone, CancellationToken cancellationToken = default);
    void Update(WindZone zone);
}

public interface IClimateZoneRepository
{
    Task<ClimateZone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClimateZone?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClimateZone>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<ClimateZone?> FindByIlPrefixAsync(string ilPrefix, CancellationToken cancellationToken = default);
    Task AddAsync(ClimateZone zone, CancellationToken cancellationToken = default);
    void Update(ClimateZone zone);
}

public interface IColorOptionRepository
{
    Task<ColorOption?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ColorOption?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ColorOption>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task AddAsync(ColorOption color, CancellationToken cancellationToken = default);
    void Update(ColorOption color);
    void Remove(ColorOption color);
}

public interface IGlassTypeRepository
{
    Task<GlassType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GlassType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    /// <summary>Batched lookup by id set — used to avoid per-panel queries in technical/cutting handlers.</summary>
    Task<IReadOnlyDictionary<Guid, GlassType>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlassType>> ListAsync(bool? isActive = null, GlassStructure? structure = null, CancellationToken cancellationToken = default);
    Task AddAsync(GlassType type, CancellationToken cancellationToken = default);
    void Update(GlassType type);
    void Remove(GlassType type);
}

public interface IProfileSystemRepository
{
    Task<ProfileSystem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProfileSystem?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Batched lookup with Items included — used to avoid per-run queries in cutting handlers.</summary>
    Task<IReadOnlyDictionary<Guid, ProfileSystem>> GetWithItemsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<ProfileSystem?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProfileSystem>> ListAsync(
        bool? isActive = null,
        Guid? brandId = null,
        GlassSystemType? systemType = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(ProfileSystem system, CancellationToken cancellationToken = default);
    void Update(ProfileSystem system);
    void Remove(ProfileSystem system);
}

public interface IProfileItemRepository
{
    Task<ProfileItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProfileItem>> ListBySystemAsync(Guid systemId, bool? isActive = null, CancellationToken cancellationToken = default);
    Task AddAsync(ProfileItem item, CancellationToken cancellationToken = default);
    void Update(ProfileItem item);
    void Remove(ProfileItem item);
}

public interface IHardwareItemRepository
{
    Task<HardwareItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<HardwareItem?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HardwareItem>> ListAsync(
        bool? isActive = null,
        HardwareCategoryKind? category = null,
        Guid? compatibleSystemId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(HardwareItem item, CancellationToken cancellationToken = default);
    void Update(HardwareItem item);
    void Remove(HardwareItem item);
}

public interface IHardwareKitRepository
{
    Task<HardwareKit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<HardwareKit?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<HardwareKit?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HardwareKit>> ListAsync(bool? isActive = null, Guid? systemId = null, CancellationToken cancellationToken = default);
    Task AddAsync(HardwareKit kit, CancellationToken cancellationToken = default);
    void Update(HardwareKit kit);
    void Remove(HardwareKit kit);
}

public interface IBrandVendorRepository
{
    Task<BrandVendor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BrandVendor?> GetByBrandAndVendorAsync(Guid brandId, Guid vendorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BrandVendor>> ListByBrandAsync(Guid brandId, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BrandVendor>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<BrandVendor?> GetPreferredForBrandAsync(Guid brandId, CancellationToken cancellationToken = default);
    Task AddAsync(BrandVendor link, CancellationToken cancellationToken = default);
    void Update(BrandVendor link);
    void Remove(BrandVendor link);
}

public interface IDiscountRuleRepository
{
    Task<DiscountRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DiscountRule?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<DiscountRule?> GetByCouponCodeAsync(string couponCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiscountRule>> ListAsync(bool? isActive = null, DiscountScope? scope = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiscountRule>> ListActiveForCustomerGroupAsync(Guid customerGroupId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task AddAsync(DiscountRule rule, CancellationToken cancellationToken = default);
    void Update(DiscountRule rule);
    void Remove(DiscountRule rule);
}

public interface IGlassNotificationTemplateRepository
{
    Task<GlassNotificationTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GlassNotificationTemplate?> ResolveAsync(
        GlassNotificationEventCode eventCode,
        GlassNotificationChannel channel,
        string locale,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlassNotificationTemplate>> ListAsync(
        bool? isActive = null,
        GlassNotificationEventCode? eventCode = null,
        GlassNotificationChannel? channel = null,
        string? locale = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(GlassNotificationTemplate template, CancellationToken cancellationToken = default);
    void Update(GlassNotificationTemplate template);
    void Remove(GlassNotificationTemplate template);
}

public interface IGlassEnclosureSettingsRepository
{
    Task<GlassEnclosureSettings?> GetForCurrentTenantAsync(CancellationToken cancellationToken = default);
    Task<GlassEnclosureSettings> GetOrCreateForCurrentTenantAsync(CancellationToken cancellationToken = default);
    Task AddAsync(GlassEnclosureSettings settings, CancellationToken cancellationToken = default);
    void Update(GlassEnclosureSettings settings);
}
