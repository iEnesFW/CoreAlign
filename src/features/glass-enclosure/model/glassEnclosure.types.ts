export type InspectorSection = 'general' | 'dimensions' | 'hardware' | 'glass';

export type GlassSystemType =
  | 'Folding'
  | 'Sliding'
  | 'HeatInsulatedSliding'
  | 'Guillotine'
  | 'Hinged'
  | 'Fixed';

export type GlassOpeningType =
  | 'Fixed'
  | 'Folding'
  | 'SlidingLeft'
  | 'SlidingRight'
  | 'Hinged'
  | 'Guillotine';

export type ProfileRole =
  | 'Top'
  | 'Bottom'
  | 'SideJamb'
  | 'Mullion'
  | 'Sash'
  | 'Adapter'
  | 'DripRail'
  | 'Corner';

export type GlassStructure = 'Tempered' | 'Laminated' | 'DoubleGlazed' | 'TripleGlazed' | 'LowE';

export type HardwareCategoryKind =
  | 'Hinge'
  | 'Roller'
  | 'Lock'
  | 'Handle'
  | 'Gasket'
  | 'Brush'
  | 'Bumper'
  | 'WallBracket'
  | 'Chain'
  | 'DripCap'
  | 'CornerPost'
  | 'Other';

export type ColorFinishType = 'Anodized' | 'PowderCoated' | 'WoodLook' | 'Raw';

export type CorrosionClass = 'C1' | 'C2' | 'C3' | 'C4' | 'C5';

export type DiscountScope = 'CustomerGroup' | 'Coupon' | 'Volume' | 'DateRange' | 'Manual';
export type DiscountKind = 'Percent' | 'FixedAmount';

export type GlassNotificationChannel = 'Email' | 'Sms' | 'WhatsApp' | 'InApp';
export type GlassNotificationEventCode =
  | 'QuoteSent'
  | 'QuoteViewed'
  | 'QuoteAccepted'
  | 'QuoteRejected'
  | 'OrderConfirmed'
  | 'StockReserved'
  | 'ProductionStarted'
  | 'ProductionCompleted'
  | 'InTransit'
  | 'InstallationScheduled'
  | 'InstallationCompleted'
  | 'StockLow'
  | 'PaymentDue';

export interface WindZoneDto {
  id: string;
  code: string;
  regionLabelTr: string;
  regionLabelEn: string;
  baseWindPressurePa: number;
  heightFactorMultiplier: number;
  isCoastal: boolean;
  isActive: boolean;
}

export interface ClimateZoneDto {
  id: string;
  code: string;
  nameTr: string;
  nameEn: string;
  avgWinterTemperatureC: number;
  avgHumidityPercent: number;
  corrosionClass: CorrosionClass;
  recommendsDoubleGlazing: boolean;
  recommendsCorrosionResistantCoating: boolean;
  recommendsSeismicSmallerPanel: boolean;
  ilPostalPrefixes: string[];
  isActive: boolean;
}

export interface ClimateRecommendationDto {
  climateZoneId: string | null;
  climateZoneCode: string | null;
  climateZoneNameTr: string | null;
  climateZoneNameEn: string | null;
  corrosionClass: CorrosionClass | null;
  recommendsDoubleGlazing: boolean;
  recommendsCorrosionResistantCoating: boolean;
  recommendsSeismicSmallerPanel: boolean;
  notes: string[];
}

export interface ColorOptionDto {
  id: string;
  code: string;
  name: string;
  ralCode: string | null;
  hexColor: string;
  finishType: ColorFinishType;
  priceModifierPercent: number;
  sortOrder: number;
  isActive: boolean;
}

export interface CreateColorOptionInput {
  code: string;
  name: string;
  hexColor: string;
  finishType: ColorFinishType;
  ralCode?: string | null;
  priceModifierPercent: number;
  sortOrder: number;
}

export interface UpdateColorOptionInput {
  name: string;
  hexColor: string;
  finishType: ColorFinishType;
  ralCode?: string | null;
  priceModifierPercent: number;
  sortOrder: number;
  isActive: boolean;
}

export interface GlassTypeDto {
  id: string;
  code: string;
  name: string;
  thicknessMm: number;
  structure: GlassStructure;
  glassLayers: number[];
  uValue: number;
  soundDb: number;
  maxPanelAreaM2: number;
  allowablePressurePa: number;
  weightKgPerM2: number;
  pricePerM2: number;
  currency: string;
  linkedProductId: string | null;
  isActive: boolean;
}

export interface CreateGlassTypeInput {
  code: string;
  name: string;
  thicknessMm: number;
  structure: GlassStructure;
  pricePerM2: number;
  weightKgPerM2: number;
  allowablePressurePa: number;
  maxPanelAreaM2: number;
  uValue: number;
  soundDb: number;
  glassLayers?: number[];
  currency: string;
  linkedProductId?: string | null;
}

