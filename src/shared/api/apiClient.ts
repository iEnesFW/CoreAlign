import axios, { type AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/features/auth/model/authStore';
import { authApi } from '@/features/auth/api/authApi';
import { env } from '@/shared/lib/env';
import { logger } from '@/shared/lib/logger';
import { parseError, formatError } from '@/shared/errors/errorPipeline';
import { ApiError } from './ApiError';
import { queueToast } from './toastQueue';
import { acquireRefreshLock, releaseRefreshLock, waitForRefreshLock } from './refreshLock';
import { broadcastRefresh, subscribeRefreshBroadcast } from './refreshBroadcast';
import { shouldRetry, waitForRetry, type RetriableConfig } from './retry';
import { setLoggerContext } from '@/shared/lib/logger';

const HEADER_CORRELATION_ID = 'X-Correlation-Id';

// Stable per-tab session id — reused across requests so backend logs can group
// activity by browser session in addition to per-request correlation ids.
const SESSION_ID = generateId();
setLoggerContext({ sessionId: SESSION_ID });

function generateId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID().replace(/-/g, '');
  }
  // Acceptable fallback for environments without WebCrypto (older Safari, jsdom).
  return Math.random().toString(36).slice(2) + Date.now().toString(36);
}

export type { RetriableConfig };

export const apiClient = axios.create({
  baseURL: `${env.VITE_API_URL}/api/v1`,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
});

apiClient.interceptors.request.use((config) => {
  const { accessToken } = useAuthStore.getState();
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  // Tag every request with a correlation id so backend logs can be cross-linked
  // with frontend telemetry. We respect the caller's id if they already set one
  // (e.g. an automation rerunning a request) and otherwise mint a fresh one.
  if (!config.headers[HEADER_CORRELATION_ID]) {
    config.headers[HEADER_CORRELATION_ID] = generateId();
  }
  return config;
});

// Capture the correlation id from successful + failing responses so the most
// recent one is available to logger.warn/error calls without callers having to
// thread it through manually.
const captureCorrelation = (headers: Record<string, unknown> | undefined): void => {
  if (!headers) return;
  const id = (headers[HEADER_CORRELATION_ID.toLowerCase()] ?? headers[HEADER_CORRELATION_ID]) as
    | string
    | undefined;
  if (typeof id === 'string' && id.length > 0) {
    setLoggerContext({ correlationId: id });
  }
};

// Listen for token rotations performed by another tab and apply them locally so
// queued requests after `waitForRefreshLock` retry with the fresh token instead
// of the stale in-memory copy. The token never touches localStorage.
subscribeRefreshBroadcast((msg) => {
  if (msg.type === 'token-refreshed') {
    useAuthStore.getState().setAccessToken(msg.accessToken);
  } else if (msg.type === 'signed-out') {
    useAuthStore.getState().clearAuth();
  }
});

let isRefreshing = false;
let pendingRequests: Array<{
  resolve: (value: unknown) => void;
  reject: (reason: unknown) => void;
}> = [];

const drainQueue = (error: unknown) => {
  pendingRequests.forEach(({ resolve, reject }) => {
    if (error) reject(error);
    else resolve(undefined);
  });
  pendingRequests = [];
};

const AUTH_BYPASS_PATHS = ['/auth/login', '/auth/refresh-token', '/auth/register'];

const isFailedApiResponse = (
  body: unknown,
): body is {
  isSuccess: false;
  errors?: string[];
  statusCode?: number;
  traceId?: string;
  code?: string;
} => {
  if (!body || typeof body !== 'object') return false;
  return (body as Record<string, unknown>).isSuccess === false;
};

const enforceApiSuccess = (response: AxiosResponse): AxiosResponse => {
  if (isFailedApiResponse(response.data)) {
    throw new ApiError(
      response.data.errors ?? ['Request failed.'],
      response.data.statusCode ?? response.status,
      response.data.traceId,
    );
  }
  return response;
};

const isSilent = (config: InternalAxiosRequestConfig | undefined): boolean =>
  (config as RetriableConfig | undefined)?._silent === true;

const notifyFromError = (error: AxiosError): void => {
  if (isSilent(error.config)) return;
  const parsed = parseError(error);
  if (parsed.isAborted) return;
  const description = formatError(parsed, { includeTrace: !!parsed.traceId });
  const dedupeKey = `${parsed.status}:${parsed.code ?? parsed.message.slice(0, 80)}`;
  queueToast({ dedupeKey, description, variant: 'error' });
};

apiClient.interceptors.response.use(
  (response) => {
    captureCorrelation(response.headers as Record<string, unknown>);
    return enforceApiSuccess(response);
  },
  async (error: AxiosError) => {
    captureCorrelation(error.response?.headers as Record<string, unknown> | undefined);
    const originalRequest = error.config as RetriableConfig | undefined;

    if (axios.isCancel(error)) return Promise.reject(error);

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      const isBypassed = AUTH_BYPASS_PATHS.some((path) => originalRequest.url?.includes(path));
      if (isBypassed) {
        notifyFromError(error);
        return Promise.reject(error);
      }

      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          pendingRequests.push({ resolve, reject });
        }).then(() => apiClient(originalRequest));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const ownsLock = acquireRefreshLock();
      if (!ownsLock) {
        try {
          await waitForRefreshLock();
          isRefreshing = false;
          return apiClient(originalRequest);
        } catch (waitErr) {
          isRefreshing = false;
          return Promise.reject(waitErr);
        }
      }

      try {
        const response = await authApi.refreshToken();
        if (response.isSuccess && response.data) {
          useAuthStore.getState().setAuth(response.data.accessToken, response.data.user);
          // Push the new token to other tabs so their queued retries don't 401.
          broadcastRefresh({
            type: 'token-refreshed',
            accessToken: response.data.accessToken,
            at: Date.now(),
          });
          drainQueue(null);
          return apiClient(originalRequest);
        }
        throw new Error('Token refresh failed');
      } catch (refreshError) {
        logger.warn('Token refresh failed, signing out', { url: originalRequest.url });
        drainQueue(refreshError);
        useAuthStore.getState().clearAuth();
        broadcastRefresh({ type: 'signed-out', at: Date.now() });
        window.location.href = '/login';
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
        releaseRefreshLock();
      }
    }

    if (shouldRetry(error, originalRequest)) {
      await waitForRetry(originalRequest!);
      return apiClient(originalRequest!);
    }

    notifyFromError(error);
    return Promise.reject(error);
  },
);
