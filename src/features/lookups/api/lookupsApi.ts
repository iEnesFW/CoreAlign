import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';
import type { Country, Currency, District, Province } from '../model/lookup.types';

const BASE = '/lookups';

export const lookupsApi = {
  currencies: (isActive?: boolean) =>
    apiClient
      .get<ApiResponse<Currency[]>>(`${BASE}/currencies`, {
        params: isActive === undefined ? {} : { isActive },
      })
      .then((r) => r.data),

  countries: (isActive?: boolean) =>
    apiClient
      .get<ApiResponse<Country[]>>(`${BASE}/countries`, {
        params: isActive === undefined ? {} : { isActive },
      })
      .then((r) => r.data),

  provinces: (countryCode?: string) =>
    apiClient
      .get<ApiResponse<Province[]>>(`${BASE}/provinces`, {
        params: countryCode ? { countryCode } : {},
      })
      .then((r) => r.data),

  districts: (provinceId?: number) =>
    apiClient
      .get<ApiResponse<District[]>>(`${BASE}/districts`, {
        params: provinceId ? { provinceId } : {},
      })
      .then((r) => r.data),
};
