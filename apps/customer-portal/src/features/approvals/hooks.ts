import { useQuery, type UseQueryOptions } from '@tanstack/react-query';
import type { PagedResult } from '@/features/portal/types';
import { approvalsApi } from './api';
import type { ApprovalOrderDetail, ApprovalOrderSummary } from './types';

export const approvalKeys = {
  list: (page = 1, pageSize = 20) => ['approvals', 'list', { page, pageSize }] as const,
  pendingCount: ['approvals', 'pendingCount'] as const,
  detail: (id: string) => ['approvals', 'detail', id] as const,
};

export const useApprovalsList = (
  params: { page?: number; pageSize?: number },
  options?: Omit<UseQueryOptions<PagedResult<ApprovalOrderSummary>>, 'queryKey' | 'queryFn'>,
) =>
  useQuery({
    queryKey: approvalKeys.list(params.page ?? 1, params.pageSize ?? 20),
    queryFn: () => approvalsApi.listPending(params),
    staleTime: 15_000,
    ...options,
  });

export const useApprovalsPendingCount = () =>
  useQuery({
    queryKey: approvalKeys.pendingCount,
    queryFn: async () => {
      const page = await approvalsApi.listPending({ page: 1, pageSize: 1 });
      return page.total;
    },
    staleTime: 60_000,
    refetchInterval: 60_000,
  });

export const useApprovalDetail = (
  id: string | undefined,
  options?: Omit<UseQueryOptions<ApprovalOrderDetail>, 'queryKey' | 'queryFn' | 'enabled'>,
) =>
  useQuery({
    queryKey: approvalKeys.detail(id ?? ''),
    queryFn: () => approvalsApi.getById(id!),
    enabled: !!id,
    ...options,
  });
