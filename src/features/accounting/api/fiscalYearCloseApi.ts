import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';
import type { YearEndEntry } from '../model/fiscalYearClose.types';

const BASE = '/accounting/fiscal-years';

export const fiscalYearCloseApi = {
  close: (year: number) =>
    apiClient.post<ApiResponse<YearEndEntry>>(`${BASE}/${year}/close`, {}).then((r) => r.data),

  openNext: (year: number) =>
    apiClient.post<ApiResponse<YearEndEntry>>(`${BASE}/${year}/open-next`, {}).then((r) => r.data),

  reverseClose: (year: number) =>
    apiClient
      .post<ApiResponse<YearEndEntry>>(`${BASE}/${year}/reverse-close`, {})
      .then((r) => r.data),
};
