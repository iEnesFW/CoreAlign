import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { AxiosInstance } from 'axios';
import { cachedGet, clearHttpCache, invalidateHttpCache, setCacheNamespace } from './httpCache';

const buildAxiosStub = (
  impl: (
    url: string,
  ) => Promise<{ status: number; data: unknown; headers?: Record<string, string> }>,
) => {
  const get = vi.fn(impl);
  return { get } as unknown as AxiosInstance & { get: ReturnType<typeof vi.fn> };
};

beforeEach(() => {
  clearHttpCache();
  setCacheNamespace('test-tenant');
});

describe('cachedGet', () => {
  it('caches successive identical requests', async () => {
    const stub = buildAxiosStub(async () => ({
      status: 200,
      data: { ok: true },
      headers: { etag: 'W/"1"' },
    }));

    const first = await cachedGet<{ ok: boolean }>(stub, '/customers');
    const second = await cachedGet<{ ok: boolean }>(stub, '/customers');

    expect(first).toEqual({ ok: true });
    expect(second).toEqual({ ok: true });
    expect(stub.get).toHaveBeenCalledTimes(1);
  });

  it('dedupes concurrent in-flight requests via single-flight', async () => {
    const stub = buildAxiosStub(
      () =>
        new Promise((resolve) =>
          setTimeout(
            () => resolve({ status: 200, data: { value: 1 }, headers: { etag: 'W/"x"' } }),
            10,
          ),
        ),
    );

    const [a, b] = await Promise.all([
      cachedGet<{ value: number }>(stub, '/customers'),
      cachedGet<{ value: number }>(stub, '/customers'),
    ]);

    expect(a).toEqual({ value: 1 });
    expect(b).toEqual({ value: 1 });
    expect(stub.get).toHaveBeenCalledTimes(1);
  });

  it('skips cache when bypass is set', async () => {
    const stub = buildAxiosStub(async () => ({
      status: 200,
      data: { ok: true },
      headers: { etag: 'W/"1"' },
    }));

    await cachedGet(stub, '/customers');
    await cachedGet(stub, '/customers', undefined, { bypass: true });

    expect(stub.get).toHaveBeenCalledTimes(2);
  });

  it('invalidateHttpCache removes matching entries', async () => {
    const stub = buildAxiosStub(async () => ({
      status: 200,
      data: { ok: true },
      headers: { etag: 'W/"1"' },
    }));

    await cachedGet(stub, '/customers');
    invalidateHttpCache([/\/customers/i]);
    await cachedGet(stub, '/customers');

    expect(stub.get).toHaveBeenCalledTimes(2);
  });

  it('does not cache when no TTL rule matches the url', async () => {
    const stub = buildAxiosStub(async () => ({
      status: 200,
      data: { ok: true },
    }));

    await cachedGet(stub, '/auth/me');
    await cachedGet(stub, '/auth/me');

    expect(stub.get).toHaveBeenCalledTimes(2);
  });
});