export interface UpdateGlassTypeInput extends Omit<CreateGlassTypeInput, 'code'> {
  isActive: boolean;
}

export interface ProfileItemDto {
  id: string;
  systemId: string;
  role: ProfileRole;
  code: string;
  name: string;
  stockBarLengthMm: number;
  weightKgPerMeter: number;
  pricePerKg: number;
  crossSectionSvg: string | null;
  crossSectionDxfUrl: string | null;
  parametricDescriptionJson: string | null;
  defaultColorId: string | null;
  preferredVendorId: string | null;
  vendorPartNumber: string | null;
  leadTimeDays: number;
  reorderPointMeters: number;
  currency: string;
  linkedProductId: string | null;
  isActive: boolean;
}

export interface ProfileSystemDto {
  id: string;
  code: string;
  name: string;
  brandId: string;
  brandName: string | null;
  systemType: GlassSystemType;
  maxPanelWidthMm: number;
  maxPanelHeightMm: number;
  maxPanelWeightKg: number;
  supportedGlassThicknesses: number[];
  supportedOpenings: GlassOpeningType[];
  certificationClass: string | null;
  fireClass: string | null;
  thermalUValue: number | null;
  thermalBreakFactor: number;
  description: string | null;
  isActive: boolean;
  items: ProfileItemDto[];
}

export interface CreateProfileSystemInput {
  code: string;
  name: string;
  brandId: string;
  systemType: GlassSystemType;
  maxPanelWidthMm: number;
  maxPanelHeightMm: number;
  maxPanelWeightKg: number;
  supportedGlassThicknesses: number[];
  supportedOpenings: GlassOpeningType[];
  certificationClass?: string | null;
  fireClass?: string | null;
  thermalUValue?: number | null;
  thermalBreakFactor: number;
  description?: string | null;
}

export interface UpdateProfileSystemInput extends Omit<CreateProfileSystemInput, 'code'> {
  isActive: boolean;
}

export interface CreateProfileItemInput {
  systemId: string;
  role: ProfileRole;
  code: string;
  name: string;
  stockBarLengthMm: number;
  weightKgPerMeter: number;
  pricePerKg: number;
  crossSectionSvg?: string | null;
  crossSectionDxfUrl?: string | null;
  parametricDescriptionJson?: string | null;
  defaultColorId?: string | null;
  preferredVendorId?: string | null;
  vendorPartNumber?: string | null;
  leadTimeDays: number;
  reorderPointMeters: number;
  currency: string;
  linkedProductId?: string | null;
}

export interface UpdateProfileItemInput extends Omit<CreateProfileItemInput, 'systemId' | 'code'> {
  isActive: boolean;
}

export interface HardwareItemDto {
  id: string;
  code: string;
  name: string;
  category: HardwareCategoryKind;
  brandId: string;
  brandName: string | null;
  unit: string;
  unitPrice: number;
  currency: string;
  maxLoadKg: number | null;
  compatibleSystemIds: string[];
  modelGlbUrl: string | null;
  preferredVendorId: string | null;
  vendorPartNumber: string | null;
  leadTimeDays: number;
  reorderPointQuantity: number;
  linkedProductId: string | null;
  isActive: boolean;
}

export interface CreateHardwareItemInput {
  code: string;
  name: string;
  category: HardwareCategoryKind;
  brandId: string;
  unit: string;
  unitPrice: number;
  compatibleSystemIds?: string[];
  maxLoadKg?: number | null;
  modelGlbUrl?: string | null;
  preferredVendorId?: string | null;
  vendorPartNumber?: string | null;
  leadTimeDays: number;
  reorderPointQuantity: number;
  currency: string;
  linkedProductId?: string | null;
}

export interface UpdateHardwareItemInput extends Omit<CreateHardwareItemInput, 'code'> {
  isActive: boolean;
}

export interface HardwareKitItemDto {
  id: string;
  kitId: string;
  hardwareItemId: string;
  hardwareItemName: string | null;
  quantityFormula: string;
  conditionExpression: string | null;
  note: string | null;
  sortOrder: number;
}

export interface HardwareKitDto {
  id: string;
  code: string;
  name: string;
  systemId: string;
  systemName: string | null;
  description: string | null;
  isActive: boolean;
  items: HardwareKitItemDto[];
}

export interface CreateHardwareKitItemInput {
  hardwareItemId: string;
  quantityFormula: string;
  conditionExpression?: string | null;
  note?: string | null;
  sortOrder: number;
}

export interface CreateHardwareKitInput {
  code: string;
  name: string;
  systemId: string;
  description?: string | null;
  items: CreateHardwareKitItemInput[];
}

export interface UpdateHardwareKitInput {
  name: string;
  systemId: string;
  description?: string | null;
  isActive: boolean;
  items: CreateHardwareKitItemInput[];
}

