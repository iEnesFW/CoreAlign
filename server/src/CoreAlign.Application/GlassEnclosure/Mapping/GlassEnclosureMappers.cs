using System.Text.Json;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.Mapping;

public static class GlassEnclosureMappers
{
    public static WindZoneDto ToDto(WindZone zone) => new(
        zone.Id, zone.Code, zone.RegionLabelTr, zone.RegionLabelEn,
        zone.BaseWindPressurePa, zone.HeightFactorMultiplier,
        zone.IsCoastal, zone.IsActive);

    public static ClimateZoneDto ToDto(ClimateZone zone) => new(
        zone.Id, zone.Code, zone.NameTr, zone.NameEn,
        zone.AvgWinterTemperatureC, zone.AvgHumidityPercent,
        zone.CorrosionClass,
        zone.RecommendsDoubleGlazing,
        zone.RecommendsCorrosionResistantCoating,
        zone.RecommendsSeismicSmallerPanel,
        DeserializeStringArray(zone.IlPostalPrefixListJson),
        zone.IsActive);

    public static ColorOptionDto ToDto(ColorOption color) => new(
        color.Id, color.Code, color.Name, color.RalCode, color.HexColor,
        color.FinishType, color.PriceModifierPercent, color.SortOrder, color.IsActive);

    public static GlassTypeDto ToDto(GlassType type) => new(
        type.Id, type.Code, type.Name, type.ThicknessMm, type.Structure,
        DeserializeDecimalArray(type.GlassLayersJson),
        type.UValue, type.SoundDb, type.MaxPanelAreaM2,
        type.AllowablePressurePa, type.WeightKgPerM2,
        type.PricePerM2, type.Currency, type.LinkedProductId, type.IsActive);

    public static ProfileItemDto ToDto(ProfileItem item) => new(
        item.Id, item.SystemId, item.Role, item.Code, item.Name,
        item.StockBarLengthMm, item.WeightKgPerMeter, item.PricePerKg,
        item.CrossSectionSvg, item.CrossSectionDxfUrl, item.ParametricDescriptionJson,
        item.DefaultColorId, item.PreferredVendorId, item.VendorPartNumber,
        item.LeadTimeDays, item.ReorderPointMeters,
        item.Currency, item.LinkedProductId, item.IsActive);

    public static ProfileSystemDto ToDto(ProfileSystem system, string? brandName = null) => new(
        system.Id, system.Code, system.Name, system.BrandId, brandName,
        system.SystemType, system.MaxPanelWidthMm, system.MaxPanelHeightMm, system.MaxPanelWeightKg,
        DeserializeIntArray(system.SupportedGlassThicknessesJson),
        DeserializeStringArray(system.SupportedOpeningsJson),
        system.CertificationClass, system.FireClass,
        system.ThermalUValue, system.ThermalBreakFactor,
        system.Description, system.IsActive,
        system.Items.Select(ToDto).ToList());

    public static HardwareItemDto ToDto(HardwareItem item, string? brandName = null) => new(
        item.Id, item.Code, item.Name, item.Category, item.BrandId, brandName,
        item.Unit, item.UnitPrice, item.Currency, item.MaxLoadKg,
        DeserializeGuidArray(item.CompatibleSystemIdsJson),
        item.ModelGlbUrl, item.PreferredVendorId, item.VendorPartNumber,
        item.LeadTimeDays, item.ReorderPointQuantity,
        item.LinkedProductId, item.IsActive);

    public static HardwareKitItemDto ToDto(HardwareKitItem item, string? hardwareItemName = null) => new(
        item.Id, item.KitId, item.HardwareItemId, hardwareItemName,
        item.QuantityFormula, item.ConditionExpression, item.Note, item.SortOrder);

    public static HardwareKitDto ToDto(HardwareKit kit, string? systemName = null) => new(
        kit.Id, kit.Code, kit.Name, kit.SystemId, systemName,
        kit.Description, kit.IsActive,
        kit.Items.Select(i => ToDto(i)).ToList());

    public static BrandVendorDto ToDto(BrandVendor link, string? brandName = null, string? vendorName = null) => new(
        link.Id, link.BrandId, brandName, link.VendorId, vendorName,
        link.DefaultLeadTimeDays, link.DefaultPaymentTerms, link.IsPreferred, link.IsActive);

    public static DiscountRuleDto ToDto(DiscountRule rule) => new(
        rule.Id, rule.Code, rule.Name, rule.Scope, rule.CustomerGroupId, rule.CouponCode,
        rule.MinAreaM2, rule.ValidFromUtc, rule.ValidUntilUtc,
        rule.DiscountKind, rule.DiscountValue, rule.Stackable, rule.Priority, rule.IsActive);

    public static GlassNotificationTemplateDto ToDto(GlassNotificationTemplate template) => new(
        template.Id, template.Code, template.EventCode, template.Channel, template.Locale,
        template.SubjectTemplate, template.BodyTemplate, template.IsActive);

    public static GlassEnclosureSettingsDto ToDto(GlassEnclosureSettings s) => new(
        s.DefaultStockBarLengthMm, s.DefaultJumboGlassWidthMm, s.DefaultJumboGlassHeightMm,
        s.SawKerfMm, s.GlassKerfMm, s.GuillotineRequired, s.DefaultWastePercent,
        s.LaborCostPerM2, s.DefaultMarginPercent, s.DefaultTaxRatePercent,
        s.BendRailFeePerM, s.BentGlassCostFactor,
        s.FieldToleranceTopMm, s.FieldToleranceSideMm,
        s.TransportRatePerKm, s.TransportRatePerKg,
        s.ScaffoldingRequiredFromFloor, s.ScaffoldingRatePerM2,
        s.CraneRequiredFromFloor, s.CraneRatePerMeter,
        s.WorkshopDailyCapacityM2,
        DeserializeStringArray(s.DefaultPaymentTermsJson),
        s.DefaultLocale, s.DefaultCurrency, s.DataRetentionDays,
        s.WhatsappBusinessPhoneId, s.NotificationEmailFrom,
        s.QuoteShareTokenTtlDays, s.OnboardingComplete);

    public static string SerializeIntArray(IEnumerable<int> values) =>
        JsonSerializer.Serialize(values?.ToArray() ?? Array.Empty<int>());

    public static string SerializeDecimalArray(IEnumerable<decimal> values) =>
        JsonSerializer.Serialize(values?.ToArray() ?? Array.Empty<decimal>());

    public static string SerializeStringArray(IEnumerable<string> values) =>
        JsonSerializer.Serialize(values?.ToArray() ?? Array.Empty<string>());

    public static string SerializeGuidArray(IEnumerable<Guid> values) =>
        JsonSerializer.Serialize(values?.ToArray() ?? Array.Empty<Guid>());

    public static string SerializeOpenings(IEnumerable<GlassOpeningType> openings) =>
        JsonSerializer.Serialize(openings?.Select(o => o.ToString()).ToArray() ?? Array.Empty<string>());

    private static IReadOnlyList<string> DeserializeStringArray(string json) =>
        SafeDeserialize<List<string>>(json) ?? new List<string>();

    private static IReadOnlyList<int> DeserializeIntArray(string json) =>
        SafeDeserialize<List<int>>(json) ?? new List<int>();

    private static IReadOnlyList<decimal> DeserializeDecimalArray(string json) =>
        SafeDeserialize<List<decimal>>(json) ?? new List<decimal>();

    private static IReadOnlyList<Guid> DeserializeGuidArray(string json) =>
        SafeDeserialize<List<Guid>>(json) ?? new List<Guid>();

    private static T? SafeDeserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
