using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Queries;

public record GetColorOptionsQuery(bool? IsActive = true) : IRequest<IReadOnlyList<ColorOptionDto>>;
public record GetColorOptionByIdQuery(Guid Id) : IRequest<ColorOptionDto?>;

public record GetGlassTypesQuery(bool? IsActive = true, GlassStructure? Structure = null) : IRequest<IReadOnlyList<GlassTypeDto>>;
public record GetGlassTypeByIdQuery(Guid Id) : IRequest<GlassTypeDto?>;

public record GetProfileSystemsQuery(bool? IsActive = true, Guid? BrandId = null, GlassSystemType? SystemType = null) : IRequest<IReadOnlyList<ProfileSystemDto>>;
public record GetProfileSystemByIdQuery(Guid Id) : IRequest<ProfileSystemDto?>;
public record GetProfileItemsBySystemQuery(Guid SystemId, bool? IsActive = true) : IRequest<IReadOnlyList<ProfileItemDto>>;

public record GetHardwareItemsQuery(bool? IsActive = true, HardwareCategoryKind? Category = null, Guid? CompatibleSystemId = null) : IRequest<IReadOnlyList<HardwareItemDto>>;
public record GetHardwareItemByIdQuery(Guid Id) : IRequest<HardwareItemDto?>;

public record GetHardwareKitsQuery(bool? IsActive = true, Guid? SystemId = null) : IRequest<IReadOnlyList<HardwareKitDto>>;
public record GetHardwareKitByIdQuery(Guid Id) : IRequest<HardwareKitDto?>;

public record GetBrandVendorsQuery(bool? IsActive = true, Guid? BrandId = null) : IRequest<IReadOnlyList<BrandVendorDto>>;
public record GetBrandVendorByIdQuery(Guid Id) : IRequest<BrandVendorDto?>;

public record GetDiscountRulesQuery(bool? IsActive = true, DiscountScope? Scope = null) : IRequest<IReadOnlyList<DiscountRuleDto>>;
public record GetDiscountRuleByIdQuery(Guid Id) : IRequest<DiscountRuleDto?>;

public record GetGlassNotificationTemplatesQuery(
    bool? IsActive = true,
    GlassNotificationEventCode? EventCode = null,
    GlassNotificationChannel? Channel = null,
    string? Locale = null) : IRequest<IReadOnlyList<GlassNotificationTemplateDto>>;

public record GetGlassNotificationTemplateByIdQuery(Guid Id) : IRequest<GlassNotificationTemplateDto?>;

public record GetGlassEnclosureSettingsQuery : IRequest<GlassEnclosureSettingsDto>;

public record GetOnboardingStatusQuery : IRequest<OnboardingStatusDto>;