export interface BrandVendorDto {
  id: string;
  brandId: string;
  brandName: string | null;
  vendorId: string;
  vendorName: string | null;
  defaultLeadTimeDays: number;
  defaultPaymentTerms: string | null;
  isPreferred: boolean;
  isActive: boolean;
}

export interface CreateBrandVendorInput {
  brandId: string;
  vendorId: string;
  defaultLeadTimeDays: number;
  isPreferred: boolean;
  defaultPaymentTerms?: string | null;
}

export interface UpdateBrandVendorInput {
  defaultLeadTimeDays: number;
  isPreferred: boolean;
  defaultPaymentTerms?: string | null;
  isActive: boolean;
}

export interface DiscountRuleDto {
  id: string;
  code: string;
  name: string;
  scope: DiscountScope;
  customerGroupId: string | null;
  couponCode: string | null;
  minAreaM2: number | null;
  validFromUtc: string | null;
  validUntilUtc: string | null;
  discountKind: DiscountKind;
  discountValue: number;
  stackable: boolean;
  priority: number;
  isActive: boolean;
}

export interface CreateDiscountRuleInput {
  code: string;
  name: string;
  scope: DiscountScope;
  discountKind: DiscountKind;
  discountValue: number;
  customerGroupId?: string | null;
  couponCode?: string | null;
  minAreaM2?: number | null;
  validFromUtc?: string | null;
  validUntilUtc?: string | null;
  stackable: boolean;
  priority: number;
}

export interface UpdateDiscountRuleInput extends Omit<CreateDiscountRuleInput, 'code'> {
  isActive: boolean;
}

export interface GlassNotificationTemplateDto {
  id: string;
  code: string;
  eventCode: GlassNotificationEventCode;
  channel: GlassNotificationChannel;
  locale: string;
  subjectTemplate: string | null;
  bodyTemplate: string;
  isActive: boolean;
}

export interface CreateGlassNotificationTemplateInput {
  code: string;
  eventCode: GlassNotificationEventCode;
  channel: GlassNotificationChannel;
  locale: string;
  subjectTemplate?: string | null;
  bodyTemplate: string;
}

export interface UpdateGlassNotificationTemplateInput extends Omit<
  CreateGlassNotificationTemplateInput,
  'code'
> {
  isActive: boolean;
}

export interface GlassEnclosureSettingsDto {
  defaultStockBarLengthMm: number;
  defaultJumboGlassWidthMm: number;
  defaultJumboGlassHeightMm: number;
  sawKerfMm: number;
  glassKerfMm: number;
  guillotineRequired: boolean;
  defaultWastePercent: number;
  laborCostPerM2: number;
  defaultMarginPercent: number;
  fieldToleranceTopMm: number;
  fieldToleranceSideMm: number;
  transportRatePerKm: number;
  transportRatePerKg: number;
  scaffoldingRequiredFromFloor: number;
  scaffoldingRatePerM2: number;
  craneRequiredFromFloor: number;
  craneRatePerMeter: number;
  workshopDailyCapacityM2: number;
  defaultPaymentTerms: string[];
  defaultLocale: string;
  defaultCurrency: string;
  dataRetentionDays: number;
  whatsappBusinessPhoneId: string | null;
  notificationEmailFrom: string | null;
  quoteShareTokenTtlDays: number;
  onboardingComplete: boolean;
}

export interface UpdateSettingsCoreInput {
  defaultStockBarLengthMm: number;
  defaultJumboGlassWidthMm: number;
  defaultJumboGlassHeightMm: number;
  sawKerfMm: number;
  glassKerfMm: number;
  guillotineRequired: boolean;
  defaultWastePercent: number;
  laborCostPerM2: number;
  defaultMarginPercent: number;
}

export interface UpdateSettingsFieldInput {
  fieldToleranceTopMm: number;
  fieldToleranceSideMm: number;
}

export interface UpdateSettingsInstallationInput {
  transportRatePerKm: number;
  transportRatePerKg: number;
  scaffoldingRequiredFromFloor: number;
  scaffoldingRatePerM2: number;
  craneRequiredFromFloor: number;
  craneRatePerMeter: number;
  workshopDailyCapacityM2: number;
}

export interface UpdateSettingsLocaleInput {
  defaultLocale: string;
  defaultCurrency: string;
  defaultPaymentTerms: string[];
  whatsappBusinessPhoneId?: string | null;
  notificationEmailFrom?: string | null;
  quoteShareTokenTtlDays: number;
  dataRetentionDays: number;
}

export interface OnboardingStatusDto {
  isComplete: boolean;
  brandsSelected: boolean;
  workshopConfigured: boolean;
  demoSeeded: boolean;
  totalProfileSystems: number;
  totalGlassTypes: number;
  totalHardwareItems: number;
  totalColors: number;
}

export interface CompleteOnboardingInput {
  selectedBrandCodes: string[];
  seedDemoCatalog: boolean;
}
