import { apiClient } from '@/shared/api/apiClient';
import type { PagedResult } from '@/features/portal/types';
import type { ApprovalOrderDetail, ApprovalOrderSummary } from './types';

const BASE = '/customer-portal/approvals';

export const approvalsApi = {
  listPending: async (params: { page?: number; pageSize?: number }) => {
    const { data } = await apiClient.get<PagedResult<ApprovalOrderSummary>>(BASE, { params });
    return data;
  },
  getById: async (id: string): Promise<ApprovalOrderDetail> => {
    const { data } = await apiClient.get<ApprovalOrderDetail>(`${BASE}/${id}`);
    return data;
  },
  approve: async (id: string): Promise<ApprovalOrderDetail> => {
    const { data } = await apiClient.post<ApprovalOrderDetail>(`${BASE}/${id}/approve`);
    return data;
  },
  reject: async (id: string, reason: string): Promise<ApprovalOrderDetail> => {
    const { data } = await apiClient.post<ApprovalOrderDetail>(`${BASE}/${id}/reject`, { reason });
    return data;
  },
};
