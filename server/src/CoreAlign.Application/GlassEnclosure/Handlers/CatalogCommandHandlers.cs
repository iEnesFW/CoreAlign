using CoreAlign.Application.Catalog.Linker;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Mapping;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Handlers;

public class CreateColorOptionCommandHandler : IRequestHandler<CreateColorOptionCommand, ColorOptionDto>
{
    private readonly IColorOptionRepository _repository;
    public CreateColorOptionCommandHandler(IColorOptionRepository repository) => _repository = repository;

    public async Task<ColorOptionDto> Handle(CreateColorOptionCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByCodeAsync(request.Data.Code, cancellationToken);
        if (existing is not null) throw new GlassEnclosureDuplicateCodeException("ColorOption", request.Data.Code);

        var color = new ColorOption(
            request.Data.Code, request.Data.Name, request.Data.HexColor, request.Data.FinishType,
            request.Data.RalCode, request.Data.PriceModifierPercent, request.Data.SortOrder);
        await _repository.AddAsync(color, cancellationToken);
        return GlassEnclosureMappers.ToDto(color);
    }
}

public class UpdateColorOptionCommandHandler : IRequestHandler<UpdateColorOptionCommand, ColorOptionDto>
{
    private readonly IColorOptionRepository _repository;
    public UpdateColorOptionCommandHandler(IColorOptionRepository repository) => _repository = repository;

    public async Task<ColorOptionDto> Handle(UpdateColorOptionCommand request, CancellationToken cancellationToken)
    {
        var color = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("ColorOption");
        color.Update(
            request.Data.Name, request.Data.HexColor, request.Data.FinishType,
            request.Data.RalCode, request.Data.PriceModifierPercent,
            request.Data.SortOrder, request.Data.IsActive);
        _repository.Update(color);
        return GlassEnclosureMappers.ToDto(color);
    }
}

public class DeleteColorOptionCommandHandler : IRequestHandler<DeleteColorOptionCommand, Unit>
{
    private readonly IColorOptionRepository _repository;
    public DeleteColorOptionCommandHandler(IColorOptionRepository repository) => _repository = repository;

    public async Task<Unit> Handle(DeleteColorOptionCommand request, CancellationToken cancellationToken)
    {
        var color = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("ColorOption");
        _repository.Remove(color);
        return Unit.Value;
    }
}

public class CreateGlassTypeCommandHandler : IRequestHandler<CreateGlassTypeCommand, GlassTypeDto>
{
    private readonly IGlassTypeRepository _repository;
    private readonly ICatalogProductLinker _linker;

    public CreateGlassTypeCommandHandler(IGlassTypeRepository repository, ICatalogProductLinker linker)
    {
        _repository = repository;
        _linker = linker;
    }

    public async Task<GlassTypeDto> Handle(CreateGlassTypeCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByCodeAsync(request.Data.Code, cancellationToken);
        if (existing is not null) throw new GlassEnclosureDuplicateCodeException("GlassType", request.Data.Code);

        var glass = new GlassType(
            request.Data.Code, request.Data.Name, request.Data.ThicknessMm, request.Data.Structure,
            request.Data.PricePerM2, request.Data.WeightKgPerM2, request.Data.AllowablePressurePa,
            request.Data.MaxPanelAreaM2, request.Data.UValue, request.Data.SoundDb,
            GlassEnclosureMappers.SerializeDecimalArray(request.Data.GlassLayers ?? Array.Empty<decimal>()),
            request.Data.Currency, request.Data.LinkedProductId);
        await _repository.AddAsync(glass, cancellationToken);
        await _linker.EnsureLinkedAsync(glass, CatalogItemKind.Glass, cancellationToken);
        return GlassEnclosureMappers.ToDto(glass);
    }
}

public class UpdateGlassTypeCommandHandler : IRequestHandler<UpdateGlassTypeCommand, GlassTypeDto>
{
    private readonly IGlassTypeRepository _repository;
    public UpdateGlassTypeCommandHandler(IGlassTypeRepository repository) => _repository = repository;

