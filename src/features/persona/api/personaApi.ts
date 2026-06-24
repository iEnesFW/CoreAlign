import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';
import type { UxComplexityMode } from '@/shared/lib/persona';

export interface UserPreferenceSnapshot {
  effectiveMode: UxComplexityMode;
  userOverride: UxComplexityMode | null;
  tenantDefault: UxComplexityMode;
  localeOverride: string | null;
  themeOverride: string | null;
  perScreenOverridesJson: string | null;
}

export interface UpdateUserPreferenceInput {
  mode?: UxComplexityMode | null;
  localeOverride?: string | null;
  themeOverride?: string | null;
  perScreenOverridesJson?: string | null;
}

export const personaApi = {
  getMine: () =>
    apiClient.get<ApiResponse<UserPreferenceSnapshot>>('/users/me/preferences').then((r) => r.data),

  update: (input: UpdateUserPreferenceInput) =>
    apiClient
      .patch<ApiResponse<UserPreferenceSnapshot>>('/users/me/preferences', input)
      .then((r) => r.data),
};
