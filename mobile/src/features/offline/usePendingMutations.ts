import { useCallback, useEffect, useState } from 'react';
import { getPendingSummary, type PendingMutationsSummary } from './syncQueue';

const EMPTY: PendingMutationsSummary = { total: 0, failed: 0, oldestCreatedAt: null };

const POLL_INTERVAL_MS = 5000;

export const usePendingMutations = (
  pollIntervalMs: number = POLL_INTERVAL_MS,
): {
  summary: PendingMutationsSummary;
  refresh: () => Promise<void>;
} => {
  const [summary, setSummary] = useState<PendingMutationsSummary>(EMPTY);

  const refresh = useCallback(async (): Promise<void> => {
    try {
      const next = await getPendingSummary();
      setSummary(next);
    } catch {
      // ignore — DB may be opening
    }
  }, []);

  useEffect(() => {
    let cancelled = false;
    const tick = async (): Promise<void> => {
      if (cancelled) return;
      await refresh();
    };
    void tick();
    const handle = setInterval(() => {
      void tick();
    }, pollIntervalMs);
    return () => {
      cancelled = true;
      clearInterval(handle);
    };
  }, [pollIntervalMs, refresh]);

  return { summary, refresh };
};
