import { useQuery } from '@tanstack/react-query';
import { activityApi } from '../api/activityApi';
import type { ActivityLogListParams } from '../model/activity.types';

export const useActivityLogsQuery = (params: ActivityLogListParams) =>
  useQuery({
    queryKey: ['activity', 'logs', params],
    queryFn: () => activityApi.list(params),
    placeholderData: (previous) => previous,
    staleTime: 10_000,
  });
