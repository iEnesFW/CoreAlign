using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.DTOs;

public record WindZoneDto(
    Guid Id,
    string Code,
    string RegionLabelTr,
    string RegionLabelEn,
    decimal BaseWindPressurePa,
    decimal HeightFactorMultiplier,
    bool IsCoastal,
    bool IsActive);

public record ClimateZoneDto(
    Guid Id,
    string Code,
    string NameTr,
    string NameEn,
    decimal AvgWinterTemperatureC,
    decimal AvgHumidityPercent,
    CorrosionClass CorrosionClass,
    bool RecommendsDoubleGlazing,
    bool RecommendsCorrosionResistantCoating,
    bool RecommendsSeismicSmallerPanel,
    IReadOnlyList<string> IlPostalPrefixes,
    bool IsActive);

public record ClimateRecommendationDto(
    Guid? ClimateZoneId,
    string? ClimateZoneCode,
    string? ClimateZoneNameTr,
    string? ClimateZoneNameEn,
    CorrosionClass? CorrosionClass,
    bool RecommendsDoubleGlazing,
    bool RecommendsCorrosionResistantCoating,
    bool RecommendsSeismicSmallerPanel,
    IReadOnlyList<string> Notes);

public record ColorOptionDto(
    Guid Id,
    string Code,
    string Name,
    string? RalCode,
    string HexColor,
    ColorFinishType FinishType,
    decimal PriceModifierPercent,
    int SortOrder,
    bool IsActive);

public record CreateColorOptionDto(
    string Code,
    string Name,
    string HexColor,
    ColorFinishType FinishType,
    string? RalCode,
    decimal PriceModifierPercent,
    int SortOrder);

public record UpdateColorOptionDto(
    string Name,
    string HexColor,
    ColorFinishType FinishType,
    string? RalCode,
    decimal PriceModifierPercent,
    int SortOrder,
    bool IsActive);

public record GlassTypeDto(
    Guid Id,
    string Code,
    string Name,
    int ThicknessMm,
    GlassStructure Structure,
    IReadOnlyList<decimal> GlassLayers,
    decimal UValue,
    decimal SoundDb,
    decimal MaxPanelAreaM2,
    decimal AllowablePressurePa,
    decimal WeightKgPerM2,
    decimal PricePerM2,
    string Currency,
    Guid? LinkedProductId,
    bool IsActive);

public record CreateGlassTypeDto(
    string Code,
    string Name,
    int ThicknessMm,
    GlassStructure Structure,
    decimal PricePerM2,
    decimal WeightKgPerM2,
    decimal AllowablePressurePa,
    decimal MaxPanelAreaM2,
    decimal UValue,
    decimal SoundDb,
    IReadOnlyList<decimal>? GlassLayers,
    string Currency,
    Guid? LinkedProductId);

public record UpdateGlassTypeDto(
    string Name,
    int ThicknessMm,
    GlassStructure Structure,
    decimal PricePerM2,
    decimal WeightKgPerM2,
    decimal AllowablePressurePa,
    decimal MaxPanelAreaM2,
    decimal UValue,
    decimal SoundDb,
    IReadOnlyList<decimal>? GlassLayers,
    string Currency,
    Guid? LinkedProductId,
    bool IsActive);

public record ProfileItemDto(
    Guid Id,
    Guid SystemId,
    ProfileRole Role,
    string Code,
    string Name,
    int StockBarLengthMm,
    decimal WeightKgPerMeter,
    decimal PricePerKg,
    string? CrossSectionSvg,
    string? CrossSectionDxfUrl,
    string? ParametricDescriptionJson,
    Guid? DefaultColorId,
    Guid? PreferredVendorId,
    string? VendorPartNumber,
    int LeadTimeDays,
    decimal ReorderPointMeters,
    string Currency,
    Guid? LinkedProductId,
    bool IsActive);

public record ProfileSystemDto(
    Guid Id,
    string Code,
    string Name,
    Guid BrandId,
    string? BrandName,
    GlassSystemType SystemType,
    int MaxPanelWidthMm,
    int MaxPanelHeightMm,
    decimal MaxPanelWeightKg,
    IReadOnlyList<int> SupportedGlassThicknesses,
    IReadOnlyList<string> SupportedOpenings,
    string? CertificationClass,
    string? FireClass,
    decimal? ThermalUValue,
    decimal ThermalBreakFactor,
    string? Description,
    bool IsActive,
    IReadOnlyList<ProfileItemDto> Items);

public record CreateProfileSystemDto(
    string Code,
    string Name,
    Guid BrandId,
    GlassSystemType SystemType,
    int MaxPanelWidthMm,
    int MaxPanelHeightMm,
    decimal MaxPanelWeightKg,
    IReadOnlyList<int> SupportedGlassThicknesses,
    IReadOnlyList<GlassOpeningType> SupportedOpenings,
    string? CertificationClass,
    string? FireClass,
    decimal? ThermalUValue,
    decimal ThermalBreakFactor,
    string? Description);

