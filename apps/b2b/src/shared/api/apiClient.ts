import axios, { type AxiosError, type AxiosResponse } from 'axios';
import { toast } from 'sonner';
import i18n from '@/app/i18n';
import { useAuthStore } from '@/features/auth/authStore';

export const apiClient = axios.create({
  baseURL: '/api/v1',
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true,
});

apiClient.interceptors.request.use((config) => {
  const { accessToken } = useAuthStore.getState();
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
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
  (response) => unwrap(response),
  (error: AxiosError) => {
    if (axios.isCancel(error)) return Promise.reject(error);

    const status = error.response?.status;
    const body = error.response?.data;
    const message = isFailureEnvelope(body) ? (body.errors?.[0] ?? error.message) : error.message;

    if (status === 401) {
      useAuthStore.getState().clearAuth();
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
