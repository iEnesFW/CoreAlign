import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { whitelabelApi } from '../api/whitelabelApi';
import type { TenantThemeAssetKind, UpdateTenantThemeInput } from '../model/whitelabel.types';

const TENANT_THEME_KEY = ['whitelabel', 'tenant-theme'] as const;

export const useTenantThemeQuery = (enabled = true) =>
  useQuery({
    queryKey: TENANT_THEME_KEY,
    queryFn: () => whitelabelApi.getTheme(),
    enabled,
    staleTime: 60 * 1000,
  });

export const usePublicThemeQuery = (subdomain?: string, domain?: string) =>
  useQuery({
    queryKey: ['whitelabel', 'public-theme', subdomain ?? null, domain ?? null] as const,
    queryFn: () => whitelabelApi.getPublicTheme(subdomain, domain),
    enabled: Boolean(subdomain || domain),
    staleTime: 5 * 60 * 1000,
  });

export const useUpdateTenantTheme = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateTenantThemeInput) => whitelabelApi.updateTheme(input),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['whitelabel'] });
    },
  });
};

export const useUploadThemeAsset = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ kind, file }: { kind: TenantThemeAssetKind; file: File }) =>
      whitelabelApi.uploadAsset(kind, file),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['whitelabel'] });
    },
  });
};
