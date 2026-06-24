import { apiClient } from '@/shared/api/apiClient';
import { safeRequest, type SafeResult } from '@/shared/lib/safeRequest';
import type { ApiResponse } from '@/shared/types/api';
import type { ErrorLogDetail, ErrorLogFilters, ErrorLogPage } from '../errorLogs.types';

const BASE = '/admin/error-logs';

const unwrap = async <T>(promise: Promise<{ data: ApiResponse<T> }>): Promise<T> => {
  const { data } = await promise;
  if (!data.isSuccess || data.data === null || data.data === undefined) {
    throw new Error(data.errors?.[0] ?? 'Request failed.');
  }
  return data.data as T;
};

export const errorLogsAdminApi = {
  list: (filters: ErrorLogFilters): Promise<SafeResult<ErrorLogPage>> =>
    safeRequest(unwrap<ErrorLogPage>(apiClient.get(BASE, { params: filters }))),

  get: (id: string): Promise<SafeResult<ErrorLogDetail>> =>
    safeRequest(unwrap<ErrorLogDetail>(apiClient.get(`${BASE}/${id}`))),

  resolve: (id: string, notes: string | null): Promise<SafeResult<void>> =>
    safeRequest(apiClient.post(`${BASE}/${id}/resolve`, { notes }).then(() => undefined)),
};
