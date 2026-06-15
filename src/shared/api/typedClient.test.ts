import { describe, expect, it, vi, beforeEach } from 'vitest';

vi.mock('./apiClient', () => {
  const requestSpy = vi.fn(async () => ({ data: { isSuccess: true }, status: 200, headers: {} }));
  return {
    apiClient: {
      defaults: { baseURL: '/api/v1', withCredentials: true, headers: {} },
      request: requestSpy,
      interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } },
    },
    __requestSpy: requestSpy,
  };
});

vi.mock('./EMCM.Client', () => {
  class Client {
    public baseUrl: string;
    public instance: { request: (cfg: { url: string; method: string }) => Promise<unknown> };
    constructor(
      baseUrl?: string,
      instance?: { request: (cfg: { url: string; method: string }) => Promise<unknown> },
    ) {
      this.baseUrl = baseUrl ?? '';
      this.instance = instance!;
    }
    async dashboard() {
      const url_ = this.baseUrl + '/api/v1/customer-portal/dashboard';
      return this.instance.request({ url: url_, method: 'GET' });
    }
  }
  return { Client };
});

import { getTypedClient } from './typedClient';
import * as apiClientModule from './apiClient';

const requestSpy = (apiClientModule as unknown as { __requestSpy: ReturnType<typeof vi.fn> })
  .__requestSpy;

describe('typedClient', () => {
  beforeEach(() => {
    requestSpy.mockClear();
  });

  it('strips the /api/v1 prefix before delegating so apiClient does not double-prefix the URL', async () => {
    const client = getTypedClient() as unknown as { dashboard: () => Promise<unknown> };
    await client.dashboard();
    expect(requestSpy).toHaveBeenCalledTimes(1);
    const calledWith = requestSpy.mock.calls[0][0] as { url: string };
    expect(calledWith.url).toBe('/customer-portal/dashboard');
  });

  it('returns the same generated client instance on repeated calls', () => {
    const first = getTypedClient();
    const second = getTypedClient();
    expect(first).toBe(second);
  });
});
