import type {
  AxiosError,
  AxiosInstance,
  AxiosRequestConfig,
  InternalAxiosRequestConfig,
} from 'axios';
import axios from 'axios';
import Constants from 'expo-constants';
import * as SecureStore from 'expo-secure-store';

const TOKEN_KEY = 'corealign.jwt';
const REFRESH_KEY = 'corealign.refresh';

const resolveBaseUrl = (): string => {
  const extra = Constants.expoConfig?.extra ?? {};
  const fromExtra = typeof extra.apiBaseUrl === 'string' ? extra.apiBaseUrl : null;
  return fromExtra ?? 'https://api.corealign.dev';
};

export const TokenStorage = {
  async getAccess(): Promise<string | null> {
    return SecureStore.getItemAsync(TOKEN_KEY);
  },
  async getRefresh(): Promise<string | null> {
    return SecureStore.getItemAsync(REFRESH_KEY);
  },
  async setTokens(access: string, refresh: string): Promise<void> {
    await SecureStore.setItemAsync(TOKEN_KEY, access);
    await SecureStore.setItemAsync(REFRESH_KEY, refresh);
  },
  async clear(): Promise<void> {
    await SecureStore.deleteItemAsync(TOKEN_KEY);
    await SecureStore.deleteItemAsync(REFRESH_KEY);
  },
};

type RefreshHandler = (
  refreshToken: string,
) => Promise<{ accessToken: string; refreshToken: string }>;

let pendingRefresh: Promise<string | null> | null = null;
let refreshHandler: RefreshHandler | null = null;
let onAuthFailure: (() => void) | null = null;

export const registerRefreshHandler = (handler: RefreshHandler): void => {
  refreshHandler = handler;
};

export const registerAuthFailureHandler = (handler: () => void): void => {
  onAuthFailure = handler;
};

const performRefresh = async (): Promise<string | null> => {
  if (pendingRefresh) return pendingRefresh;
  pendingRefresh = (async () => {
    try {
      const refresh = await TokenStorage.getRefresh();
      if (!refresh || !refreshHandler) return null;
      const next = await refreshHandler(refresh);
      await TokenStorage.setTokens(next.accessToken, next.refreshToken);
      return next.accessToken;
    } catch {
      await TokenStorage.clear();
      onAuthFailure?.();
      return null;
    } finally {
      pendingRefresh = null;
    }
  })();
  return pendingRefresh;
};

export const apiClient: AxiosInstance = axios.create({
  baseURL: resolveBaseUrl(),
  timeout: 30000,
  headers: { 'Content-Type': 'application/json' },
});

apiClient.interceptors.request.use(async (config: InternalAxiosRequestConfig) => {
  const token = await TokenStorage.getAccess();
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`);
  }
  return config;
});

interface RetryConfig extends AxiosRequestConfig {
  _retry?: boolean;
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as RetryConfig | undefined;
    const status = error.response?.status;
    if (status === 401 && original && !original._retry) {
      original._retry = true;
      const fresh = await performRefresh();
      if (fresh) {
        original.headers = original.headers ?? {};
        (original.headers as Record<string, string>)['Authorization'] = `Bearer ${fresh}`;
        return apiClient.request(original);
      }
    }
    return Promise.reject(error);
  },
);
