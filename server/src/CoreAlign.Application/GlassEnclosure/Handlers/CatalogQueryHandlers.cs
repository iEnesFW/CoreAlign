using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Mapping;
using CoreAlign.Application.GlassEnclosure.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Handlers;

public class GetColorOptionsQueryHandler : IRequestHandler<GetColorOptionsQuery, IReadOnlyList<ColorOptionDto>>
{
    private readonly IColorOptionRepository _repository;
    public GetColorOptionsQueryHandler(IColorOptionRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<ColorOptionDto>> Handle(GetColorOptionsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.ListAsync(request.IsActive, cancellationToken);
        return items.Select(GlassEnclosureMappers.ToDto).ToList();
    }
}

public class GetColorOptionByIdQueryHandler : IRequestHandler<GetColorOptionByIdQuery, ColorOptionDto?>
{
    private readonly IColorOptionRepository _repository;
    public GetColorOptionByIdQueryHandler(IColorOptionRepository repository) => _repository = repository;

    public async Task<ColorOptionDto?> Handle(GetColorOptionByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return item is null ? null : GlassEnclosureMappers.ToDto(item);
    }
}

public class GetGlassTypesQueryHandler : IRequestHandler<GetGlassTypesQuery, IReadOnlyList<GlassTypeDto>>
{
    private readonly IGlassTypeRepository _repository;
    public GetGlassTypesQueryHandler(IGlassTypeRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<GlassTypeDto>> Handle(GetGlassTypesQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.ListAsync(request.IsActive, request.Structure, cancellationToken);
        return items.Select(GlassEnclosureMappers.ToDto).ToList();
    }
}

public class GetGlassTypeByIdQueryHandler : IRequestHandler<GetGlassTypeByIdQuery, GlassTypeDto?>
{
    private readonly IGlassTypeRepository _repository;
    public GetGlassTypeByIdQueryHandler(IGlassTypeRepository repository) => _repository = repository;

    public async Task<GlassTypeDto?> Handle(GetGlassTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return item is null ? null : GlassEnclosureMappers.ToDto(item);
    }
}

public class GetProfileSystemsQueryHandler : IRequestHandler<GetProfileSystemsQuery, IReadOnlyList<ProfileSystemDto>>
{
    private readonly IProfileSystemRepository _systemRepo;
    private readonly IBrandRepository _brandRepo;
    public GetProfileSystemsQueryHandler(IProfileSystemRepository systemRepo, IBrandRepository brandRepo)
    {
        _systemRepo = systemRepo;
        _brandRepo = brandRepo;
    }

    public async Task<IReadOnlyList<ProfileSystemDto>> Handle(GetProfileSystemsQuery request, CancellationToken cancellationToken)
    {
        var systems = await _systemRepo.ListAsync(request.IsActive, request.BrandId, request.SystemType, cancellationToken);
        var brands = (await _brandRepo.ListAsync(isActive: null, cancellationToken)).ToDictionary(b => b.Id, b => b.Name);
        return systems
            .Select(s => GlassEnclosureMappers.ToDto(s, brands.TryGetValue(s.BrandId, out var name) ? name : null))
            .ToList();
    }
}

public class GetProfileSystemByIdQueryHandler : IRequestHandler<GetProfileSystemByIdQuery, ProfileSystemDto?>
{
    private readonly IProfileSystemRepository _systemRepo;
    private readonly IBrandRepository _brandRepo;
    public GetProfileSystemByIdQueryHandler(IProfileSystemRepository systemRepo, IBrandRepository brandRepo)
    {
        _systemRepo = systemRepo;
        _brandRepo = brandRepo;
    }

    public async Task<ProfileSystemDto?> Handle(GetProfileSystemByIdQuery request, CancellationToken cancellationToken)
    {
        var system = await _systemRepo.GetWithItemsAsync(request.Id, cancellationToken);
        if (system is null) return null;
        var brand = await _brandRepo.GetByIdAsync(system.BrandId, cancellationToken);
        return GlassEnclosureMappers.ToDto(system, brand?.Name);
    }
}

public class GetProfileItemsBySystemQueryHandler : IRequestHandler<GetProfileItemsBySystemQuery, IReadOnlyList<ProfileItemDto>>
{
    private readonly IProfileItemRepository _repository;
    public GetProfileItemsBySystemQueryHandler(IProfileItemRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<ProfileItemDto>> Handle(GetProfileItemsBySystemQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.ListBySystemAsync(request.SystemId, request.IsActive, cancellationToken);
        return items.Select(GlassEnclosureMappers.ToDto).ToList();
    }
}

public class GetHardwareItemsQueryHandler : IRequestHandler<GetHardwareItemsQuery, IReadOnlyList<HardwareItemDto>>
{
    private readonly IHardwareItemRepository _hwRepo;
    private readonly IBrandRepository _brandRepo;
    public GetHardwareItemsQueryHandler(IHardwareItemRepository hwRepo, IBrandRepository brandRepo)
    {
        _hwRepo = hwRepo;
        _brandRepo = brandRepo;
    }

