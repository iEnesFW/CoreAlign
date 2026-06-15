import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type {
  BrandVendorDto,
  ClimateRecommendationDto,
  ClimateZoneDto,
  ColorOptionDto,
  CompleteOnboardingInput,
  CreateBrandVendorInput,
  CreateColorOptionInput,
  CreateDiscountRuleInput,
  CreateGlassNotificationTemplateInput,
  CreateGlassTypeInput,
  CreateHardwareItemInput,
  CreateHardwareKitInput,
  CreateProfileItemInput,
  CreateProfileSystemInput,
  DiscountRuleDto,
  GlassEnclosureSettingsDto,
  GlassNotificationTemplateDto,
  GlassStructure,
  GlassSystemType,
  GlassTypeDto,
  HardwareCategoryKind,
  HardwareItemDto,
  HardwareKitDto,
  OnboardingStatusDto,
  ProfileItemDto,
  ProfileSystemDto,
  UpdateBrandVendorInput,
  UpdateColorOptionInput,
  UpdateDiscountRuleInput,
  UpdateGlassNotificationTemplateInput,
  UpdateGlassTypeInput,
  UpdateHardwareItemInput,
  UpdateHardwareKitInput,
  UpdateProfileItemInput,
  UpdateProfileSystemInput,
  UpdateSettingsCoreInput,
  UpdateSettingsFieldInput,
  UpdateSettingsInstallationInput,
  UpdateSettingsLocaleInput,
  WindZoneDto,
} from '../model/glassEnclosure.types';

const BASE = '/glass-enclosure';
const INVALIDATION = [/\/glass-enclosure/i] as const;

const post = <T, U = unknown>(path: string, body: U) =>
  apiClient.post<ApiResponse<T>>(`${BASE}${path}`, body).then((r) => {
    invalidateHttpCache(INVALIDATION);
    return r.data;
  });

const put = <T, U = unknown>(path: string, body: U) =>
  apiClient.put<ApiResponse<T>>(`${BASE}${path}`, body).then((r) => {
    invalidateHttpCache(INVALIDATION);
    return r.data;
  });

const del = (path: string) =>
  apiClient.delete(`${BASE}${path}`).then((r) => {
    invalidateHttpCache(INVALIDATION);
    return r.data;
  });

