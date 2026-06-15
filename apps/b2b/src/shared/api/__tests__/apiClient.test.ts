import { describe, expect, it } from 'vitest';
import { apiClient } from '@/shared/api/apiClient';

describe('apiClient axios instance', () => {
  it('uses /api/v1 as baseURL', () => {
    expect(apiClient.defaults.baseURL).toBe('/api/v1');
  });

  it('sends JSON content-type by default', () => {
    expect(apiClient.defaults.headers['Content-Type']).toBe('application/json');
  });

  it('honors withCredentials for cookie auth', () => {
    expect(apiClient.defaults.withCredentials).toBe(true);
  });

  it('has at least one request interceptor for auth header', () => {
    const reqInterceptors = (apiClient.interceptors.request as unknown as { handlers: unknown[] })
      .handlers;
    expect(reqInterceptors.length).toBeGreaterThan(0);
  });

  it('has at least one response interceptor for envelope unwrap', () => {
    const resInterceptors = (apiClient.interceptors.response as unknown as { handlers: unknown[] })
      .handlers;
    expect(resInterceptors.length).toBeGreaterThan(0);
  });
});
