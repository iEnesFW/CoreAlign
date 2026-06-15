using System.Text.Json;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class WindZoneRepository : IWindZoneRepository
{
    private readonly CoreAlignDbContext _context;
    public WindZoneRepository(CoreAlignDbContext context) => _context = context;

    public Task<WindZone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.WindZones.FirstOrDefaultAsync(z => z.Id == id, cancellationToken);

    public Task<WindZone?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.WindZones.FirstOrDefaultAsync(z => z.Code == code, cancellationToken);

    public async Task<IReadOnlyList<WindZone>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.WindZones.AsNoTracking();
        if (isActive.HasValue) query = query.Where(z => z.IsActive == isActive.Value);
        return await query.OrderBy(z => z.Code).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WindZone zone, CancellationToken cancellationToken = default) =>
        await _context.WindZones.AddAsync(zone, cancellationToken);
    public void Update(WindZone zone) => _context.WindZones.Update(zone);
}

public class ClimateZoneRepository : IClimateZoneRepository
{
    private readonly CoreAlignDbContext _context;
    public ClimateZoneRepository(CoreAlignDbContext context) => _context = context;

    public Task<ClimateZone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.ClimateZones.FirstOrDefaultAsync(z => z.Id == id, cancellationToken);

    public Task<ClimateZone?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.ClimateZones.FirstOrDefaultAsync(z => z.Code == code, cancellationToken);

    public async Task<IReadOnlyList<ClimateZone>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ClimateZones.AsNoTracking();
        if (isActive.HasValue) query = query.Where(z => z.IsActive == isActive.Value);
        return await query.OrderBy(z => z.Code).ToListAsync(cancellationToken);
    }

    public async Task<ClimateZone?> FindByIlPrefixAsync(string ilPrefix, CancellationToken cancellationToken = default)
    {
        var zones = await _context.ClimateZones.AsNoTracking().Where(z => z.IsActive).ToListAsync(cancellationToken);
        foreach (var zone in zones)
        {
            var prefixes = ParseStringArray(zone.IlPostalPrefixListJson);
            if (prefixes.Any(p => string.Equals(p, ilPrefix, StringComparison.OrdinalIgnoreCase)))
            {
                return zone;
            }
        }
        return null;
    }

    public async Task AddAsync(ClimateZone zone, CancellationToken cancellationToken = default) =>
        await _context.ClimateZones.AddAsync(zone, cancellationToken);
    public void Update(ClimateZone zone) => _context.ClimateZones.Update(zone);

    private static IReadOnlyList<string> ParseStringArray(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}

public class ColorOptionRepository : IColorOptionRepository
{
    private readonly CoreAlignDbContext _context;
    public ColorOptionRepository(CoreAlignDbContext context) => _context = context;

    public Task<ColorOption?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassColorOptions.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<ColorOption?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.GlassColorOptions.FirstOrDefaultAsync(c => c.Code == code, cancellationToken);

    public async Task<IReadOnlyList<ColorOption>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.GlassColorOptions.AsNoTracking();
        if (isActive.HasValue) query = query.Where(c => c.IsActive == isActive.Value);
        return await query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ColorOption color, CancellationToken cancellationToken = default) =>
        await _context.GlassColorOptions.AddAsync(color, cancellationToken);
    public void Update(ColorOption color) => _context.GlassColorOptions.Update(color);
    public void Remove(ColorOption color) => _context.GlassColorOptions.Remove(color);
}

public class GlassTypeRepository : IGlassTypeRepository
{
    private readonly CoreAlignDbContext _context;
    public GlassTypeRepository(CoreAlignDbContext context) => _context = context;

    public Task<GlassType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassTypes.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<GlassType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.GlassTypes.FirstOrDefaultAsync(g => g.Code == code, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, GlassType>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var distinct = ids.Distinct().ToArray();
        if (distinct.Length == 0) return new Dictionary<Guid, GlassType>();

        var rows = await _context.GlassTypes
            .AsNoTracking()
            .Where(g => distinct.Contains(g.Id))
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(g => g.Id);
    }

