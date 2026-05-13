import { logger } from '@/shared/lib/logger';

const STORAGE_VERSION = 'v1';
const KEY_PREFIX = `corealign:${STORAGE_VERSION}:`;

const buildKey = (key: string) => `${KEY_PREFIX}${key}`;

const safeGetItem = (key: string): string | null => {
  try {
    return window.localStorage.getItem(buildKey(key));
  } catch (err) {
    logger.warn('storage.getItem failed', { key, err: (err as Error)?.message });
    return null;
  }
};

const safeSetItem = (key: string, value: string): boolean => {
  try {
    window.localStorage.setItem(buildKey(key), value);
    return true;
  } catch (err) {
    logger.warn('storage.setItem failed (likely quota)', { key, err: (err as Error)?.message });
    return false;
  }
};

const safeRemoveItem = (key: string): void => {
  try {
    window.localStorage.removeItem(buildKey(key));
  } catch (err) {
    logger.warn('storage.removeItem failed', { key, err: (err as Error)?.message });
  }
};

export interface StorageSlot<T> {
  readonly key: string;
  get(): T | null;
  set(value: T): boolean;
  remove(): void;
  subscribe(listener: (value: T | null) => void): () => void;
}

interface StorageSlotOptions<T> {
  readonly key: string;
  readonly schema?: (raw: unknown) => T | null;
}

const matchesPrefixedKey = (storageKey: string | null, prefixedKey: string): boolean =>
  storageKey === prefixedKey;

export const createStorageSlot = <T>(options: StorageSlotOptions<T>): StorageSlot<T> => {
  const { key, schema } = options;
  const prefixedKey = buildKey(key);
  const listeners = new Set<(value: T | null) => void>();

  const read = (): T | null => {
    const raw = safeGetItem(key);
    if (raw === null) return null;
    try {
      const parsed = JSON.parse(raw) as unknown;
      if (schema) {
        return schema(parsed);
      }
      return parsed as T;
    } catch (err) {
      logger.warn('storage.parse failed; removing corrupted entry', {
        key,
        err: (err as Error)?.message,
      });
      safeRemoveItem(key);
      return null;
    }
  };

  const notify = (value: T | null): void => {
    listeners.forEach((listener) => {
      try {
        listener(value);
      } catch (err) {
        logger.warn('storage.listener threw', { key, err: (err as Error)?.message });
      }
    });
  };

  return {
    key,
    get: read,
    set: (value: T) => {
      const ok = safeSetItem(key, JSON.stringify(value));
      if (ok) notify(value);
      return ok;
    },
    remove: () => {
      safeRemoveItem(key);
      notify(null);
    },
    subscribe: (listener) => {
      listeners.add(listener);
      const handler = (e: StorageEvent) => {
        if (!matchesPrefixedKey(e.key, prefixedKey)) return;
        if (e.newValue === null) {
          listener(null);
          return;
        }
        try {
          const parsed = JSON.parse(e.newValue) as unknown;
          listener(schema ? schema(parsed) : (parsed as T));
        } catch {
          listener(null);
        }
      };
      window.addEventListener('storage', handler);
      return () => {
        listeners.delete(listener);
        window.removeEventListener('storage', handler);
      };
    },
  };
};

export const clearAllStorage = (): void => {
  try {
    const keysToRemove: string[] = [];
    for (let i = 0; i < window.localStorage.length; i++) {
      const key = window.localStorage.key(i);
      if (key && key.startsWith(KEY_PREFIX)) keysToRemove.push(key);
    }
    keysToRemove.forEach((k) => window.localStorage.removeItem(k));
  } catch (err) {
    logger.warn('storage.clearAll failed', { err: (err as Error)?.message });
  }
};

export const listStorageKeys = (): string[] => {
  const keys: string[] = [];
  try {
    for (let i = 0; i < window.localStorage.length; i++) {
      const k = window.localStorage.key(i);
      if (k && k.startsWith(KEY_PREFIX)) keys.push(k.slice(KEY_PREFIX.length));
    }
  } catch (err) {
    logger.warn('storage.list failed', { err: (err as Error)?.message });
  }
  return keys;
};
