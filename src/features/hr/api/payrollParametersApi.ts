import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type {
  CreatePayrollParametersInput,
  PayrollParameters,
  UpdatePayrollParametersInput,
} from '../model/parameters.types';

const BASE = '/payroll-parameters';
const INVALIDATION = [/\/payroll-parameters/i] as const;

const mutate = <T>(p: Promise<{ data: ApiResponse<T> }>) =>
  p.then((r) => {
    invalidateHttpCache(INVALIDATION);
    return r.data;
  });

export const payrollParametersApi = {
  list: () => cachedGet<ApiResponse<PayrollParameters[]>>(apiClient, BASE),

  getById: (id: string) => cachedGet<ApiResponse<PayrollParameters>>(apiClient, `${BASE}/${id}`),

  create: (input: CreatePayrollParametersInput) =>
    mutate(apiClient.post<ApiResponse<PayrollParameters>>(BASE, input)),

  update: (input: UpdatePayrollParametersInput) =>
    mutate(apiClient.put<ApiResponse<PayrollParameters>>(`${BASE}/${input.id}`, input)),
};
