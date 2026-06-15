import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';

export interface PlatformTenantDto {
  id: string;
  name: string;
  slug: string;
  legalName?: string | null;
  dpoContactName?: string | null;
  dpoContactEmail?: string | null;
  isActive: boolean;
  isArchived: boolean;
  archivedAtUtc?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface PagedTenants {
  items: PlatformTenantDto[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface UpdatePlatformTenantRequest {
  id: string;
  name: string;
  slug: string;
  dpoContactName?: string | null;
  dpoContactEmail?: string | null;
}

const BASE = '/platform/tenants';

export const platformTenantApi = {
  list: (search: string | undefined, page: number, pageSize: number, includeArchived: boolean) =>
    apiClient
      .get<ApiResponse<PagedTenants>>(BASE, { params: { search, page, pageSize, includeArchived } })
      .then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<ApiResponse<PlatformTenantDto>>(`${BASE}/${id}`).then((r) => r.data),

  update: (request: UpdatePlatformTenantRequest) =>
    apiClient
      .put<ApiResponse<PlatformTenantDto>>(`${BASE}/${request.id}`, request)
      .then((r) => r.data),

  archive: (id: string) =>
    apiClient.post<ApiResponse<boolean>>(`${BASE}/${id}/archive`).then((r) => r.data),

  restore: (id: string) =>
    apiClient.post<ApiResponse<boolean>>(`${BASE}/${id}/restore`).then((r) => r.data),
};
