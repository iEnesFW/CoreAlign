import { useCallback, useState } from 'react';
import { logger } from '@/shared/lib/logger';

/**
 * useState whose value is mirrored to localStorage so a user's UI preference
 * (collapsed sections, page size, density…) survives reloads. Reads are
 * lazy-initialized once; writes are best-effort (private-mode / quota errors
 * are swallowed so the UI never breaks because storage is unavailable).
 */
export function usePersistedState<T>(
  key: string,
  defaultValue: T,
): [T, (value: T | ((prev: T) => T)) => void] {
  const [state, setState] = useState<T>(() => {
    if (typeof window === 'undefined') return defaultValue;
    try {
      const raw = window.localStorage.getItem(key);
      return raw === null ? defaultValue : (JSON.parse(raw) as T);
    } catch {
      return defaultValue;
    }
  });

  const setPersisted = useCallback(
    (value: T | ((prev: T) => T)) => {
      setState((prev) => {
        const next = typeof value === 'function' ? (value as (p: T) => T)(prev) : value;
        try {
          window.localStorage.setItem(key, JSON.stringify(next));
        } catch (err) {
          logger.debug('usePersistedState: write failed', { key, err });
        }
        return next;
      });
    },
    [key],
  );

  return [state, setPersisted];
}