    public async Task<IReadOnlyList<GlassType>> ListAsync(bool? isActive = null, GlassStructure? structure = null, CancellationToken cancellationToken = default)
    {
        var query = _context.GlassTypes.AsNoTracking();
        if (isActive.HasValue) query = query.Where(g => g.IsActive == isActive.Value);
        if (structure.HasValue) query = query.Where(g => g.Structure == structure.Value);
        return await query.OrderBy(g => g.ThicknessMm).ThenBy(g => g.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(GlassType type, CancellationToken cancellationToken = default) =>
        await _context.GlassTypes.AddAsync(type, cancellationToken);
    public void Update(GlassType type) => _context.GlassTypes.Update(type);
    public void Remove(GlassType type) => _context.GlassTypes.Remove(type);
}

public class ProfileSystemRepository : IProfileSystemRepository
{
    private readonly CoreAlignDbContext _context;
    public ProfileSystemRepository(CoreAlignDbContext context) => _context = context;

    public Task<ProfileSystem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassProfileSystems.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<ProfileSystem?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassProfileSystems
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, ProfileSystem>> GetWithItemsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var distinct = ids.Distinct().ToArray();
        if (distinct.Length == 0) return new Dictionary<Guid, ProfileSystem>();

        var rows = await _context.GlassProfileSystems
            .Include(s => s.Items)
            .AsSplitQuery()
            .Where(s => distinct.Contains(s.Id))
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(s => s.Id);
    }

    public Task<ProfileSystem?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.GlassProfileSystems.FirstOrDefaultAsync(s => s.Code == code, cancellationToken);

    public async Task<IReadOnlyList<ProfileSystem>> ListAsync(
        bool? isActive = null,
        Guid? brandId = null,
        GlassSystemType? systemType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.GlassProfileSystems.AsNoTracking();
        if (isActive.HasValue) query = query.Where(s => s.IsActive == isActive.Value);
        if (brandId.HasValue) query = query.Where(s => s.BrandId == brandId.Value);
        if (systemType.HasValue) query = query.Where(s => s.SystemType == systemType.Value);
        return await query.OrderBy(s => s.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProfileSystem system, CancellationToken cancellationToken = default) =>
        await _context.GlassProfileSystems.AddAsync(system, cancellationToken);
    public void Update(ProfileSystem system) => _context.GlassProfileSystems.Update(system);
    public void Remove(ProfileSystem system) => _context.GlassProfileSystems.Remove(system);
}

public class ProfileItemRepository : IProfileItemRepository
{
    private readonly CoreAlignDbContext _context;
    public ProfileItemRepository(CoreAlignDbContext context) => _context = context;

    public Task<ProfileItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassProfileItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProfileItem>> ListBySystemAsync(Guid systemId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.GlassProfileItems.AsNoTracking().Where(i => i.SystemId == systemId);
        if (isActive.HasValue) query = query.Where(i => i.IsActive == isActive.Value);
        return await query.OrderBy(i => i.Role).ThenBy(i => i.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProfileItem item, CancellationToken cancellationToken = default) =>
        await _context.GlassProfileItems.AddAsync(item, cancellationToken);
    public void Update(ProfileItem item) => _context.GlassProfileItems.Update(item);
    public void Remove(ProfileItem item) => _context.GlassProfileItems.Remove(item);
}

public class HardwareItemRepository : IHardwareItemRepository
{
    private readonly CoreAlignDbContext _context;
    public HardwareItemRepository(CoreAlignDbContext context) => _context = context;

    public Task<HardwareItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassHardwareItems.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public Task<HardwareItem?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.GlassHardwareItems.FirstOrDefaultAsync(h => h.Code == code, cancellationToken);

    public async Task<IReadOnlyList<HardwareItem>> ListAsync(
        bool? isActive = null,
        HardwareCategoryKind? category = null,
        Guid? compatibleSystemId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.GlassHardwareItems.AsNoTracking();
        if (isActive.HasValue) query = query.Where(h => h.IsActive == isActive.Value);
        if (category.HasValue) query = query.Where(h => h.Category == category.Value);

        var items = await query.OrderBy(h => h.Name).ToListAsync(cancellationToken);
        if (!compatibleSystemId.HasValue) return items;

        var target = compatibleSystemId.Value;
        return items.Where(h => IsCompatibleWith(h.CompatibleSystemIdsJson, target)).ToList();
    }

    public async Task AddAsync(HardwareItem item, CancellationToken cancellationToken = default) =>
        await _context.GlassHardwareItems.AddAsync(item, cancellationToken);
    public void Update(HardwareItem item) => _context.GlassHardwareItems.Update(item);
    public void Remove(HardwareItem item) => _context.GlassHardwareItems.Remove(item);

    private static bool IsCompatibleWith(string compatibleSystemIdsJson, Guid systemId)
    {
        try
        {
            var ids = JsonSerializer.Deserialize<List<Guid>>(compatibleSystemIdsJson);
            return ids is not null && ids.Contains(systemId);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public class HardwareKitRepository : IHardwareKitRepository
{
    private readonly CoreAlignDbContext _context;
    public HardwareKitRepository(CoreAlignDbContext context) => _context = context;

    public Task<HardwareKit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassHardwareKits.FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

    public Task<HardwareKit?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassHardwareKits
            .Include(k => k.Items)
            .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

    public Task<HardwareKit?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.GlassHardwareKits.FirstOrDefaultAsync(k => k.Code == code, cancellationToken);

    public async Task<IReadOnlyList<HardwareKit>> ListAsync(bool? isActive = null, Guid? systemId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.GlassHardwareKits.AsNoTracking();
        if (isActive.HasValue) query = query.Where(k => k.IsActive == isActive.Value);
        if (systemId.HasValue) query = query.Where(k => k.SystemId == systemId.Value);
        return await query.OrderBy(k => k.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(HardwareKit kit, CancellationToken cancellationToken = default) =>
        await _context.GlassHardwareKits.AddAsync(kit, cancellationToken);
    public void Update(HardwareKit kit) => _context.GlassHardwareKits.Update(kit);
    public void Remove(HardwareKit kit) => _context.GlassHardwareKits.Remove(kit);
}

public class BrandVendorRepository : IBrandVendorRepository
{
    private readonly CoreAlignDbContext _context;
    public BrandVendorRepository(CoreAlignDbContext context) => _context = context;

    public Task<BrandVendor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassBrandVendors.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<BrandVendor?> GetByBrandAndVendorAsync(Guid brandId, Guid vendorId, CancellationToken cancellationToken = default) =>
        _context.GlassBrandVendors.FirstOrDefaultAsync(b => b.BrandId == brandId && b.VendorId == vendorId, cancellationToken);

    public async Task<IReadOnlyList<BrandVendor>> ListByBrandAsync(Guid brandId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.GlassBrandVendors.AsNoTracking().Where(b => b.BrandId == brandId);
        if (isActive.HasValue) query = query.Where(b => b.IsActive == isActive.Value);
        return await query.OrderByDescending(b => b.IsPreferred).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BrandVendor>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.GlassBrandVendors.AsNoTracking();
        if (isActive.HasValue) query = query.Where(b => b.IsActive == isActive.Value);
        return await query.ToListAsync(cancellationToken);
    }

    public Task<BrandVendor?> GetPreferredForBrandAsync(Guid brandId, CancellationToken cancellationToken = default) =>
        _context.GlassBrandVendors.FirstOrDefaultAsync(b => b.BrandId == brandId && b.IsPreferred && b.IsActive, cancellationToken);

    public async Task AddAsync(BrandVendor link, CancellationToken cancellationToken = default) =>
        await _context.GlassBrandVendors.AddAsync(link, cancellationToken);
    public void Update(BrandVendor link) => _context.GlassBrandVendors.Update(link);
    public void Remove(BrandVendor link) => _context.GlassBrandVendors.Remove(link);
}

public class DiscountRuleRepository : IDiscountRuleRepository
{
    private readonly CoreAlignDbContext _context;
    public DiscountRuleRepository(CoreAlignDbContext context) => _context = context;

    public Task<DiscountRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassDiscountRules.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<DiscountRule?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.GlassDiscountRules.FirstOrDefaultAsync(d => d.Code == code, cancellationToken);

    public Task<DiscountRule?> GetByCouponCodeAsync(string couponCode, CancellationToken cancellationToken = default) =>
        _context.GlassDiscountRules.FirstOrDefaultAsync(d => d.CouponCode == couponCode && d.IsActive, cancellationToken);

    public async Task<IReadOnlyList<DiscountRule>> ListAsync(bool? isActive = null, DiscountScope? scope = null, CancellationToken cancellationToken = default)
    {
        var query = _context.GlassDiscountRules.AsNoTracking();
        if (isActive.HasValue) query = query.Where(d => d.IsActive == isActive.Value);
        if (scope.HasValue) query = query.Where(d => d.Scope == scope.Value);
        return await query.OrderBy(d => d.Priority).ThenBy(d => d.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DiscountRule>> ListActiveForCustomerGroupAsync(Guid customerGroupId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await _context.GlassDiscountRules
            .AsNoTracking()
            .Where(d => d.IsActive
                && d.Scope == DiscountScope.CustomerGroup
                && d.CustomerGroupId == customerGroupId
                && (d.ValidFromUtc == null || d.ValidFromUtc <= nowUtc)
                && (d.ValidUntilUtc == null || d.ValidUntilUtc >= nowUtc))
            .OrderBy(d => d.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DiscountRule rule, CancellationToken cancellationToken = default) =>
        await _context.GlassDiscountRules.AddAsync(rule, cancellationToken);
    public void Update(DiscountRule rule) => _context.GlassDiscountRules.Update(rule);
    public void Remove(DiscountRule rule) => _context.GlassDiscountRules.Remove(rule);
}

public class GlassNotificationTemplateRepository : IGlassNotificationTemplateRepository
{
    private readonly CoreAlignDbContext _context;
    public GlassNotificationTemplateRepository(CoreAlignDbContext context) => _context = context;

    public Task<GlassNotificationTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.GlassNotificationTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<GlassNotificationTemplate?> ResolveAsync(
        GlassNotificationEventCode eventCode,
        GlassNotificationChannel channel,
        string locale,
        CancellationToken cancellationToken = default)
    {
        var primary = await _context.GlassNotificationTemplates
            .FirstOrDefaultAsync(
                t => t.EventCode == eventCode && t.Channel == channel && t.Locale == locale && t.IsActive,
                cancellationToken);
        if (primary is not null) return primary;

        return await _context.GlassNotificationTemplates
            .FirstOrDefaultAsync(
                t => t.EventCode == eventCode && t.Channel == channel && t.IsActive,
                cancellationToken);
    }

    public async Task<IReadOnlyList<GlassNotificationTemplate>> ListAsync(
        bool? isActive = null,
        GlassNotificationEventCode? eventCode = null,
        GlassNotificationChannel? channel = null,
        string? locale = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.GlassNotificationTemplates.AsNoTracking();
        if (isActive.HasValue) query = query.Where(t => t.IsActive == isActive.Value);
        if (eventCode.HasValue) query = query.Where(t => t.EventCode == eventCode.Value);
        if (channel.HasValue) query = query.Where(t => t.Channel == channel.Value);
        if (!string.IsNullOrWhiteSpace(locale)) query = query.Where(t => t.Locale == locale);
        return await query.OrderBy(t => t.EventCode).ThenBy(t => t.Channel).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(GlassNotificationTemplate template, CancellationToken cancellationToken = default) =>
        await _context.GlassNotificationTemplates.AddAsync(template, cancellationToken);
    public void Update(GlassNotificationTemplate template) => _context.GlassNotificationTemplates.Update(template);
    public void Remove(GlassNotificationTemplate template) => _context.GlassNotificationTemplates.Remove(template);
}

public class GlassEnclosureSettingsRepository : IGlassEnclosureSettingsRepository
{
    private readonly CoreAlignDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GlassEnclosureSettingsRepository(CoreAlignDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public Task<GlassEnclosureSettings?> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) =>
        _context.GlassEnclosureSettingsStore.FirstOrDefaultAsync(cancellationToken);

    public async Task<GlassEnclosureSettings> GetOrCreateForCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _context.GlassEnclosureSettingsStore.FirstOrDefaultAsync(cancellationToken);
        if (existing is not null) return existing;

        var tenantId = _tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required to create glass enclosure settings.");
        var settings = new GlassEnclosureSettings(tenantId);
        await _context.GlassEnclosureSettingsStore.AddAsync(settings, cancellationToken);
        return settings;
    }

    public async Task AddAsync(GlassEnclosureSettings settings, CancellationToken cancellationToken = default) =>
        await _context.GlassEnclosureSettingsStore.AddAsync(settings, cancellationToken);
    public void Update(GlassEnclosureSettings settings) => _context.GlassEnclosureSettingsStore.Update(settings);
}
