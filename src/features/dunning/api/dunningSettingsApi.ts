import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type { DunningSetting, UpsertDunningSettingInput } from '../model/dunning.types';

const BASE = '/dunning-settings';
const INVALIDATION = [/\/dunning-settings/i] as const;

export const dunningSettingsApi = {
  list: () => cachedGet<ApiResponse<DunningSetting[]>>(apiClient, BASE),

  upsert: (input: UpsertDunningSettingInput) =>
    apiClient.put<ApiResponse<DunningSetting>>(BASE, input).then((r) => {
      invalidateHttpCache(INVALIDATION);
      return r.data;
    }),
};
