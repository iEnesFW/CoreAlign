import axios, { type AxiosError, type AxiosResponse } from 'axios';
import { toast } from 'sonner';
import i18n from '@/app/i18n';
import { authBridge } from './authBridge';

const HEADER_CORRELATION_ID = 'X-Correlation-Id';

let lastCorrelationId: string | undefined;

export const getLastCorrelationId = (): string | undefined => lastCorrelationId;

const newCorrelationId = (): string =>
  typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function'
    ? crypto.randomUUID().replace(/-/g, '')
    : Math.random().toString(36).slice(2) + Date.now().toString(36);

const captureCorrelation = (headers: Record<string, unknown> | undefined): void => {
  if (!headers) return;
  const id = (headers[HEADER_CORRELATION_ID.toLowerCase()] ?? headers[HEADER_CORRELATION_ID]) as
    | string
    | undefined;
  if (typeof id === 'string' && id.length > 0) lastCorrelationId = id;
};

export const apiClient = axios.create({
  baseURL: '/api/v1',
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true,
});

apiClient.interceptors.request.use((config) => {
  const accessToken = authBridge.getAccessToken();
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  if (!config.headers[HEADER_CORRELATION_ID]) {
    config.headers[HEADER_CORRELATION_ID] = newCorrelationId();
  }
  return config;
});

interface ApiEnvelope<T> {
  isSuccess: boolean;
  data?: T;
  errors?: string[];
  statusCode?: number;
  traceId?: string;
}

const isFailureEnvelope = (body: unknown): body is ApiEnvelope<unknown> & { isSuccess: false } => {
  if (!body || typeof body !== 'object') return false;
  return (body as { isSuccess?: unknown }).isSuccess === false;
};

const unwrap = (response: AxiosResponse): AxiosResponse => {
  const body = response.data as ApiEnvelope<unknown> | unknown;
  if (body && typeof body === 'object' && 'isSuccess' in (body as object)) {
    const envelope = body as ApiEnvelope<unknown>;
    if (envelope.isSuccess === false) {
      throw Object.assign(new Error(envelope.errors?.[0] ?? i18n.t('b2b.common.requestFailed')), {
        status: envelope.statusCode ?? response.status,
        traceId: envelope.traceId,
      });
    }
    return { ...response, data: envelope.data };
  }
  return response;
};

apiClient.interceptors.response.use(
  (response) => {
    captureCorrelation(response.headers as Record<string, unknown>);
    return unwrap(response);
  },
  (error: AxiosError) => {
    captureCorrelation(error.response?.headers as Record<string, unknown> | undefined);
    if (axios.isCancel(error)) return Promise.reject(error);

    const status = error.response?.status;
    const body = error.response?.data;
    const message = isFailureEnvelope(body) ? (body.errors?.[0] ?? error.message) : error.message;

    if (status === 401) {
      authBridge.clearAuth();
      if (typeof window !== 'undefined' && !window.location.pathname.startsWith('/login')) {
        window.location.href = '/login';
      }
    } else if (status === 403) {
      toast.error(message || i18n.t('b2b.common.noPermission'));
    } else if (!error.response) {
      toast.error(i18n.t('b2b.common.networkError'));
    }

    return Promise.reject(Object.assign(error, { status, normalizedMessage: message }));
  },
);
