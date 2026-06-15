using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.GlassEnclosure.Authorization;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Queries;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/glass-enclosure")]
public class GlassEnclosureCatalogController : ControllerBase
{
    private readonly IMediator _mediator;
    public GlassEnclosureCatalogController(IMediator mediator) => _mediator = mediator;

    [HttpGet("colors")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetColors([FromQuery] bool? isActive, CancellationToken ct) =>
        (await _mediator.Send(new GetColorOptionsQuery(isActive), ct)).ToOk();

    [HttpGet("colors/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetColorById(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new GetColorOptionByIdQuery(id), ct)).ToOk();

    [HttpPost("colors")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> CreateColor([FromBody] CreateColorOptionDto data, CancellationToken ct) =>
        (await _mediator.Send(new CreateColorOptionCommand(data), ct)).ToCreated();

    [HttpPut("colors/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> UpdateColor(Guid id, [FromBody] UpdateColorOptionDto data, CancellationToken ct) =>
        (await _mediator.Send(new UpdateColorOptionCommand(id, data), ct)).ToOk();

    [HttpDelete("colors/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> DeleteColor(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteColorOptionCommand(id), ct);
        return NoContent();
    }

    [HttpGet("glass-types")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetGlassTypes(
        [FromQuery] bool? isActive,
        [FromQuery] GlassStructure? structure,
        CancellationToken ct) =>
        (await _mediator.Send(new GetGlassTypesQuery(isActive, structure), ct)).ToOk();

    [HttpGet("glass-types/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetGlassTypeById(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new GetGlassTypeByIdQuery(id), ct)).ToOk();

    [HttpPost("glass-types")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> CreateGlassType([FromBody] CreateGlassTypeDto data, CancellationToken ct) =>
        (await _mediator.Send(new CreateGlassTypeCommand(data), ct)).ToCreated();

    [HttpPut("glass-types/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> UpdateGlassType(Guid id, [FromBody] UpdateGlassTypeDto data, CancellationToken ct) =>
        (await _mediator.Send(new UpdateGlassTypeCommand(id, data), ct)).ToOk();

    [HttpDelete("glass-types/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> DeleteGlassType(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteGlassTypeCommand(id), ct);
        return NoContent();
    }

    [HttpGet("profile-systems")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetProfileSystems(
        [FromQuery] bool? isActive,
        [FromQuery] Guid? brandId,
        [FromQuery] GlassSystemType? systemType,
        CancellationToken ct) =>
        (await _mediator.Send(new GetProfileSystemsQuery(isActive, brandId, systemType), ct)).ToOk();

    [HttpGet("profile-systems/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetProfileSystemById(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new GetProfileSystemByIdQuery(id), ct)).ToOk();

    [HttpPost("profile-systems")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> CreateProfileSystem([FromBody] CreateProfileSystemDto data, CancellationToken ct) =>
        (await _mediator.Send(new CreateProfileSystemCommand(data), ct)).ToCreated();

    [HttpPut("profile-systems/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> UpdateProfileSystem(Guid id, [FromBody] UpdateProfileSystemDto data, CancellationToken ct) =>
        (await _mediator.Send(new UpdateProfileSystemCommand(id, data), ct)).ToOk();

    [HttpDelete("profile-systems/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> DeleteProfileSystem(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteProfileSystemCommand(id), ct);
        return NoContent();
    }

    [HttpGet("profile-systems/{systemId:guid}/items")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetProfileItems(Guid systemId, [FromQuery] bool? isActive, CancellationToken ct) =>
        (await _mediator.Send(new GetProfileItemsBySystemQuery(systemId, isActive), ct)).ToOk();

    [HttpPost("profile-items")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> CreateProfileItem([FromBody] CreateProfileItemDto data, CancellationToken ct) =>
        (await _mediator.Send(new CreateProfileItemCommand(data), ct)).ToCreated();

    [HttpPut("profile-items/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> UpdateProfileItem(Guid id, [FromBody] UpdateProfileItemDto data, CancellationToken ct) =>
        (await _mediator.Send(new UpdateProfileItemCommand(id, data), ct)).ToOk();

    [HttpDelete("profile-items/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> DeleteProfileItem(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteProfileItemCommand(id), ct);
        return NoContent();
    }

    [HttpGet("hardware-items")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetHardwareItems(
        [FromQuery] bool? isActive,
        [FromQuery] HardwareCategoryKind? category,
        [FromQuery] Guid? compatibleSystemId,
        CancellationToken ct) =>
        (await _mediator.Send(new GetHardwareItemsQuery(isActive, category, compatibleSystemId), ct)).ToOk();

    [HttpGet("hardware-items/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetHardwareItemById(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new GetHardwareItemByIdQuery(id), ct)).ToOk();

    [HttpPost("hardware-items")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> CreateHardwareItem([FromBody] CreateHardwareItemDto data, CancellationToken ct) =>
        (await _mediator.Send(new CreateHardwareItemCommand(data), ct)).ToCreated();

    [HttpPut("hardware-items/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> UpdateHardwareItem(Guid id, [FromBody] UpdateHardwareItemDto data, CancellationToken ct) =>
        (await _mediator.Send(new UpdateHardwareItemCommand(id, data), ct)).ToOk();

    [HttpDelete("hardware-items/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> DeleteHardwareItem(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteHardwareItemCommand(id), ct);
        return NoContent();
    }

    [HttpGet("hardware-kits")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetHardwareKits(
        [FromQuery] bool? isActive,
        [FromQuery] Guid? systemId,
        CancellationToken ct) =>
        (await _mediator.Send(new GetHardwareKitsQuery(isActive, systemId), ct)).ToOk();

    [HttpGet("hardware-kits/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetHardwareKitById(Guid id, CancellationToken ct) =>
        (await _mediator.Send(new GetHardwareKitByIdQuery(id), ct)).ToOk();

    [HttpPost("hardware-kits")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> CreateHardwareKit([FromBody] CreateHardwareKitDto data, CancellationToken ct) =>
        (await _mediator.Send(new CreateHardwareKitCommand(data), ct)).ToCreated();

    [HttpPut("hardware-kits/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> UpdateHardwareKit(Guid id, [FromBody] UpdateHardwareKitDto data, CancellationToken ct) =>
        (await _mediator.Send(new UpdateHardwareKitCommand(id, data), ct)).ToOk();

    [HttpDelete("hardware-kits/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogUpdate)]
    public async Task<IActionResult> DeleteHardwareKit(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteHardwareKitCommand(id), ct);
        return NoContent();
    }

    [HttpGet("brand-vendors")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetBrandVendors(
        [FromQuery] bool? isActive,
        [FromQuery] Guid? brandId,
        CancellationToken ct) =>
        (await _mediator.Send(new GetBrandVendorsQuery(isActive, brandId), ct)).ToOk();

    [HttpPost("brand-vendors")]
    [Authorize(Policy = GlassEnclosurePolicies.BrandVendorUpdate)]
    public async Task<IActionResult> CreateBrandVendor([FromBody] CreateBrandVendorDto data, CancellationToken ct) =>
        (await _mediator.Send(new CreateBrandVendorCommand(data), ct)).ToCreated();

    [HttpPut("brand-vendors/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.BrandVendorUpdate)]
    public async Task<IActionResult> UpdateBrandVendor(Guid id, [FromBody] UpdateBrandVendorDto data, CancellationToken ct) =>
        (await _mediator.Send(new UpdateBrandVendorCommand(id, data), ct)).ToOk();

    [HttpDelete("brand-vendors/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.BrandVendorUpdate)]
    public async Task<IActionResult> DeleteBrandVendor(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteBrandVendorCommand(id), ct);
        return NoContent();
    }

    [HttpGet("discount-rules")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetDiscountRules(
        [FromQuery] bool? isActive,
        [FromQuery] DiscountScope? scope,
        CancellationToken ct) =>
        (await _mediator.Send(new GetDiscountRulesQuery(isActive, scope), ct)).ToOk();

    [HttpPost("discount-rules")]
    [Authorize(Policy = GlassEnclosurePolicies.DiscountRuleUpdate)]
    public async Task<IActionResult> CreateDiscountRule([FromBody] CreateDiscountRuleDto data, CancellationToken ct) =>
        (await _mediator.Send(new CreateDiscountRuleCommand(data), ct)).ToCreated();

    [HttpPut("discount-rules/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.DiscountRuleUpdate)]
    public async Task<IActionResult> UpdateDiscountRule(Guid id, [FromBody] UpdateDiscountRuleDto data, CancellationToken ct) =>
        (await _mediator.Send(new UpdateDiscountRuleCommand(id, data), ct)).ToOk();

    [HttpDelete("discount-rules/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.DiscountRuleUpdate)]
    public async Task<IActionResult> DeleteDiscountRule(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteDiscountRuleCommand(id), ct);
        return NoContent();
    }

    [HttpGet("notification-templates")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetNotificationTemplates(
        [FromQuery] bool? isActive,
        [FromQuery] GlassNotificationEventCode? eventCode,
        [FromQuery] GlassNotificationChannel? channel,
        [FromQuery] string? locale,
        CancellationToken ct) =>
        (await _mediator.Send(new GetGlassNotificationTemplatesQuery(isActive, eventCode, channel, locale), ct)).ToOk();

    [HttpPost("notification-templates")]
    [Authorize(Policy = GlassEnclosurePolicies.NotificationTemplateUpdate)]
    public async Task<IActionResult> CreateNotificationTemplate([FromBody] CreateGlassNotificationTemplateDto data, CancellationToken ct) =>
        (await _mediator.Send(new CreateGlassNotificationTemplateCommand(data), ct)).ToCreated();

    [HttpPut("notification-templates/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.NotificationTemplateUpdate)]
    public async Task<IActionResult> UpdateNotificationTemplate(Guid id, [FromBody] UpdateGlassNotificationTemplateDto data, CancellationToken ct) =>
        (await _mediator.Send(new UpdateGlassNotificationTemplateCommand(id, data), ct)).ToOk();

    [HttpDelete("notification-templates/{id:guid}")]
    [Authorize(Policy = GlassEnclosurePolicies.NotificationTemplateUpdate)]
    public async Task<IActionResult> DeleteNotificationTemplate(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteGlassNotificationTemplateCommand(id), ct);
        return NoContent();
    }

    [HttpGet("wind-zones")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetWindZones([FromQuery] bool? isActive, CancellationToken ct) =>
        (await _mediator.Send(new GetWindZonesQuery(isActive), ct)).ToOk();

    [HttpGet("climate-zones")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetClimateZones([FromQuery] bool? isActive, CancellationToken ct) =>
        (await _mediator.Send(new GetClimateZonesQuery(isActive), ct)).ToOk();

    [HttpGet("climate/recommendation")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetClimateRecommendation(
        [FromQuery] string? city,
        [FromQuery] string? postalCode,
        CancellationToken ct) =>
        (await _mediator.Send(new GetClimateRecommendationQuery(city, postalCode), ct)).ToOk();

    [HttpGet("settings")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetSettings(CancellationToken ct) =>
        (await _mediator.Send(new GetGlassEnclosureSettingsQuery(), ct)).ToOk();

    [HttpPut("settings/core")]
    [Authorize(Policy = GlassEnclosurePolicies.SettingsUpdate)]
    public async Task<IActionResult> UpdateSettingsCore([FromBody] UpdateGlassEnclosureSettingsCoreDto data, CancellationToken ct) =>
        (await _mediator.Send(new UpdateGlassEnclosureSettingsCoreCommand(data), ct)).ToOk();

    [HttpPut("settings/field")]
    [Authorize(Policy = GlassEnclosurePolicies.SettingsUpdate)]
    public async Task<IActionResult> UpdateSettingsField([FromBody] UpdateGlassEnclosureSettingsFieldDto data, CancellationToken ct) =>
        (await _mediator.Send(new UpdateGlassEnclosureSettingsFieldCommand(data), ct)).ToOk();

    [HttpPut("settings/installation")]
    [Authorize(Policy = GlassEnclosurePolicies.SettingsUpdate)]
    public async Task<IActionResult> UpdateSettingsInstallation([FromBody] UpdateGlassEnclosureSettingsInstallationDto data, CancellationToken ct) =>
        (await _mediator.Send(new UpdateGlassEnclosureSettingsInstallationCommand(data), ct)).ToOk();

    [HttpPut("settings/locale")]
    [Authorize(Policy = GlassEnclosurePolicies.SettingsUpdate)]
    public async Task<IActionResult> UpdateSettingsLocale([FromBody] UpdateGlassEnclosureSettingsLocaleDto data, CancellationToken ct) =>
        (await _mediator.Send(new UpdateGlassEnclosureSettingsLocaleCommand(data), ct)).ToOk();

    [HttpGet("onboarding/status")]
    [Authorize(Policy = GlassEnclosurePolicies.CatalogView)]
    public async Task<IActionResult> GetOnboardingStatus(CancellationToken ct) =>
        (await _mediator.Send(new GetOnboardingStatusQuery(), ct)).ToOk();

    [HttpPost("onboarding/complete")]
    [Authorize(Policy = GlassEnclosurePolicies.SettingsUpdate)]
    public async Task<IActionResult> CompleteOnboarding([FromBody] CompleteOnboardingDto data, CancellationToken ct) =>
        (await _mediator.Send(new CompleteOnboardingCommand(data), ct)).ToOk();
}
