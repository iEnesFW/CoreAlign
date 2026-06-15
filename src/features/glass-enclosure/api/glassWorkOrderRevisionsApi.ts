import { apiClient } from '@/shared/api/apiClient';
import { cachedGet, invalidateHttpCache } from '@/shared/http/httpCache';
import type { ApiResponse } from '@/shared/types/api';
import type {
  ApproveWorkOrderRevisionInput,
  RejectWorkOrderRevisionInput,
  WorkOrderRevisionDto,
} from '../model/workOrder.types';

const BASE = '/glass-enclosure/work-orders';
const INVALIDATION = [/\/glass-enclosure\/work-orders\//i] as const;

export const glassWorkOrderRevisionsApi = {
  list: (workOrderId: string) =>
    cachedGet<ApiResponse<WorkOrderRevisionDto[]>>(apiClient, `${BASE}/${workOrderId}/revisions`),

  approve: (workOrderId: string, revisionId: string, input?: ApproveWorkOrderRevisionInput) =>
    apiClient
      .post<void>(`${BASE}/${workOrderId}/revisions/${revisionId}/approve`, {
        overrideReason: input?.overrideReason ?? null,
      })
      .then((response) => {
        invalidateHttpCache(INVALIDATION);
        return response.data;
      }),

  reject: (workOrderId: string, revisionId: string, input: RejectWorkOrderRevisionInput) =>
    apiClient
      .post<void>(`${BASE}/${workOrderId}/revisions/${revisionId}/reject`, {
        reason: input.reason,
      })
      .then((response) => {
        invalidateHttpCache(INVALIDATION);
        return response.data;
      }),
};