    public async Task<GlassTypeDto> Handle(UpdateGlassTypeCommand request, CancellationToken cancellationToken)
    {
        var glass = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("GlassType");
        glass.Update(
            request.Data.Name, request.Data.ThicknessMm, request.Data.Structure,
            request.Data.PricePerM2, request.Data.WeightKgPerM2, request.Data.AllowablePressurePa,
            request.Data.MaxPanelAreaM2, request.Data.UValue, request.Data.SoundDb,
            GlassEnclosureMappers.SerializeDecimalArray(request.Data.GlassLayers ?? Array.Empty<decimal>()),
            request.Data.Currency, request.Data.LinkedProductId, request.Data.IsActive);
        _repository.Update(glass);
        return GlassEnclosureMappers.ToDto(glass);
    }
}

public class DeleteGlassTypeCommandHandler : IRequestHandler<DeleteGlassTypeCommand, Unit>
{
    private readonly IGlassTypeRepository _repository;
    public DeleteGlassTypeCommandHandler(IGlassTypeRepository repository) => _repository = repository;

    public async Task<Unit> Handle(DeleteGlassTypeCommand request, CancellationToken cancellationToken)
    {
        var glass = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("GlassType");
        _repository.Remove(glass);
        return Unit.Value;
    }
}

public class CreateProfileSystemCommandHandler : IRequestHandler<CreateProfileSystemCommand, ProfileSystemDto>
{
    private readonly IProfileSystemRepository _repository;
    public CreateProfileSystemCommandHandler(IProfileSystemRepository repository) => _repository = repository;

    public async Task<ProfileSystemDto> Handle(CreateProfileSystemCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByCodeAsync(request.Data.Code, cancellationToken);
        if (existing is not null) throw new GlassEnclosureDuplicateCodeException("ProfileSystem", request.Data.Code);

        var system = new ProfileSystem(
            request.Data.Code, request.Data.Name, request.Data.BrandId, request.Data.SystemType,
            request.Data.MaxPanelWidthMm, request.Data.MaxPanelHeightMm, request.Data.MaxPanelWeightKg,
            GlassEnclosureMappers.SerializeIntArray(request.Data.SupportedGlassThicknesses),
            GlassEnclosureMappers.SerializeOpenings(request.Data.SupportedOpenings),
            request.Data.CertificationClass, request.Data.FireClass,
            request.Data.ThermalUValue, request.Data.ThermalBreakFactor, request.Data.Description);
        await _repository.AddAsync(system, cancellationToken);
        return GlassEnclosureMappers.ToDto(system);
    }
}

public class UpdateProfileSystemCommandHandler : IRequestHandler<UpdateProfileSystemCommand, ProfileSystemDto>
{
    private readonly IProfileSystemRepository _repository;
    public UpdateProfileSystemCommandHandler(IProfileSystemRepository repository) => _repository = repository;

    public async Task<ProfileSystemDto> Handle(UpdateProfileSystemCommand request, CancellationToken cancellationToken)
    {
        var system = await _repository.GetWithItemsAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("ProfileSystem");
        system.Update(
            request.Data.Name, request.Data.BrandId, request.Data.SystemType,
            request.Data.MaxPanelWidthMm, request.Data.MaxPanelHeightMm, request.Data.MaxPanelWeightKg,
            GlassEnclosureMappers.SerializeIntArray(request.Data.SupportedGlassThicknesses),
            GlassEnclosureMappers.SerializeOpenings(request.Data.SupportedOpenings),
            request.Data.CertificationClass, request.Data.FireClass,
            request.Data.ThermalUValue, request.Data.ThermalBreakFactor,
            request.Data.Description, request.Data.IsActive);
        _repository.Update(system);
        return GlassEnclosureMappers.ToDto(system);
    }
}

public class DeleteProfileSystemCommandHandler : IRequestHandler<DeleteProfileSystemCommand, Unit>
{
    private readonly IProfileSystemRepository _repository;
    public DeleteProfileSystemCommandHandler(IProfileSystemRepository repository) => _repository = repository;

