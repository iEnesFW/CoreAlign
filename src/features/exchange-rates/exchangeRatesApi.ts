import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';

export interface ExchangeRateDto {
  id: string;
  currency: string;
  rateAgainstTry: number;
  validOnDate: string;
  source: string;
  fetchedAtUtc: string;
}

const BASE = '/settings/exchange-rates';

export const exchangeRatesApi = {
  list: (from?: string, to?: string, currency?: string) =>
    apiClient
      .get<ApiResponse<ExchangeRateDto[]>>(BASE, { params: { from, to, currency } })
      .then((r) => r.data),

  refresh: () => apiClient.post<ApiResponse<number>>(`${BASE}/refresh`).then((r) => r.data),
};