    public async Task<IReadOnlyList<HardwareItemDto>> Handle(GetHardwareItemsQuery request, CancellationToken cancellationToken)
    {
        var items = await _hwRepo.ListAsync(request.IsActive, request.Category, request.CompatibleSystemId, cancellationToken);
        var brands = (await _brandRepo.ListAsync(isActive: null, cancellationToken)).ToDictionary(b => b.Id, b => b.Name);
        return items
            .Select(i => GlassEnclosureMappers.ToDto(i, brands.TryGetValue(i.BrandId, out var name) ? name : null))
            .ToList();
    }
}

public class GetHardwareItemByIdQueryHandler : IRequestHandler<GetHardwareItemByIdQuery, HardwareItemDto?>
{
    private readonly IHardwareItemRepository _hwRepo;
    private readonly IBrandRepository _brandRepo;
    public GetHardwareItemByIdQueryHandler(IHardwareItemRepository hwRepo, IBrandRepository brandRepo)
    {
        _hwRepo = hwRepo;
        _brandRepo = brandRepo;
    }

    public async Task<HardwareItemDto?> Handle(GetHardwareItemByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _hwRepo.GetByIdAsync(request.Id, cancellationToken);
        if (item is null) return null;
        var brand = await _brandRepo.GetByIdAsync(item.BrandId, cancellationToken);
        return GlassEnclosureMappers.ToDto(item, brand?.Name);
    }
}

public class GetHardwareKitsQueryHandler : IRequestHandler<GetHardwareKitsQuery, IReadOnlyList<HardwareKitDto>>
{
    private readonly IHardwareKitRepository _kitRepo;
    private readonly IProfileSystemRepository _systemRepo;
    public GetHardwareKitsQueryHandler(IHardwareKitRepository kitRepo, IProfileSystemRepository systemRepo)
    {
        _kitRepo = kitRepo;
        _systemRepo = systemRepo;
    }

    public async Task<IReadOnlyList<HardwareKitDto>> Handle(GetHardwareKitsQuery request, CancellationToken cancellationToken)
    {
        var kits = await _kitRepo.ListAsync(request.IsActive, request.SystemId, cancellationToken);
        var systems = (await _systemRepo.ListAsync(cancellationToken: cancellationToken)).ToDictionary(s => s.Id, s => s.Name);
        return kits
            .Select(k => GlassEnclosureMappers.ToDto(k, systems.TryGetValue(k.SystemId, out var name) ? name : null))
            .ToList();
    }
}

public class GetHardwareKitByIdQueryHandler : IRequestHandler<GetHardwareKitByIdQuery, HardwareKitDto?>
{
    private readonly IHardwareKitRepository _kitRepo;
    private readonly IProfileSystemRepository _systemRepo;
    public GetHardwareKitByIdQueryHandler(IHardwareKitRepository kitRepo, IProfileSystemRepository systemRepo)
    {
        _kitRepo = kitRepo;
        _systemRepo = systemRepo;
    }

    public async Task<HardwareKitDto?> Handle(GetHardwareKitByIdQuery request, CancellationToken cancellationToken)
    {
        var kit = await _kitRepo.GetWithItemsAsync(request.Id, cancellationToken);
        if (kit is null) return null;
        var system = await _systemRepo.GetByIdAsync(kit.SystemId, cancellationToken);
        return GlassEnclosureMappers.ToDto(kit, system?.Name);
    }
}

public class GetBrandVendorsQueryHandler : IRequestHandler<GetBrandVendorsQuery, IReadOnlyList<BrandVendorDto>>
{
    private readonly IBrandVendorRepository _linkRepo;
    private readonly IBrandRepository _brandRepo;
    private readonly IVendorRepository _vendorRepo;

    public GetBrandVendorsQueryHandler(
        IBrandVendorRepository linkRepo,
        IBrandRepository brandRepo,
        IVendorRepository vendorRepo)
    {
        _linkRepo = linkRepo;
        _brandRepo = brandRepo;
        _vendorRepo = vendorRepo;
    }

