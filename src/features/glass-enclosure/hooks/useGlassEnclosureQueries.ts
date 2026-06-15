import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { glassEnclosureCatalogApi } from '../api/glassEnclosureCatalogApi';
import { glassEnclosureKeys } from './glassEnclosureKeys';
import type {
  GlassStructure,
  GlassSystemType,
  HardwareCategoryKind,
} from '../model/glassEnclosure.types';

export const useColorOptionsQuery = (isActive: boolean | undefined = true) =>
  useQuery({
    queryKey: [...glassEnclosureKeys.colors(), { isActive }],
    queryFn: () => glassEnclosureCatalogApi.listColors(isActive),
  });

export const useGlassTypesQuery = (
  params: { isActive?: boolean; structure?: GlassStructure } = {},
) =>
  useQuery({
    queryKey: [...glassEnclosureKeys.glassTypes(), params],
    queryFn: () => glassEnclosureCatalogApi.listGlassTypes(params),
  });

export const useProfileSystemsQuery = (
  params: { isActive?: boolean; brandId?: string; systemType?: GlassSystemType } = {},
) =>
  useQuery({
    queryKey: [...glassEnclosureKeys.profileSystems(), params],
    queryFn: () => glassEnclosureCatalogApi.listProfileSystems(params),
  });

export const useProfileSystemQuery = (id: string | null) =>
  useQuery({
    queryKey: glassEnclosureKeys.profileSystem(id),
    queryFn: () => glassEnclosureCatalogApi.getProfileSystem(id as string),
    enabled: id !== null,
  });

export const useProfileItemsBySystemQuery = (
  systemId: string | null,
  isActive: boolean | undefined = true,
) =>
  useQuery({
    queryKey: [...glassEnclosureKeys.profileItems(systemId), { isActive }],
    queryFn: () => glassEnclosureCatalogApi.listProfileItems(systemId as string, isActive),
    enabled: systemId !== null,
  });

export const useHardwareItemsQuery = (
  params: { isActive?: boolean; category?: HardwareCategoryKind; compatibleSystemId?: string } = {},
) =>
  useQuery({
    queryKey: [...glassEnclosureKeys.hardwareItems(), params],
    queryFn: () => glassEnclosureCatalogApi.listHardwareItems(params),
  });

export const useHardwareKitsQuery = (params: { isActive?: boolean; systemId?: string } = {}) =>
  useQuery({
    queryKey: [...glassEnclosureKeys.hardwareKits(), params],
    queryFn: () => glassEnclosureCatalogApi.listHardwareKits(params),
  });

export const useBrandVendorsQuery = (params: { isActive?: boolean; brandId?: string } = {}) =>
  useQuery({
    queryKey: [...glassEnclosureKeys.brandVendors(), params],
    queryFn: () => glassEnclosureCatalogApi.listBrandVendors(params),
  });

export const useDiscountRulesQuery = (isActive: boolean | undefined = true) =>
  useQuery({
    queryKey: [...glassEnclosureKeys.discountRules(), { isActive }],
    queryFn: () => glassEnclosureCatalogApi.listDiscountRules({ isActive }),
  });

export const useNotificationTemplatesQuery = (
  params: { isActive?: boolean; locale?: string } = {},
) =>
  useQuery({
    queryKey: [...glassEnclosureKeys.notificationTemplates(), params],
    queryFn: () => glassEnclosureCatalogApi.listNotificationTemplates(params),
  });

export const useWindZonesQuery = (isActive: boolean | undefined = true) =>
  useQuery({
    queryKey: [...glassEnclosureKeys.windZones(), { isActive }],
    queryFn: () => glassEnclosureCatalogApi.listWindZones(isActive),
  });

export const useClimateZonesQuery = (isActive: boolean | undefined = true) =>
  useQuery({
    queryKey: [...glassEnclosureKeys.climateZones(), { isActive }],
    queryFn: () => glassEnclosureCatalogApi.listClimateZones(isActive),
  });

export const useClimateRecommendationQuery = (city: string | null, postalCode: string | null) =>
  useQuery({
    queryKey: glassEnclosureKeys.climateRecommendation(city, postalCode),
    queryFn: () =>
      glassEnclosureCatalogApi.getClimateRecommendation(city ?? undefined, postalCode ?? undefined),
    enabled: city !== null || postalCode !== null,
  });