    public async Task<Unit> Handle(DeleteProfileSystemCommand request, CancellationToken cancellationToken)
    {
        var system = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("ProfileSystem");
        _repository.Remove(system);
        return Unit.Value;
    }
}

public class CreateProfileItemCommandHandler : IRequestHandler<CreateProfileItemCommand, ProfileItemDto>
{
    private readonly IProfileItemRepository _itemRepo;
    private readonly IProfileSystemRepository _systemRepo;
    private readonly ICatalogProductLinker _linker;

    public CreateProfileItemCommandHandler(
        IProfileItemRepository itemRepo,
        IProfileSystemRepository systemRepo,
        ICatalogProductLinker linker)
    {
        _itemRepo = itemRepo;
        _systemRepo = systemRepo;
        _linker = linker;
    }

    public async Task<ProfileItemDto> Handle(CreateProfileItemCommand request, CancellationToken cancellationToken)
    {
        var system = await _systemRepo.GetByIdAsync(request.Data.SystemId, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("ProfileSystem");

        var item = new ProfileItem(
            system.Id, request.Data.Role, request.Data.Code, request.Data.Name,
            request.Data.StockBarLengthMm, request.Data.WeightKgPerMeter, request.Data.PricePerKg,
            request.Data.CrossSectionSvg, request.Data.CrossSectionDxfUrl, request.Data.ParametricDescriptionJson,
            request.Data.DefaultColorId, request.Data.PreferredVendorId, request.Data.VendorPartNumber,
            request.Data.LeadTimeDays, request.Data.ReorderPointMeters,
            request.Data.Currency, request.Data.LinkedProductId);
        await _itemRepo.AddAsync(item, cancellationToken);
        await _linker.EnsureLinkedAsync(item, CatalogItemKind.Profile, cancellationToken);
        return GlassEnclosureMappers.ToDto(item);
    }
}

public class UpdateProfileItemCommandHandler : IRequestHandler<UpdateProfileItemCommand, ProfileItemDto>
{
    private readonly IProfileItemRepository _repository;
    public UpdateProfileItemCommandHandler(IProfileItemRepository repository) => _repository = repository;

    public async Task<ProfileItemDto> Handle(UpdateProfileItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("ProfileItem");
        item.Update(
            request.Data.Role, request.Data.Name, request.Data.StockBarLengthMm,
            request.Data.WeightKgPerMeter, request.Data.PricePerKg,
            request.Data.CrossSectionSvg, request.Data.CrossSectionDxfUrl, request.Data.ParametricDescriptionJson,
            request.Data.DefaultColorId, request.Data.PreferredVendorId, request.Data.VendorPartNumber,
            request.Data.LeadTimeDays, request.Data.ReorderPointMeters,
            request.Data.Currency, request.Data.LinkedProductId, request.Data.IsActive);
        _repository.Update(item);
        return GlassEnclosureMappers.ToDto(item);
    }
}

public class DeleteProfileItemCommandHandler : IRequestHandler<DeleteProfileItemCommand, Unit>
{
    private readonly IProfileItemRepository _repository;
    public DeleteProfileItemCommandHandler(IProfileItemRepository repository) => _repository = repository;

    public async Task<Unit> Handle(DeleteProfileItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("ProfileItem");
        _repository.Remove(item);
        return Unit.Value;
    }
}

public class CreateHardwareItemCommandHandler : IRequestHandler<CreateHardwareItemCommand, HardwareItemDto>
{
    private readonly IHardwareItemRepository _repository;
    private readonly ICatalogProductLinker _linker;

    public CreateHardwareItemCommandHandler(IHardwareItemRepository repository, ICatalogProductLinker linker)
    {
        _repository = repository;
        _linker = linker;
    }

