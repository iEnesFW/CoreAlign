import type { AxiosInstance, AxiosRequestConfig } from 'axios';
import { logger } from '@/shared/lib/logger';

interface CacheEntry<T> {
  data: T;
  expiry: number;
  etag?: string;
}

const ONE_HOUR_MS = 60 * 60 * 1000;
const THIRTY_SECONDS_MS = 30 * 1000;
const REVALIDATE_INTERVAL_MS = 5 * 60 * 1000;
const DEFAULT_TTL_MS = 0;

export interface TtlRule {
  readonly re: RegExp;
  readonly ttl: number;
}

const TTL_RULES: readonly TtlRule[] = [
  { re: /\/customers(\/[a-f0-9-]+)?\/?(\?.*)?$/i, ttl: ONE_HOUR_MS },
  { re: /\/customers\/[a-f0-9-]+\/summary/i, ttl: ONE_HOUR_MS },
  { re: /\/customers\/[a-f0-9-]+\/overview/i, ttl: THIRTY_SECONDS_MS },
  { re: /\/customers\/[a-f0-9-]+\/analytics/i, ttl: THIRTY_SECONDS_MS * 2 },
  { re: /\/invoices\/[a-f0-9-]+\/credit-notes/i, ttl: THIRTY_SECONDS_MS },
  { re: /\/reports\//i, ttl: THIRTY_SECONDS_MS * 2 },
  { re: /\/payments\/by-invoice\//i, ttl: THIRTY_SECONDS_MS },
  { re: /\/customers\/[a-f0-9-]+\/addresses/i, ttl: ONE_HOUR_MS },
  { re: /\/customers\/[a-f0-9-]+\/contacts/i, ttl: ONE_HOUR_MS },
  { re: /\/products(\/[a-f0-9-]+)?\/?(\?.*)?$/i, ttl: ONE_HOUR_MS },
  { re: /\/products\/[a-f0-9-]+\/components/i, ttl: ONE_HOUR_MS },
  { re: /\/master-data\//i, ttl: ONE_HOUR_MS },
  { re: /\/stock\/items/i, ttl: THIRTY_SECONDS_MS },
  { re: /\/stock\/summary/i, ttl: THIRTY_SECONDS_MS },
  { re: /\/stock\/movements/i, ttl: THIRTY_SECONDS_MS },
  { re: /\/stock\/allocations/i, ttl: THIRTY_SECONDS_MS },
  { re: /\/stock\/lots/i, ttl: ONE_HOUR_MS },
  { re: /\/stock\/reason-codes/i, ttl: ONE_HOUR_MS },
  { re: /\/shipments\//i, ttl: THIRTY_SECONDS_MS },
  { re: /\/goods-receipts(\/[a-f0-9-]+)?\/?(\?.*)?$/i, ttl: THIRTY_SECONDS_MS },
  { re: /\/payments(\/|$)/i, ttl: THIRTY_SECONDS_MS },
  { re: /\/customers\/[a-f0-9-]+\/ledger/i, ttl: THIRTY_SECONDS_MS },
  { re: /\/customers\/[a-f0-9-]+\/aging/i, ttl: THIRTY_SECONDS_MS },
  { re: /\/customers\/[a-f0-9-]+\/open-invoices/i, ttl: THIRTY_SECONDS_MS },
  { re: /\/pricing\/resolve/i, ttl: THIRTY_SECONDS_MS },
  { re: /\/pricing\/customer-product-prices/i, ttl: ONE_HOUR_MS },
  { re: /\/accounting\/periods/i, ttl: ONE_HOUR_MS },
  { re: /\/mrp\/plan\//i, ttl: THIRTY_SECONDS_MS },
  { re: /\/mrp\/action-messages/i, ttl: THIRTY_SECONDS_MS },
  { re: /\/mrp\/pegging\//i, ttl: THIRTY_SECONDS_MS },
  { re: /\/payroll-parameters(\/[a-f0-9-]+)?\/?(\?.*)?$/i, ttl: ONE_HOUR_MS },
];

const matchTtl = (url: string): number => {
  for (const rule of TTL_RULES) {
    if (rule.re.test(url)) return rule.ttl;
  }
  return DEFAULT_TTL_MS;
};

let namespace = 'anon';
const memCache = new Map<string, CacheEntry<unknown>>();
const nextRevalidateAt = new Map<string, number>();
const inflight = new Map<string, { promise: Promise<unknown>; startedAt: number }>();

const CACHE_VERSION = 'v2';
const STORAGE_PREFIX = `corealign:${CACHE_VERSION}:httpcache:`;
const INFLIGHT_TIMEOUT_MS = 30_000;

const sanitizeNamespace = (ns: string): string => {
  return (ns || 'anon').replace(/[:\s]/g, '_').slice(0, 64);
};

const sortKeysDeep = (value: unknown): unknown => {
  if (value === null || typeof value !== 'object') return value;
  if (Array.isArray(value)) return value.map(sortKeysDeep);
  const obj = value as Record<string, unknown>;
  return Object.keys(obj)
    .sort()
    .reduce<Record<string, unknown>>((acc, k) => {
      acc[k] = sortKeysDeep(obj[k]);
      return acc;
    }, {});
};

const SENSITIVE_KEYS = new Set([
  'password',
  'secret',
  'salt',
  'token',
  'accesstoken',
  'refreshtoken',
]);

const sanitize = (value: unknown): unknown => {
  if (value === null || typeof value !== 'object') return value;
  if (Array.isArray(value)) return value.map(sanitize);
  const out: Record<string, unknown> = {};
  for (const [k, v] of Object.entries(value as Record<string, unknown>)) {
    if (SENSITIVE_KEYS.has(k.toLowerCase())) {
      out[k] = '***';
    } else {
      out[k] = sanitize(v);
    }
  }
  return out;
};

const buildKey = (url: string, params?: unknown): string => {
  const suffix = params ? `?${JSON.stringify(sortKeysDeep(params))}` : '';
  return `${STORAGE_PREFIX}${namespace}:${url}${suffix}`;
};

const readPersistent = <T>(key: string): CacheEntry<T> | null => {
  try {
    const raw = window.localStorage.getItem(key);
    if (!raw) return null;
    const entry = JSON.parse(raw) as CacheEntry<T>;
    if (typeof entry.expiry !== 'number') return null;
    return entry;
  } catch {
    return null;
  }
};

const writePersistent = <T>(key: string, entry: CacheEntry<T>): void => {
  try {
    const sanitised: CacheEntry<unknown> = { ...entry, data: sanitize(entry.data) };
    window.localStorage.setItem(key, JSON.stringify(sanitised));
  } catch (err) {
    logger.warn('httpCache.write failed (quota?)', {
      key,
      err: (err as Error)?.message,
    });
  }
};

const removePersistent = (key: string): void => {
  try {
    window.localStorage.removeItem(key);
  } catch {
    // WHY: localStorage removal is best-effort; failures must not break the cache
  }
};

export const setCacheNamespace = (ns: string): void => {
  namespace = sanitizeNamespace(ns);
};

try {
  if (typeof window !== 'undefined' && window.localStorage) {
    const STALE_PREFIX_RE = /^corealign:(?!v\d+:httpcache:).*httpcache:/;
    const stale: string[] = [];
    for (let i = 0; i < window.localStorage.length; i++) {
      const k = window.localStorage.key(i);
      if (!k) continue;
      if (
        k.startsWith('corealign:') &&
        k.includes(':httpcache:') &&
        !k.startsWith(STORAGE_PREFIX)
      ) {
        stale.push(k);
      } else if (STALE_PREFIX_RE.test(k)) {
        stale.push(k);
      }
    }
    stale.forEach((k) => window.localStorage.removeItem(k));
  }
} catch {
  // WHY: localStorage may be unavailable (private mode); the stale-cache sweep is best-effort
}

export const clearHttpCache = (): void => {
  memCache.clear();
  nextRevalidateAt.clear();
  inflight.clear();
  try {
    const toRemove: string[] = [];
    for (let i = 0; i < window.localStorage.length; i++) {
      const k = window.localStorage.key(i);
      if (k && k.startsWith(STORAGE_PREFIX)) toRemove.push(k);
    }
    toRemove.forEach((k) => window.localStorage.removeItem(k));
  } catch {
    // WHY: localStorage clear is best-effort; failures must not break the cache
  }
};

export const invalidateHttpCache = (patterns: readonly RegExp[]): void => {
  const memKeysToRemove: string[] = [];
  memCache.forEach((_, key) => {
    const urlPart = key.split(':').slice(1).join(':');
    if (patterns.some((p) => p.test(urlPart))) {
      memKeysToRemove.push(key);
    }
  });
  memKeysToRemove.forEach((k) => {
    memCache.delete(k);
    nextRevalidateAt.delete(k);
  });

  try {
    const toRemove: string[] = [];
    for (let i = 0; i < window.localStorage.length; i++) {
      const k = window.localStorage.key(i);
      if (!k || !k.startsWith(STORAGE_PREFIX)) continue;
      const tail = k.slice(STORAGE_PREFIX.length);
      const urlPart = tail.split(':').slice(1).join(':');
      if (patterns.some((p) => p.test(urlPart))) {
        toRemove.push(k);
      }
    }
    toRemove.forEach((k) => window.localStorage.removeItem(k));
  } catch {
    // WHY: localStorage invalidation is best-effort; failures must not break the cache
  }
};

interface CachedGetOptions {
  readonly overrideTtl?: number;
  readonly bypass?: boolean;
}

export const cachedGet = async <T>(
  axiosInstance: AxiosInstance,
  url: string,
  config: AxiosRequestConfig = {},
  options: CachedGetOptions = {},
): Promise<T> => {
  const ttl = options.bypass ? 0 : (options.overrideTtl ?? matchTtl(url));
  if (ttl <= 0) {
    const response = await axiosInstance.get<T>(url, config);
    return response.data;
  }

  const key = buildKey(url, config.params);
  const now = Date.now();

  let memEntry = memCache.get(key) as CacheEntry<T> | undefined;
  if (!memEntry) {
    const persisted = readPersistent<T>(key);
    if (persisted && persisted.expiry > now) {
      memCache.set(key, persisted);
      memEntry = persisted;
    } else if (persisted) {
      removePersistent(key);
    }
  }

  const nextRevalidation = nextRevalidateAt.get(key) ?? 0;
  if (memEntry && memEntry.expiry > now && now < nextRevalidation) {
    return memEntry.data;
  }

  const existing = inflight.get(key);
  if (existing) {
    if (now - existing.startedAt < INFLIGHT_TIMEOUT_MS) {
      return existing.promise as Promise<T>;
    }
    inflight.delete(key);
  }

  const requestConfig: AxiosRequestConfig = {
    ...config,
    headers: {
      ...config.headers,
      ...(memEntry?.etag ? { 'If-None-Match': memEntry.etag } : {}),
    },
    validateStatus: (s) => (s >= 200 && s < 300) || s === 304,
  };

  const promise = axiosInstance
    .get<T>(url, requestConfig)
    .then((response) => {
      if (response.status === 304 && memEntry) {
        const refreshed: CacheEntry<T> = { ...memEntry, expiry: now + ttl };
        memCache.set(key, refreshed);
        writePersistent(key, refreshed);
        nextRevalidateAt.set(key, now + REVALIDATE_INTERVAL_MS);
        return memEntry.data;
      }
      const etag =
        (response.headers?.etag as string | undefined) ??
        (response.headers?.ETag as string | undefined);
      const entry: CacheEntry<T> = {
        data: response.data,
        expiry: now + ttl,
        etag,
      };
      memCache.set(key, entry);
      writePersistent(key, entry);
      nextRevalidateAt.set(key, now + REVALIDATE_INTERVAL_MS);
      return response.data;
    })
    .finally(() => {
      inflight.delete(key);
    });

  inflight.set(key, { promise, startedAt: now });
  return promise;
};
