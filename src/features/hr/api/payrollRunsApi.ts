import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type {
  CreatePayrollRunInput,
  PayrollRun,
  PayrollRunListItem,
  PayrollRunListParams,
  Payslip,
} from '../model/payroll.types';

const BASE = '/payroll-runs';
const INVALIDATION = [/\/payroll-runs/i, /\/payslips/i, /\/employees/i] as const;

const mutate = <T>(p: Promise<{ data: ApiResponse<T> }>) =>
  p.then((r) => {
    invalidateHttpCache(INVALIDATION);
    return r.data;
  });

export const payrollRunsApi = {
  search: (params: PayrollRunListParams) =>
    cachedGet<ApiResponse<PagedResult<PayrollRunListItem>>>(apiClient, BASE, { params }),

  getById: (id: string) => cachedGet<ApiResponse<PayrollRun>>(apiClient, `${BASE}/${id}`),

  getPayslips: (id: string) =>
    cachedGet<ApiResponse<Payslip[]>>(apiClient, `${BASE}/${id}/payslips`),

  create: (input: CreatePayrollRunInput) =>
    mutate(apiClient.post<ApiResponse<PayrollRun>>(BASE, input)),

  calculate: (id: string) =>
    mutate(apiClient.post<ApiResponse<PayrollRun>>(`${BASE}/${id}/calculate`)),

  approve: (id: string) => mutate(apiClient.post<ApiResponse<PayrollRun>>(`${BASE}/${id}/approve`)),

  reopen: (id: string) => mutate(apiClient.post<ApiResponse<PayrollRun>>(`${BASE}/${id}/reopen`)),

  post: (id: string) => mutate(apiClient.post<ApiResponse<PayrollRun>>(`${BASE}/${id}/post`)),

  pay: (id: string) => mutate(apiClient.post<ApiResponse<PayrollRun>>(`${BASE}/${id}/pay`)),
};