    public async Task<HardwareItemDto> Handle(CreateHardwareItemCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByCodeAsync(request.Data.Code, cancellationToken);
        if (existing is not null) throw new GlassEnclosureDuplicateCodeException("HardwareItem", request.Data.Code);

        var item = new HardwareItem(
            request.Data.Code, request.Data.Name, request.Data.Category, request.Data.BrandId,
            request.Data.Unit, request.Data.UnitPrice,
            GlassEnclosureMappers.SerializeGuidArray(request.Data.CompatibleSystemIds ?? Array.Empty<Guid>()),
            request.Data.MaxLoadKg, request.Data.ModelGlbUrl,
            request.Data.PreferredVendorId, request.Data.VendorPartNumber,
            request.Data.LeadTimeDays, request.Data.ReorderPointQuantity,
            request.Data.Currency, request.Data.LinkedProductId);
        await _repository.AddAsync(item, cancellationToken);
        await _linker.EnsureLinkedAsync(item, CatalogItemKind.Hardware, cancellationToken);
        return GlassEnclosureMappers.ToDto(item);
    }
}

public class UpdateHardwareItemCommandHandler : IRequestHandler<UpdateHardwareItemCommand, HardwareItemDto>
{
    private readonly IHardwareItemRepository _repository;
    public UpdateHardwareItemCommandHandler(IHardwareItemRepository repository) => _repository = repository;

    public async Task<HardwareItemDto> Handle(UpdateHardwareItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("HardwareItem");
        item.Update(
            request.Data.Name, request.Data.Category, request.Data.BrandId,
            request.Data.Unit, request.Data.UnitPrice,
            GlassEnclosureMappers.SerializeGuidArray(request.Data.CompatibleSystemIds ?? Array.Empty<Guid>()),
            request.Data.MaxLoadKg, request.Data.ModelGlbUrl,
            request.Data.PreferredVendorId, request.Data.VendorPartNumber,
            request.Data.LeadTimeDays, request.Data.ReorderPointQuantity,
            request.Data.Currency, request.Data.LinkedProductId, request.Data.IsActive);
        _repository.Update(item);
        return GlassEnclosureMappers.ToDto(item);
    }
}

public class DeleteHardwareItemCommandHandler : IRequestHandler<DeleteHardwareItemCommand, Unit>
{
    private readonly IHardwareItemRepository _repository;
    public DeleteHardwareItemCommandHandler(IHardwareItemRepository repository) => _repository = repository;

    public async Task<Unit> Handle(DeleteHardwareItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("HardwareItem");
        _repository.Remove(item);
        return Unit.Value;
    }
}

public class CreateHardwareKitCommandHandler : IRequestHandler<CreateHardwareKitCommand, HardwareKitDto>
{
    private readonly IHardwareKitRepository _kitRepo;
    private readonly IProfileSystemRepository _systemRepo;

    public CreateHardwareKitCommandHandler(IHardwareKitRepository kitRepo, IProfileSystemRepository systemRepo)
    {
        _kitRepo = kitRepo;
        _systemRepo = systemRepo;
    }

    public async Task<HardwareKitDto> Handle(CreateHardwareKitCommand request, CancellationToken cancellationToken)
    {
        var existing = await _kitRepo.GetByCodeAsync(request.Data.Code, cancellationToken);
        if (existing is not null) throw new GlassEnclosureDuplicateCodeException("HardwareKit", request.Data.Code);

        var system = await _systemRepo.GetByIdAsync(request.Data.SystemId, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("ProfileSystem");

        var kit = new HardwareKit(request.Data.Code, request.Data.Name, system.Id, request.Data.Description);
        await _kitRepo.AddAsync(kit, cancellationToken);

        foreach (var item in request.Data.Items ?? Array.Empty<CreateHardwareKitItemDto>())
        {
            kit.AddItem(new HardwareKitItem(
                kit.Id, item.HardwareItemId, item.QuantityFormula,
                item.ConditionExpression, item.Note, item.SortOrder));
        }
        return GlassEnclosureMappers.ToDto(kit);
    }
}

public class UpdateHardwareKitCommandHandler : IRequestHandler<UpdateHardwareKitCommand, HardwareKitDto>
{
    private readonly IHardwareKitRepository _repository;
    public UpdateHardwareKitCommandHandler(IHardwareKitRepository repository) => _repository = repository;