    public async Task<IReadOnlyList<BrandVendorDto>> Handle(GetBrandVendorsQuery request, CancellationToken cancellationToken)
    {
        var links = request.BrandId.HasValue
            ? await _linkRepo.ListByBrandAsync(request.BrandId.Value, request.IsActive, cancellationToken)
            : await _linkRepo.ListAsync(request.IsActive, cancellationToken);

        var brands = (await _brandRepo.ListAsync(isActive: null, cancellationToken)).ToDictionary(b => b.Id, b => b.Name);

        var vendorIds = links.Select(l => l.VendorId).Distinct().ToList();
        var vendors = new Dictionary<Guid, string>();
        foreach (var vendorId in vendorIds)
        {
            var vendor = await _vendorRepo.GetByIdAsync(vendorId, cancellationToken);
            if (vendor is not null) vendors[vendor.Id] = vendor.Name;
        }

        return links.Select(l => GlassEnclosureMappers.ToDto(
            l,
            brands.TryGetValue(l.BrandId, out var bn) ? bn : null,
            vendors.TryGetValue(l.VendorId, out var vn) ? vn : null)).ToList();
    }
}

public class GetBrandVendorByIdQueryHandler : IRequestHandler<GetBrandVendorByIdQuery, BrandVendorDto?>
{
    private readonly IBrandVendorRepository _linkRepo;
    private readonly IBrandRepository _brandRepo;
    private readonly IVendorRepository _vendorRepo;
    public GetBrandVendorByIdQueryHandler(IBrandVendorRepository linkRepo, IBrandRepository brandRepo, IVendorRepository vendorRepo)
    {
        _linkRepo = linkRepo;
        _brandRepo = brandRepo;
        _vendorRepo = vendorRepo;
    }

    public async Task<BrandVendorDto?> Handle(GetBrandVendorByIdQuery request, CancellationToken cancellationToken)
    {
        var link = await _linkRepo.GetByIdAsync(request.Id, cancellationToken);
        if (link is null) return null;
        var brand = await _brandRepo.GetByIdAsync(link.BrandId, cancellationToken);
        var vendor = await _vendorRepo.GetByIdAsync(link.VendorId, cancellationToken);
        return GlassEnclosureMappers.ToDto(link, brand?.Name, vendor?.Name);
    }
}

public class GetDiscountRulesQueryHandler : IRequestHandler<GetDiscountRulesQuery, IReadOnlyList<DiscountRuleDto>>
{
    private readonly IDiscountRuleRepository _repository;
    public GetDiscountRulesQueryHandler(IDiscountRuleRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<DiscountRuleDto>> Handle(GetDiscountRulesQuery request, CancellationToken cancellationToken)
    {
        var rules = await _repository.ListAsync(request.IsActive, request.Scope, cancellationToken);
        return rules.Select(GlassEnclosureMappers.ToDto).ToList();
    }
}

public class GetDiscountRuleByIdQueryHandler : IRequestHandler<GetDiscountRuleByIdQuery, DiscountRuleDto?>
{
    private readonly IDiscountRuleRepository _repository;
    public GetDiscountRuleByIdQueryHandler(IDiscountRuleRepository repository) => _repository = repository;

    public async Task<DiscountRuleDto?> Handle(GetDiscountRuleByIdQuery request, CancellationToken cancellationToken)
    {
        var rule = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return rule is null ? null : GlassEnclosureMappers.ToDto(rule);
    }
}

public class GetGlassNotificationTemplatesQueryHandler : IRequestHandler<GetGlassNotificationTemplatesQuery, IReadOnlyList<GlassNotificationTemplateDto>>
{
    private readonly IGlassNotificationTemplateRepository _repository;
    public GetGlassNotificationTemplatesQueryHandler(IGlassNotificationTemplateRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<GlassNotificationTemplateDto>> Handle(GetGlassNotificationTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await _repository.ListAsync(request.IsActive, request.EventCode, request.Channel, request.Locale, cancellationToken);
        return templates.Select(GlassEnclosureMappers.ToDto).ToList();
    }
}

public class GetGlassNotificationTemplateByIdQueryHandler : IRequestHandler<GetGlassNotificationTemplateByIdQuery, GlassNotificationTemplateDto?>
{
    private readonly IGlassNotificationTemplateRepository _repository;
    public GetGlassNotificationTemplateByIdQueryHandler(IGlassNotificationTemplateRepository repository) => _repository = repository;

    public async Task<GlassNotificationTemplateDto?> Handle(GetGlassNotificationTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return template is null ? null : GlassEnclosureMappers.ToDto(template);
    }
}
