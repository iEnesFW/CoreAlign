import axios, { type AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios';
import { env } from '@/shared/lib/env';
import { logger } from '@/shared/lib/logger';
import { parseError, formatError } from '@/shared/errors/errorPipeline';
import { ApiError } from './ApiError';
import { authBridge } from './authBridge';
import { queueToast } from './toastQueue';
import { acquireRefreshLock, releaseRefreshLock, waitForRefreshLock } from './refreshLock';
import { broadcastRefresh, subscribeRefreshBroadcast } from './refreshBroadcast';
import { shouldRetry, waitForRetry, type RetriableConfig } from './retry';
import { setLoggerContext } from '@/shared/lib/logger';

const HEADER_CORRELATION_ID = 'X-Correlation-Id';

const SESSION_ID = generateId();
setLoggerContext({ sessionId: SESSION_ID });

function generateId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID().replace(/-/g, '');
  }
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
  const accessToken = authBridge.getAccessToken();
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  if (!config.headers[HEADER_CORRELATION_ID]) {
    config.headers[HEADER_CORRELATION_ID] = generateId();
  }
  return config;
});

let lastCorrelationId: string | undefined;

export const getLastCorrelationId = (): string | undefined => lastCorrelationId;

const captureCorrelation = (headers: Record<string, unknown> | undefined): void => {
  if (!headers) return;
  const id = (headers[HEADER_CORRELATION_ID.toLowerCase()] ?? headers[HEADER_CORRELATION_ID]) as
    | string
    | undefined;
  if (typeof id === 'string' && id.length > 0) {
    lastCorrelationId = id;
    setLoggerContext({ correlationId: id });
  }
};

subscribeRefreshBroadcast((msg) => {
  if (msg.type === 'token-refreshed') {
    authBridge.applyToken(msg.accessToken);
  } else if (msg.type === 'signed-out') {
    authBridge.signOut();
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
        const newToken = await authBridge.refresh();
        if (newToken) {
          broadcastRefresh({
            type: 'token-refreshed',
            accessToken: newToken,
            at: Date.now(),
          });
          drainQueue(null);
          return apiClient(originalRequest);
        }
        throw new Error('Token refresh failed');
      } catch (refreshError) {
        logger.warn('Token refresh failed, signing out', { url: originalRequest.url });
        drainQueue(refreshError);
        authBridge.signOut();
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
