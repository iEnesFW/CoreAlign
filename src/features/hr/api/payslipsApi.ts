import { apiClient } from '@/shared/api/apiClient';
import { cachedGet } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type { Payslip } from '../model/payroll.types';

const BASE = '/payslips';

export const payslipsApi = {
  getById: (id: string) => cachedGet<ApiResponse<Payslip>>(apiClient, `${BASE}/${id}`),
};