public record UpdateProfileSystemDto(
    string Name,
    Guid BrandId,
    GlassSystemType SystemType,
    int MaxPanelWidthMm,
    int MaxPanelHeightMm,
    decimal MaxPanelWeightKg,
    IReadOnlyList<int> SupportedGlassThicknesses,
    IReadOnlyList<GlassOpeningType> SupportedOpenings,
    string? CertificationClass,
    string? FireClass,
    decimal? ThermalUValue,
    decimal ThermalBreakFactor,
    string? Description,
    bool IsActive);

public record CreateProfileItemDto(
    Guid SystemId,
    ProfileRole Role,
    string Code,
    string Name,
    int StockBarLengthMm,
    decimal WeightKgPerMeter,
    decimal PricePerKg,
    string? CrossSectionSvg,
    string? CrossSectionDxfUrl,
    string? ParametricDescriptionJson,
    Guid? DefaultColorId,
    Guid? PreferredVendorId,
    string? VendorPartNumber,
    int LeadTimeDays,
    decimal ReorderPointMeters,
    string Currency,
    Guid? LinkedProductId);

public record UpdateProfileItemDto(
    ProfileRole Role,
    string Name,
    int StockBarLengthMm,
    decimal WeightKgPerMeter,
    decimal PricePerKg,
    string? CrossSectionSvg,
    string? CrossSectionDxfUrl,
    string? ParametricDescriptionJson,
    Guid? DefaultColorId,
    Guid? PreferredVendorId,
    string? VendorPartNumber,
    int LeadTimeDays,
    decimal ReorderPointMeters,
    string Currency,
    Guid? LinkedProductId,
    bool IsActive);

public record HardwareItemDto(
    Guid Id,
    string Code,
    string Name,
    HardwareCategoryKind Category,
    Guid BrandId,
    string? BrandName,
    string Unit,
    decimal UnitPrice,
    string Currency,
    decimal? MaxLoadKg,
    IReadOnlyList<Guid> CompatibleSystemIds,
    string? ModelGlbUrl,
    Guid? PreferredVendorId,
    string? VendorPartNumber,
    int LeadTimeDays,
    decimal ReorderPointQuantity,
    Guid? LinkedProductId,
    bool IsActive);

public record CreateHardwareItemDto(
    string Code,
    string Name,
    HardwareCategoryKind Category,
    Guid BrandId,
    string Unit,
    decimal UnitPrice,
    IReadOnlyList<Guid>? CompatibleSystemIds,
    decimal? MaxLoadKg,
    string? ModelGlbUrl,
    Guid? PreferredVendorId,
    string? VendorPartNumber,
    int LeadTimeDays,
    decimal ReorderPointQuantity,
    string Currency,
    Guid? LinkedProductId);

public record UpdateHardwareItemDto(
    string Name,
    HardwareCategoryKind Category,
    Guid BrandId,
    string Unit,
    decimal UnitPrice,
    IReadOnlyList<Guid>? CompatibleSystemIds,
    decimal? MaxLoadKg,
    string? ModelGlbUrl,
    Guid? PreferredVendorId,
    string? VendorPartNumber,
    int LeadTimeDays,
    decimal ReorderPointQuantity,
    string Currency,
    Guid? LinkedProductId,
    bool IsActive);

public record HardwareKitItemDto(
    Guid Id,
    Guid KitId,
    Guid HardwareItemId,
    string? HardwareItemName,
    string QuantityFormula,
    string? ConditionExpression,
    string? Note,
    int SortOrder);

public record HardwareKitDto(
    Guid Id,
    string Code,
    string Name,
    Guid SystemId,
    string? SystemName,
    string? Description,
    bool IsActive,
    IReadOnlyList<HardwareKitItemDto> Items);

public record CreateHardwareKitItemDto(
    Guid HardwareItemId,
    string QuantityFormula,
    string? ConditionExpression,
    string? Note,
    int SortOrder);

public record CreateHardwareKitDto(
    string Code,
    string Name,
    Guid SystemId,
    string? Description,
    IReadOnlyList<CreateHardwareKitItemDto> Items);

public record UpdateHardwareKitDto(
    string Name,
    Guid SystemId,
    string? Description,
    bool IsActive,
    IReadOnlyList<CreateHardwareKitItemDto> Items);

public record BrandVendorDto(
    Guid Id,
    Guid BrandId,
    string? BrandName,
    Guid VendorId,
    string? VendorName,
    int DefaultLeadTimeDays,
    string? DefaultPaymentTerms,
    bool IsPreferred,
    bool IsActive);

public record CreateBrandVendorDto(
    Guid BrandId,
    Guid VendorId,
    int DefaultLeadTimeDays,
    bool IsPreferred,
    string? DefaultPaymentTerms);

public record UpdateBrandVendorDto(
    int DefaultLeadTimeDays,
    bool IsPreferred,
    string? DefaultPaymentTerms,
    bool IsActive);

