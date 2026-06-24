import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { errorLogsAdminApi } from '../api/errorLogsAdminApi';
import type { SafeResult } from '@/shared/lib/safeRequest';
import type { ErrorLogDetail, ErrorLogFilters, ErrorLogPage } from '../errorLogs.types';

const unwrapSafe = async <T>(promise: Promise<SafeResult<T>>): Promise<T> => {
  const [data, error] = await promise;
  if (error) throw error;
  return data as T;
};

export const errorLogKeys = {
  all: ['admin', 'error-logs'] as const,
  list: (filters: ErrorLogFilters) => [...errorLogKeys.all, 'list', filters] as const,
  detail: (id: string) => [...errorLogKeys.all, 'detail', id] as const,
};

export const useErrorLogsQuery = (filters: ErrorLogFilters) =>
  useQuery({
    queryKey: errorLogKeys.list(filters),
    queryFn: () => unwrapSafe<ErrorLogPage>(errorLogsAdminApi.list(filters)),
    placeholderData: (previous) => previous,
  });

export const useErrorLogDetailQuery = (id: string | null) =>
  useQuery({
    queryKey: errorLogKeys.detail(id ?? ''),
    queryFn: () => unwrapSafe<ErrorLogDetail>(errorLogsAdminApi.get(id as string)),
    enabled: !!id,
  });

export const useResolveErrorLogMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, notes }: { id: string; notes: string | null }) =>
      unwrapSafe(errorLogsAdminApi.resolve(id, notes)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: errorLogKeys.all });
    },
  });
};
