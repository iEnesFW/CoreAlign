import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type {
  PublicTenantThemeDto,
  TenantThemeAssetDto,
  TenantThemeAssetKind,
  TenantThemeDto,
  UpdateTenantThemeInput,
} from '../model/whitelabel.types';

const ADMIN_BASE = '/admin/tenant-theme';
const PUBLIC_BASE = '/public/theme';
const INVALIDATION = [/\/admin\/tenant-theme/i, /\/public\/theme/i] as const;

export const whitelabelApi = {
  getTheme: () => cachedGet<TenantThemeDto>(apiClient, ADMIN_BASE),

  updateTheme: (input: UpdateTenantThemeInput) =>
    apiClient.put<TenantThemeDto>(ADMIN_BASE, input).then((response) => {
      invalidateHttpCache(INVALIDATION);
      return response.data;
    }),

  uploadAsset: (kind: TenantThemeAssetKind, file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    return apiClient
      .post<TenantThemeAssetDto>(`${ADMIN_BASE}/assets/${kind}`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      .then((response) => {
        invalidateHttpCache(INVALIDATION);
        return response.data;
      });
  },

  getPublicTheme: (subdomain?: string, domain?: string) => {
    const params: Record<string, string> = {};
    if (subdomain) params.subdomain = subdomain;
    if (domain) params.domain = domain;
    return cachedGet<PublicTenantThemeDto>(apiClient, PUBLIC_BASE, { params });
  },
};