    public async Task<HardwareKitDto> Handle(UpdateHardwareKitCommand request, CancellationToken cancellationToken)
    {
        var kit = await _repository.GetWithItemsAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("HardwareKit");
        kit.Update(request.Data.Name, request.Data.SystemId, request.Data.Description, request.Data.IsActive);

        foreach (var existingItem in kit.Items.ToList())
        {
            kit.RemoveItem(existingItem.Id);
        }
        foreach (var item in request.Data.Items ?? Array.Empty<CreateHardwareKitItemDto>())
        {
            kit.AddItem(new HardwareKitItem(
                kit.Id, item.HardwareItemId, item.QuantityFormula,
                item.ConditionExpression, item.Note, item.SortOrder));
        }
        _repository.Update(kit);
        return GlassEnclosureMappers.ToDto(kit);
    }
}

public class DeleteHardwareKitCommandHandler : IRequestHandler<DeleteHardwareKitCommand, Unit>
{
    private readonly IHardwareKitRepository _repository;
    public DeleteHardwareKitCommandHandler(IHardwareKitRepository repository) => _repository = repository;

    public async Task<Unit> Handle(DeleteHardwareKitCommand request, CancellationToken cancellationToken)
    {
        var kit = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("HardwareKit");
        _repository.Remove(kit);
        return Unit.Value;
    }
}

public class CreateBrandVendorCommandHandler : IRequestHandler<CreateBrandVendorCommand, BrandVendorDto>
{
    private readonly IBrandVendorRepository _repository;
    public CreateBrandVendorCommandHandler(IBrandVendorRepository repository) => _repository = repository;

    public async Task<BrandVendorDto> Handle(CreateBrandVendorCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByBrandAndVendorAsync(request.Data.BrandId, request.Data.VendorId, cancellationToken);
        if (existing is not null) throw new GlassEnclosureDuplicateCodeException("BrandVendor", $"{request.Data.BrandId}-{request.Data.VendorId}");

        var link = new BrandVendor(
            request.Data.BrandId, request.Data.VendorId,
            request.Data.DefaultLeadTimeDays, request.Data.IsPreferred, request.Data.DefaultPaymentTerms);
        await _repository.AddAsync(link, cancellationToken);
        return GlassEnclosureMappers.ToDto(link);
    }
}

public class UpdateBrandVendorCommandHandler : IRequestHandler<UpdateBrandVendorCommand, BrandVendorDto>
{
    private readonly IBrandVendorRepository _repository;
    public UpdateBrandVendorCommandHandler(IBrandVendorRepository repository) => _repository = repository;

    public async Task<BrandVendorDto> Handle(UpdateBrandVendorCommand request, CancellationToken cancellationToken)
    {
        var link = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("BrandVendor");
        link.Update(request.Data.DefaultLeadTimeDays, request.Data.IsPreferred,
            request.Data.DefaultPaymentTerms, request.Data.IsActive);
        _repository.Update(link);
        return GlassEnclosureMappers.ToDto(link);
    }
}

public class DeleteBrandVendorCommandHandler : IRequestHandler<DeleteBrandVendorCommand, Unit>
{
    private readonly IBrandVendorRepository _repository;
    public DeleteBrandVendorCommandHandler(IBrandVendorRepository repository) => _repository = repository;

    public async Task<Unit> Handle(DeleteBrandVendorCommand request, CancellationToken cancellationToken)
    {
        var link = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("BrandVendor");
        _repository.Remove(link);
        return Unit.Value;
    }
}

public class CreateDiscountRuleCommandHandler : IRequestHandler<CreateDiscountRuleCommand, DiscountRuleDto>
{
    private readonly IDiscountRuleRepository _repository;
    public CreateDiscountRuleCommandHandler(IDiscountRuleRepository repository) => _repository = repository;