public record DiscountRuleDto(
    Guid Id,
    string Code,
    string Name,
    DiscountScope Scope,
    Guid? CustomerGroupId,
    string? CouponCode,
    decimal? MinAreaM2,
    DateTime? ValidFromUtc,
    DateTime? ValidUntilUtc,
    DiscountKind DiscountKind,
    decimal DiscountValue,
    bool Stackable,
    int Priority,
    bool IsActive);

public record CreateDiscountRuleDto(
    string Code,
    string Name,
    DiscountScope Scope,
    DiscountKind DiscountKind,
    decimal DiscountValue,
    Guid? CustomerGroupId,
    string? CouponCode,
    decimal? MinAreaM2,
    DateTime? ValidFromUtc,
    DateTime? ValidUntilUtc,
    bool Stackable,
    int Priority);

public record UpdateDiscountRuleDto(
    string Name,
    DiscountScope Scope,
    DiscountKind DiscountKind,
    decimal DiscountValue,
    Guid? CustomerGroupId,
    string? CouponCode,
    decimal? MinAreaM2,
    DateTime? ValidFromUtc,
    DateTime? ValidUntilUtc,
    bool Stackable,
    int Priority,
    bool IsActive);

public record GlassNotificationTemplateDto(
    Guid Id,
    string Code,
    GlassNotificationEventCode EventCode,
    GlassNotificationChannel Channel,
    string Locale,
    string? SubjectTemplate,
    string BodyTemplate,
    bool IsActive);

public record CreateGlassNotificationTemplateDto(
    string Code,
    GlassNotificationEventCode EventCode,
    GlassNotificationChannel Channel,
    string Locale,
    string? SubjectTemplate,
    string BodyTemplate);

public record UpdateGlassNotificationTemplateDto(
    GlassNotificationEventCode EventCode,
    GlassNotificationChannel Channel,
    string Locale,
    string? SubjectTemplate,
    string BodyTemplate,
    bool IsActive);

public record GlassEnclosureSettingsDto(
    int DefaultStockBarLengthMm,
    int DefaultJumboGlassWidthMm,
    int DefaultJumboGlassHeightMm,
    decimal SawKerfMm,
    decimal GlassKerfMm,
    bool GuillotineRequired,
    decimal DefaultWastePercent,
    decimal LaborCostPerM2,
    decimal DefaultMarginPercent,
    decimal BendRailFeePerM,
    decimal BentGlassCostFactor,
    int FieldToleranceTopMm,
    int FieldToleranceSideMm,
    decimal TransportRatePerKm,
    decimal TransportRatePerKg,
    int ScaffoldingRequiredFromFloor,
    decimal ScaffoldingRatePerM2,
    int CraneRequiredFromFloor,
    decimal CraneRatePerMeter,
    decimal WorkshopDailyCapacityM2,
    IReadOnlyList<string> DefaultPaymentTerms,
    string DefaultLocale,
    string DefaultCurrency,
    int DataRetentionDays,
    string? WhatsappBusinessPhoneId,
    string? NotificationEmailFrom,
    int QuoteShareTokenTtlDays,
    bool OnboardingComplete);

public record UpdateGlassEnclosureSettingsCoreDto(
    int DefaultStockBarLengthMm,
    int DefaultJumboGlassWidthMm,
    int DefaultJumboGlassHeightMm,
    decimal SawKerfMm,
    decimal GlassKerfMm,
    bool GuillotineRequired,
    decimal DefaultWastePercent,
    decimal LaborCostPerM2,
    decimal DefaultMarginPercent,
    decimal BendRailFeePerM = 150m,
    decimal BentGlassCostFactor = 2.75m);

public record UpdateGlassEnclosureSettingsFieldDto(
    int FieldToleranceTopMm,
    int FieldToleranceSideMm);

public record UpdateGlassEnclosureSettingsInstallationDto(
    decimal TransportRatePerKm,
    decimal TransportRatePerKg,
    int ScaffoldingRequiredFromFloor,
    decimal ScaffoldingRatePerM2,
    int CraneRequiredFromFloor,
    decimal CraneRatePerMeter,
    decimal WorkshopDailyCapacityM2);

public record UpdateGlassEnclosureSettingsLocaleDto(
    string DefaultLocale,
    string DefaultCurrency,
    IReadOnlyList<string> DefaultPaymentTerms,
    string? WhatsappBusinessPhoneId,
    string? NotificationEmailFrom,
    int QuoteShareTokenTtlDays,
    int DataRetentionDays);

public record OnboardingStatusDto(
    bool IsComplete,
    bool BrandsSelected,
    bool WorkshopConfigured,
    bool DemoSeeded,
    int TotalProfileSystems,
    int TotalGlassTypes,
    int TotalHardwareItems,
    int TotalColors);

public record CompleteOnboardingDto(
    IReadOnlyList<string> SelectedBrandCodes,
    bool SeedDemoCatalog);
