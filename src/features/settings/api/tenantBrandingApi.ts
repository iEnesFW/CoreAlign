import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';

export interface TenantLogoResult {
  logoUrl: string;
  storageKey: string;
  contentType: string;
  sizeBytes: number;
}

const BASE = '/tenants/me';

export const tenantBrandingApi = {
  uploadLogo: async (file: File): Promise<ApiResponse<TenantLogoResult>> => {
    const form = new FormData();
    form.append('file', file);
    const response = await apiClient.post<ApiResponse<TenantLogoResult>>(`${BASE}/logo`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },
};
