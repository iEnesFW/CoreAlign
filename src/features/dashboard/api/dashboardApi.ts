import { apiClient } from '@/shared/api/apiClient';
import type { ApiResponse } from '@/shared/types/api';
import type { DashboardStats } from '../model/dashboard.types';

export const dashboardApi = {
  getStats: () =>
    apiClient.get<ApiResponse<DashboardStats>>('/dashboard/stats').then((r) => r.data),
};