export const glassEnclosureCatalogApi = {
  listColors: (isActive?: boolean) =>
    cachedGet<ApiResponse<ColorOptionDto[]>>(apiClient, `${BASE}/colors`, { params: { isActive } }),
  getColor: (id: string) =>
    cachedGet<ApiResponse<ColorOptionDto>>(apiClient, `${BASE}/colors/${id}`),
  createColor: (input: CreateColorOptionInput) => post<ColorOptionDto>('/colors', input),
  updateColor: (id: string, input: UpdateColorOptionInput) =>
    put<ColorOptionDto>(`/colors/${id}`, input),
  deleteColor: (id: string) => del(`/colors/${id}`),

  listGlassTypes: (params?: { isActive?: boolean; structure?: GlassStructure }) =>
    cachedGet<ApiResponse<GlassTypeDto[]>>(apiClient, `${BASE}/glass-types`, { params }),
  getGlassType: (id: string) =>
    cachedGet<ApiResponse<GlassTypeDto>>(apiClient, `${BASE}/glass-types/${id}`),
  createGlassType: (input: CreateGlassTypeInput) => post<GlassTypeDto>('/glass-types', input),
  updateGlassType: (id: string, input: UpdateGlassTypeInput) =>
    put<GlassTypeDto>(`/glass-types/${id}`, input),
  deleteGlassType: (id: string) => del(`/glass-types/${id}`),

  listProfileSystems: (params?: {
    isActive?: boolean;
    brandId?: string;
    systemType?: GlassSystemType;
  }) =>
    cachedGet<ApiResponse<ProfileSystemDto[]>>(apiClient, `${BASE}/profile-systems`, { params }),
  getProfileSystem: (id: string) =>
    cachedGet<ApiResponse<ProfileSystemDto>>(apiClient, `${BASE}/profile-systems/${id}`),
  createProfileSystem: (input: CreateProfileSystemInput) =>
    post<ProfileSystemDto>('/profile-systems', input),
  updateProfileSystem: (id: string, input: UpdateProfileSystemInput) =>
    put<ProfileSystemDto>(`/profile-systems/${id}`, input),
  deleteProfileSystem: (id: string) => del(`/profile-systems/${id}`),

  listProfileItems: (systemId: string, isActive?: boolean) =>
    cachedGet<ApiResponse<ProfileItemDto[]>>(
      apiClient,
      `${BASE}/profile-systems/${systemId}/items`,
      {
        params: { isActive },
      },
    ),
  createProfileItem: (input: CreateProfileItemInput) =>
    post<ProfileItemDto>('/profile-items', input),
  updateProfileItem: (id: string, input: UpdateProfileItemInput) =>
    put<ProfileItemDto>(`/profile-items/${id}`, input),
  deleteProfileItem: (id: string) => del(`/profile-items/${id}`),

  listHardwareItems: (params?: {
    isActive?: boolean;
    category?: HardwareCategoryKind;
    compatibleSystemId?: string;
  }) => cachedGet<ApiResponse<HardwareItemDto[]>>(apiClient, `${BASE}/hardware-items`, { params }),
  getHardwareItem: (id: string) =>
    cachedGet<ApiResponse<HardwareItemDto>>(apiClient, `${BASE}/hardware-items/${id}`),
  createHardwareItem: (input: CreateHardwareItemInput) =>
    post<HardwareItemDto>('/hardware-items', input),
  updateHardwareItem: (id: string, input: UpdateHardwareItemInput) =>
    put<HardwareItemDto>(`/hardware-items/${id}`, input),
  deleteHardwareItem: (id: string) => del(`/hardware-items/${id}`),

  listHardwareKits: (params?: { isActive?: boolean; systemId?: string }) =>
    cachedGet<ApiResponse<HardwareKitDto[]>>(apiClient, `${BASE}/hardware-kits`, { params }),
  getHardwareKit: (id: string) =>
    cachedGet<ApiResponse<HardwareKitDto>>(apiClient, `${BASE}/hardware-kits/${id}`),
  createHardwareKit: (input: CreateHardwareKitInput) =>
    post<HardwareKitDto>('/hardware-kits', input),
  updateHardwareKit: (id: string, input: UpdateHardwareKitInput) =>
    put<HardwareKitDto>(`/hardware-kits/${id}`, input),
  deleteHardwareKit: (id: string) => del(`/hardware-kits/${id}`),

  listBrandVendors: (params?: { isActive?: boolean; brandId?: string }) =>
    cachedGet<ApiResponse<BrandVendorDto[]>>(apiClient, `${BASE}/brand-vendors`, { params }),
  createBrandVendor: (input: CreateBrandVendorInput) =>
    post<BrandVendorDto>('/brand-vendors', input),
  updateBrandVendor: (id: string, input: UpdateBrandVendorInput) =>
    put<BrandVendorDto>(`/brand-vendors/${id}`, input),
  deleteBrandVendor: (id: string) => del(`/brand-vendors/${id}`),

  listDiscountRules: (params?: { isActive?: boolean }) =>
    cachedGet<ApiResponse<DiscountRuleDto[]>>(apiClient, `${BASE}/discount-rules`, { params }),
  createDiscountRule: (input: CreateDiscountRuleInput) =>
    post<DiscountRuleDto>('/discount-rules', input),
  updateDiscountRule: (id: string, input: UpdateDiscountRuleInput) =>
    put<DiscountRuleDto>(`/discount-rules/${id}`, input),
  deleteDiscountRule: (id: string) => del(`/discount-rules/${id}`),

  listNotificationTemplates: (params?: { isActive?: boolean; locale?: string }) =>
    cachedGet<ApiResponse<GlassNotificationTemplateDto[]>>(
      apiClient,
      `${BASE}/notification-templates`,
      {
        params,
      },
    ),
  createNotificationTemplate: (input: CreateGlassNotificationTemplateInput) =>
    post<GlassNotificationTemplateDto>('/notification-templates', input),
  updateNotificationTemplate: (id: string, input: UpdateGlassNotificationTemplateInput) =>
    put<GlassNotificationTemplateDto>(`/notification-templates/${id}`, input),
  deleteNotificationTemplate: (id: string) => del(`/notification-templates/${id}`),

  listWindZones: (isActive?: boolean) =>
    cachedGet<ApiResponse<WindZoneDto[]>>(apiClient, `${BASE}/wind-zones`, {
      params: { isActive },
    }),
  listClimateZones: (isActive?: boolean) =>
    cachedGet<ApiResponse<ClimateZoneDto[]>>(apiClient, `${BASE}/climate-zones`, {
      params: { isActive },
    }),
  getClimateRecommendation: (city?: string, postalCode?: string) =>
    cachedGet<ApiResponse<ClimateRecommendationDto>>(apiClient, `${BASE}/climate/recommendation`, {
      params: { city, postalCode },
    }),

  getSettings: () =>
    cachedGet<ApiResponse<GlassEnclosureSettingsDto>>(apiClient, `${BASE}/settings`),
  updateSettingsCore: (input: UpdateSettingsCoreInput) =>
    put<GlassEnclosureSettingsDto>('/settings/core', input),
  updateSettingsField: (input: UpdateSettingsFieldInput) =>
    put<GlassEnclosureSettingsDto>('/settings/field', input),
  updateSettingsInstallation: (input: UpdateSettingsInstallationInput) =>
    put<GlassEnclosureSettingsDto>('/settings/installation', input),
  updateSettingsLocale: (input: UpdateSettingsLocaleInput) =>
    put<GlassEnclosureSettingsDto>('/settings/locale', input),

  getOnboardingStatus: () =>
    cachedGet<ApiResponse<OnboardingStatusDto>>(apiClient, `${BASE}/onboarding/status`),
  completeOnboarding: (input: CompleteOnboardingInput) =>
    post<OnboardingStatusDto>('/onboarding/complete', input),
};
