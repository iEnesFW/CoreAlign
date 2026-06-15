using CoreAlign.Application.Common;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Commands;

public record CreateColorOptionCommand(CreateColorOptionDto Data) : IRequest<ColorOptionDto>, ITransactionalRequest;
public record UpdateColorOptionCommand(Guid Id, UpdateColorOptionDto Data) : IRequest<ColorOptionDto>, ITransactionalRequest;
public record DeleteColorOptionCommand(Guid Id) : IRequest<Unit>, ITransactionalRequest;

public record CreateGlassTypeCommand(CreateGlassTypeDto Data) : IRequest<GlassTypeDto>, ITransactionalRequest;
public record UpdateGlassTypeCommand(Guid Id, UpdateGlassTypeDto Data) : IRequest<GlassTypeDto>, ITransactionalRequest;
public record DeleteGlassTypeCommand(Guid Id) : IRequest<Unit>, ITransactionalRequest;

public record CreateProfileSystemCommand(CreateProfileSystemDto Data) : IRequest<ProfileSystemDto>, ITransactionalRequest;
public record UpdateProfileSystemCommand(Guid Id, UpdateProfileSystemDto Data) : IRequest<ProfileSystemDto>, ITransactionalRequest;
public record DeleteProfileSystemCommand(Guid Id) : IRequest<Unit>, ITransactionalRequest;

public record CreateProfileItemCommand(CreateProfileItemDto Data) : IRequest<ProfileItemDto>, ITransactionalRequest;
public record UpdateProfileItemCommand(Guid Id, UpdateProfileItemDto Data) : IRequest<ProfileItemDto>, ITransactionalRequest;
public record DeleteProfileItemCommand(Guid Id) : IRequest<Unit>, ITransactionalRequest;

public record CreateHardwareItemCommand(CreateHardwareItemDto Data) : IRequest<HardwareItemDto>, ITransactionalRequest;
public record UpdateHardwareItemCommand(Guid Id, UpdateHardwareItemDto Data) : IRequest<HardwareItemDto>, ITransactionalRequest;
public record DeleteHardwareItemCommand(Guid Id) : IRequest<Unit>, ITransactionalRequest;

public record CreateHardwareKitCommand(CreateHardwareKitDto Data) : IRequest<HardwareKitDto>, ITransactionalRequest;
public record UpdateHardwareKitCommand(Guid Id, UpdateHardwareKitDto Data) : IRequest<HardwareKitDto>, ITransactionalRequest;
public record DeleteHardwareKitCommand(Guid Id) : IRequest<Unit>, ITransactionalRequest;

public record CreateBrandVendorCommand(CreateBrandVendorDto Data) : IRequest<BrandVendorDto>, ITransactionalRequest;
public record UpdateBrandVendorCommand(Guid Id, UpdateBrandVendorDto Data) : IRequest<BrandVendorDto>, ITransactionalRequest;
public record DeleteBrandVendorCommand(Guid Id) : IRequest<Unit>, ITransactionalRequest;

public record CreateDiscountRuleCommand(CreateDiscountRuleDto Data) : IRequest<DiscountRuleDto>, ITransactionalRequest;
public record UpdateDiscountRuleCommand(Guid Id, UpdateDiscountRuleDto Data) : IRequest<DiscountRuleDto>, ITransactionalRequest;
public record DeleteDiscountRuleCommand(Guid Id) : IRequest<Unit>, ITransactionalRequest;

public record CreateGlassNotificationTemplateCommand(CreateGlassNotificationTemplateDto Data) : IRequest<GlassNotificationTemplateDto>, ITransactionalRequest;
public record UpdateGlassNotificationTemplateCommand(Guid Id, UpdateGlassNotificationTemplateDto Data) : IRequest<GlassNotificationTemplateDto>, ITransactionalRequest;
public record DeleteGlassNotificationTemplateCommand(Guid Id) : IRequest<Unit>, ITransactionalRequest;

public record UpdateGlassEnclosureSettingsCoreCommand(UpdateGlassEnclosureSettingsCoreDto Data) : IRequest<GlassEnclosureSettingsDto>, ITransactionalRequest;
public record UpdateGlassEnclosureSettingsFieldCommand(UpdateGlassEnclosureSettingsFieldDto Data) : IRequest<GlassEnclosureSettingsDto>, ITransactionalRequest;
public record UpdateGlassEnclosureSettingsInstallationCommand(UpdateGlassEnclosureSettingsInstallationDto Data) : IRequest<GlassEnclosureSettingsDto>, ITransactionalRequest;
public record UpdateGlassEnclosureSettingsLocaleCommand(UpdateGlassEnclosureSettingsLocaleDto Data) : IRequest<GlassEnclosureSettingsDto>, ITransactionalRequest;
public record CompleteOnboardingCommand(CompleteOnboardingDto Data) : IRequest<OnboardingStatusDto>, ITransactionalRequest;
