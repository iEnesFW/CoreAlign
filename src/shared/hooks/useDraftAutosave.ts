import { useCallback, useEffect, useRef, useState } from 'react';
import { logger } from '@/shared/lib/logger';

interface DraftAutosaveOptions {
  enabled: boolean;
  intervalMs?: number;
}

interface DraftAutosaveResult<T> {
  lastSavedAt: number | null;
  peekDraft: () => T | null;
  saveNow: () => void;
  clearDraft: () => void;
}

export function useDraftAutosave<T>(
  key: string,
  value: T,
  { enabled, intervalMs = 30000 }: DraftAutosaveOptions,
): DraftAutosaveResult<T> {
  const valueRef = useRef(value);
  useEffect(() => {
    valueRef.current = value;
  }, [value]);

  const [lastSavedAt, setLastSavedAt] = useState<number | null>(null);

  const write = useCallback(() => {
    if (typeof window === 'undefined') return;
    try {
      window.localStorage.setItem(key, JSON.stringify(valueRef.current));
      setLastSavedAt(Date.now());
    } catch (err) {
      logger.debug('useDraftAutosave: write failed', { key, err });
    }
  }, [key]);

  const clearDraft = useCallback(() => {
    if (typeof window === 'undefined') return;
    try {
      window.localStorage.removeItem(key);
    } catch (err) {
      logger.debug('useDraftAutosave: clear failed', { key, err });
    }
    setLastSavedAt(null);
  }, [key]);

  const peekDraft = useCallback((): T | null => {
    if (typeof window === 'undefined') return null;
    try {
      const raw = window.localStorage.getItem(key);
      return raw === null ? null : (JSON.parse(raw) as T);
    } catch {
      return null;
    }
  }, [key]);

  useEffect(() => {
    if (!enabled) return undefined;
    const id = window.setInterval(write, intervalMs);
    return () => window.clearInterval(id);
  }, [enabled, intervalMs, write]);

  return { lastSavedAt, peekDraft, saveNow: write, clearDraft };
}