    public async Task<DiscountRuleDto> Handle(CreateDiscountRuleCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByCodeAsync(request.Data.Code, cancellationToken);
        if (existing is not null) throw new GlassEnclosureDuplicateCodeException("DiscountRule", request.Data.Code);

        var rule = new DiscountRule(
            request.Data.Code, request.Data.Name, request.Data.Scope,
            request.Data.DiscountKind, request.Data.DiscountValue,
            request.Data.CustomerGroupId, request.Data.CouponCode, request.Data.MinAreaM2,
            request.Data.ValidFromUtc, request.Data.ValidUntilUtc,
            request.Data.Stackable, request.Data.Priority);
        await _repository.AddAsync(rule, cancellationToken);
        return GlassEnclosureMappers.ToDto(rule);
    }
}

public class UpdateDiscountRuleCommandHandler : IRequestHandler<UpdateDiscountRuleCommand, DiscountRuleDto>
{
    private readonly IDiscountRuleRepository _repository;
    public UpdateDiscountRuleCommandHandler(IDiscountRuleRepository repository) => _repository = repository;

    public async Task<DiscountRuleDto> Handle(UpdateDiscountRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("DiscountRule");
        rule.Update(
            request.Data.Name, request.Data.Scope, request.Data.DiscountKind, request.Data.DiscountValue,
            request.Data.CustomerGroupId, request.Data.CouponCode, request.Data.MinAreaM2,
            request.Data.ValidFromUtc, request.Data.ValidUntilUtc,
            request.Data.Stackable, request.Data.Priority, request.Data.IsActive);
        _repository.Update(rule);
        return GlassEnclosureMappers.ToDto(rule);
    }
}

public class DeleteDiscountRuleCommandHandler : IRequestHandler<DeleteDiscountRuleCommand, Unit>
{
    private readonly IDiscountRuleRepository _repository;
    public DeleteDiscountRuleCommandHandler(IDiscountRuleRepository repository) => _repository = repository;

    public async Task<Unit> Handle(DeleteDiscountRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("DiscountRule");
        _repository.Remove(rule);
        return Unit.Value;
    }
}

public class CreateGlassNotificationTemplateCommandHandler : IRequestHandler<CreateGlassNotificationTemplateCommand, GlassNotificationTemplateDto>
{
    private readonly IGlassNotificationTemplateRepository _repository;
    public CreateGlassNotificationTemplateCommandHandler(IGlassNotificationTemplateRepository repository) => _repository = repository;

    public async Task<GlassNotificationTemplateDto> Handle(CreateGlassNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = new GlassNotificationTemplate(
            request.Data.Code, request.Data.EventCode, request.Data.Channel, request.Data.Locale,
            request.Data.BodyTemplate, request.Data.SubjectTemplate);
        await _repository.AddAsync(template, cancellationToken);
        return GlassEnclosureMappers.ToDto(template);
    }
}

public class UpdateGlassNotificationTemplateCommandHandler : IRequestHandler<UpdateGlassNotificationTemplateCommand, GlassNotificationTemplateDto>
{
    private readonly IGlassNotificationTemplateRepository _repository;
    public UpdateGlassNotificationTemplateCommandHandler(IGlassNotificationTemplateRepository repository) => _repository = repository;

    public async Task<GlassNotificationTemplateDto> Handle(UpdateGlassNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("NotificationTemplate");
        template.Update(
            request.Data.EventCode, request.Data.Channel, request.Data.Locale,
            request.Data.SubjectTemplate, request.Data.BodyTemplate, request.Data.IsActive);
        _repository.Update(template);
        return GlassEnclosureMappers.ToDto(template);
    }
}

public class DeleteGlassNotificationTemplateCommandHandler : IRequestHandler<DeleteGlassNotificationTemplateCommand, Unit>
{
    private readonly IGlassNotificationTemplateRepository _repository;
    public DeleteGlassNotificationTemplateCommandHandler(IGlassNotificationTemplateRepository repository) => _repository = repository;

    public async Task<Unit> Handle(DeleteGlassNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new GlassEnclosureNotFoundException("NotificationTemplate");
        _repository.Remove(template);
        return Unit.Value;
    }
}
