import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type {
  AppUser,
  InviteUserInput,
  Role,
  SetUserActiveInput,
  UpdateUserRolesInput,
} from '../model/user.types';

const BASE = '/users';
const INVALIDATION = [/\/users/i] as const;

export const usersApi = {
  list: () => cachedGet<ApiResponse<AppUser[]>>(apiClient, BASE),

  listRoles: () => cachedGet<ApiResponse<Role[]>>(apiClient, `${BASE}/roles`),

  invite: (input: InviteUserInput) =>
    apiClient.post<ApiResponse<AppUser>>(BASE, input).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),

  updateRoles: (input: UpdateUserRolesInput) =>
    apiClient
      .put<ApiResponse<AppUser>>(`${BASE}/${input.id}/roles`, { roleIds: input.roleIds })
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),

  setActive: (input: SetUserActiveInput) =>
    apiClient
      .put<ApiResponse<AppUser>>(`${BASE}/${input.id}/active`, { isActive: input.isActive })
      .then((r) => {
        invalidateHttpCache(INVALIDATION);
        return r.data;
      }),
};
