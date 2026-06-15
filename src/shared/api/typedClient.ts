import type { AxiosInstance, AxiosRequestConfig } from 'axios';
import { apiClient } from './apiClient';
import { Client as GeneratedClient } from './EMCM.Client';

const API_V1_PREFIX = '/api/v1';

const stripPrefix = (url: string | undefined): string => {
  if (!url) return '';
  return url.startsWith(API_V1_PREFIX) ? url.slice(API_V1_PREFIX.length) : url;
};

const buildTypedShim = (): AxiosInstance => {
  const shim = ((config?: AxiosRequestConfig) =>
    apiClient.request({ ...config, url: stripPrefix(config?.url) })) as unknown as AxiosInstance;
  shim.request = (config: AxiosRequestConfig) =>
    apiClient.request({ ...config, url: stripPrefix(config.url) });
  return shim;
};

let cached: GeneratedClient | null = null;

export const getTypedClient = (): GeneratedClient => {
  if (cached) return cached;
  cached = new GeneratedClient('', buildTypedShim());
  return cached;
};
