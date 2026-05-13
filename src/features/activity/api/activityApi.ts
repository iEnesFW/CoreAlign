import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse, PagedResult } from '@/shared/types/api';
import type { ActivityLog, ActivityLogListParams } from '../model/activity.types';

export const activityApi = {
  list: (params: ActivityLogListParams) =>
    apiClient
      .get<ApiResponse<PagedResult<ActivityLog>>>('/activity/logs', { params })
      .then((r) => r.data),
};
