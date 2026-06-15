import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';
import type {
  SsoIdentityProviderDto,
  CreateSsoIdentityProviderRequest,
  UpdateSsoIdentityProviderRequest,
  SsoTestConnectionResult,
} from '../model/sso.types';

const ADMIN_BASE = '/admin/identity-providers';

export const ssoApi = {
  list: () => apiClient.get<ApiResponse<SsoIdentityProviderDto[]>>(ADMIN_BASE).then((r) => r.data),

  get: (id: string) =>
    apiClient.get<ApiResponse<SsoIdentityProviderDto>>(`${ADMIN_BASE}/${id}`).then((r) => r.data),

  create: (body: CreateSsoIdentityProviderRequest) =>
    apiClient.post<ApiResponse<SsoIdentityProviderDto>>(ADMIN_BASE, body).then((r) => r.data),

  update: (id: string, body: UpdateSsoIdentityProviderRequest) =>
    apiClient
      .put<ApiResponse<SsoIdentityProviderDto>>(`${ADMIN_BASE}/${id}`, body)
      .then((r) => r.data),

  remove: (id: string) =>
    apiClient.delete<ApiResponse<{ id: string }>>(`${ADMIN_BASE}/${id}`).then((r) => r.data),

  testConnection: (id: string) =>
    apiClient
      .post<ApiResponse<SsoTestConnectionResult>>(`${ADMIN_BASE}/${id}/test-connection`)
      .then((r) => r.data),

  buildLoginUrl: (
    tenantSlug: string,
    idpName: string,
    protocol: 'saml' | 'oidc',
    returnUrl: string,
  ) => {
    const params = new URLSearchParams({ returnUrl });
    return `/api/v1/auth/${protocol}/${encodeURIComponent(tenantSlug)}/${encodeURIComponent(idpName)}/login?${params.toString()}`;
  },
};
