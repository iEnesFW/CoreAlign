import type { AxiosError, AxiosResponse, InternalAxiosRequestConfig } from 'axios';

const TRANSIENT_BACKEND_CODES = new Set([
  'COMMAND_TIMEOUT',
  'SERVICE_NOT_READY',
  'UPSTREAM_FAILED',
  'TIMEOUT',
]);

const IDEMPOTENT_METHODS = new Set(['get', 'head', 'options']);

const BACKOFF_BASE_MS = 300;
const MAX_RETRIES = 2;

export interface RetriableConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
  _retryCount?: number;
  _silent?: boolean;
}

const sleep = (ms: number) => new Promise<void>((r) => setTimeout(r, ms));

const extractBackendCode = (response: AxiosResponse | undefined): string | undefined => {
  if (!response) return undefined;
  const data = response.data as { code?: unknown } | undefined;
  if (!data) return undefined;
  return typeof data.code === 'string' ? data.code : undefined;
};

export const isTransientFailure = (error: AxiosError): boolean => {
  const status = error.response?.status ?? 0;
  if (status >= 500 && status < 600) return true;
  if (error.code === 'ECONNABORTED' || error.code === 'ETIMEDOUT') return true;
  const backendCode = extractBackendCode(error.response);
  if (backendCode && TRANSIENT_BACKEND_CODES.has(backendCode)) return true;
  return false;
};

export const isRetriableMethod = (config: RetriableConfig | undefined): boolean => {
  const method = (config?.method ?? 'get').toLowerCase();
  return IDEMPOTENT_METHODS.has(method);
};

export const shouldRetry = (error: AxiosError, config: RetriableConfig | undefined): boolean => {
  if (!config) return false;
  if ((config._retryCount ?? 0) >= MAX_RETRIES) return false;
  if (!isRetriableMethod(config)) return false;
  return isTransientFailure(error);
};

export const waitForRetry = async (config: RetriableConfig): Promise<void> => {
  const attempt = config._retryCount ?? 0;
  const delay = BACKOFF_BASE_MS * Math.pow(2, attempt);
  config._retryCount = attempt + 1;
  await sleep(delay);
};