export const useSettingsQuery = () =>
  useQuery({
    queryKey: glassEnclosureKeys.settings(),
    queryFn: () => glassEnclosureCatalogApi.getSettings(),
  });

export const useOnboardingStatusQuery = () =>
  useQuery({
    queryKey: glassEnclosureKeys.onboarding(),
    queryFn: () => glassEnclosureCatalogApi.getOnboardingStatus(),
  });

const invalidateAll = (qc: ReturnType<typeof useQueryClient>) =>
  qc.invalidateQueries({ queryKey: glassEnclosureKeys.all });

export const useCreateColorMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassEnclosureCatalogApi.createColor,
    onSuccess: () => invalidateAll(qc),
  });
};

export const useUpdateColorMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      input,
    }: {
      id: string;
      input: Parameters<typeof glassEnclosureCatalogApi.updateColor>[1];
    }) => glassEnclosureCatalogApi.updateColor(id, input),
    onSuccess: () => invalidateAll(qc),
  });
};

export const useDeleteColorMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassEnclosureCatalogApi.deleteColor,
    onSuccess: () => invalidateAll(qc),
  });
};

export const useCreateGlassTypeMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassEnclosureCatalogApi.createGlassType,
    onSuccess: () => invalidateAll(qc),
  });
};

export const useUpdateGlassTypeMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      input,
    }: {
      id: string;
      input: Parameters<typeof glassEnclosureCatalogApi.updateGlassType>[1];
    }) => glassEnclosureCatalogApi.updateGlassType(id, input),
    onSuccess: () => invalidateAll(qc),
  });
};

export const useDeleteGlassTypeMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassEnclosureCatalogApi.deleteGlassType,
    onSuccess: () => invalidateAll(qc),
  });
};

export const useCreateProfileSystemMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassEnclosureCatalogApi.createProfileSystem,
    onSuccess: () => invalidateAll(qc),
  });
};

export const useUpdateProfileSystemMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      input,
    }: {
      id: string;
      input: Parameters<typeof glassEnclosureCatalogApi.updateProfileSystem>[1];
    }) => glassEnclosureCatalogApi.updateProfileSystem(id, input),
    onSuccess: () => invalidateAll(qc),
  });
};

export const useDeleteProfileSystemMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassEnclosureCatalogApi.deleteProfileSystem,
    onSuccess: () => invalidateAll(qc),
  });
};

export const useCreateProfileItemMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassEnclosureCatalogApi.createProfileItem,
    onSuccess: () => invalidateAll(qc),
  });
};

export const useUpdateProfileItemMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      input,
    }: {
      id: string;
      input: Parameters<typeof glassEnclosureCatalogApi.updateProfileItem>[1];
    }) => glassEnclosureCatalogApi.updateProfileItem(id, input),
    onSuccess: () => invalidateAll(qc),
  });
};

export const useDeleteProfileItemMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassEnclosureCatalogApi.deleteProfileItem,
    onSuccess: () => invalidateAll(qc),
  });
};

export const useCreateHardwareItemMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassEnclosureCatalogApi.createHardwareItem,
    onSuccess: () => invalidateAll(qc),
  });
};

export const useUpdateHardwareItemMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      input,
    }: {
      id: string;
      input: Parameters<typeof glassEnclosureCatalogApi.updateHardwareItem>[1];
    }) => glassEnclosureCatalogApi.updateHardwareItem(id, input),
    onSuccess: () => invalidateAll(qc),
  });
};

export const useDeleteHardwareItemMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassEnclosureCatalogApi.deleteHardwareItem,
    onSuccess: () => invalidateAll(qc),
  });
};

export const useUpdateSettingsCoreMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassEnclosureCatalogApi.updateSettingsCore,
    onSuccess: () => invalidateAll(qc),
  });
};

export const useUpdateSettingsFieldMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassEnclosureCatalogApi.updateSettingsField,
    onSuccess: () => invalidateAll(qc),
  });
};

export const useUpdateSettingsInstallationMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassEnclosureCatalogApi.updateSettingsInstallation,
    onSuccess: () => invalidateAll(qc),
  });
};

export const useUpdateSettingsLocaleMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassEnclosureCatalogApi.updateSettingsLocale,
    onSuccess: () => invalidateAll(qc),
  });
};

export const useCompleteOnboardingMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassEnclosureCatalogApi.completeOnboarding,
    onSuccess: () => invalidateAll(qc),
  });
};
