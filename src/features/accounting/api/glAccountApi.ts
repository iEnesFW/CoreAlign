import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';
import type {
  CreateGLAccountRequest,
  GLAccount,
  GLAccountListParams,
  UpdateGLAccountRequest,
} from '../model/glAccount.types';

const BASE = '/accounting/gl-accounts';

export const glAccountApi = {
  list: (params: GLAccountListParams) =>
    apiClient.get<ApiResponse<GLAccount[]>>(BASE, { params }).then((r) => r.data),

  tree: () => apiClient.get<ApiResponse<GLAccount[]>>(`${BASE}/tree`).then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<ApiResponse<GLAccount>>(`${BASE}/${id}`).then((r) => r.data),

  create: (request: CreateGLAccountRequest) =>
    apiClient.post<ApiResponse<GLAccount>>(BASE, request).then((r) => r.data),

  update: (request: UpdateGLAccountRequest) =>
    apiClient.put<ApiResponse<GLAccount>>(`${BASE}/${request.id}`, request).then((r) => r.data),

  setActive: (id: string, isActive: boolean) =>
    apiClient
      .post<ApiResponse<GLAccount>>(`${BASE}/${id}/active`, { id, isActive })
      .then((r) => r.data),

  remove: (id: string) =>
    apiClient.delete<ApiResponse<boolean>>(`${BASE}/${id}`).then((r) => r.data),

  seedTurkish: () =>
    apiClient.post<ApiResponse<number>>(`${BASE}/seed-turkish`).then((r) => r.data),
};
