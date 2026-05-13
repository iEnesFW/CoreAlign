import axios from 'axios';
import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query';
import { logger } from '@/shared/lib/logger';
import { isApiError } from '@/shared/api/ApiError';

const MAX_RETRIES = 1;
const BACKOFF_BASE_MS = 1000;
const BACKOFF_CAP_MS = 8000;
const DEFAULT_STALE_MS = 5 * 60 * 1000;

const statusOf = (error: unknown): number | null => {
  if (isApiError(error)) return error.statusCode;
  if (axios.isAxiosError(error)) return error.response?.status ?? null;
  return null;
};

const isClientError = (error: unknown): boolean => {
  const status = statusOf(error);
  return status !== null && status >= 400 && status < 500;
};

const shouldRetry = (failureCount: number, error: unknown): boolean => {
  if (failureCount >= MAX_RETRIES) return false;
  if (axios.isCancel(error)) return false;
  if (isClientError(error)) return false;
  return true;
};

const retryDelay = (attempt: number): number =>
  Math.min(BACKOFF_BASE_MS * Math.pow(2, attempt), BACKOFF_CAP_MS);

const describeKey = (key: unknown): string => {
  try {
    return JSON.stringify(key);
  } catch {
    return String(key);
  }
};

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: shouldRetry,
      retryDelay,
      staleTime: DEFAULT_STALE_MS,
      refetchOnWindowFocus: false,
    },
    mutations: {
      retry: false,
    },
  },
  queryCache: new QueryCache({
    onError: (error, query) => {
      if (axios.isCancel(error)) return;
      logger.warn('query.error', {
        queryKey: describeKey(query.queryKey),
        status: statusOf(error),
        message: (error as Error)?.message,
      });
    },
  }),
  mutationCache: new MutationCache({
    onError: (error, _variables, _context, mutation) => {
      if (axios.isCancel(error)) return;
      logger.warn('mutation.error', {
        mutationKey: mutation.options.mutationKey
          ? describeKey(mutation.options.mutationKey)
          : undefined,
        status: statusOf(error),
        message: (error as Error)?.message,
      });
    },
  }),
});
