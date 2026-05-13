import { toast } from 'sonner';
import { logger } from '@/shared/lib/logger';

const DEDUPE_WINDOW_MS = 10_000;
const STICKY_MAX = 3;
const STICKY_LENGTH_THRESHOLD = 160;
const DEDUPE_CACHE_LIMIT = 256;

const lastShownAt = new Map<string, number>();
let stickyCount = 0;

const evictIfFull = (): void => {
  if (lastShownAt.size <= DEDUPE_CACHE_LIMIT) return;
  const toDrop = lastShownAt.size - DEDUPE_CACHE_LIMIT;
  let i = 0;
  for (const key of lastShownAt.keys()) {
    if (i++ >= toDrop) break;
    lastShownAt.delete(key);
  }
};

const shouldSuppress = (dedupeKey: string, now: number): boolean => {
  const last = lastShownAt.get(dedupeKey);
  if (last && now - last < DEDUPE_WINDOW_MS) return true;
  lastShownAt.set(dedupeKey, now);
  evictIfFull();
  return false;
};

export interface QueuedToastOptions {
  readonly dedupeKey: string;
  readonly description: string;
  readonly variant?: 'error' | 'success' | 'info' | 'warning';
}

export const queueToast = ({
  dedupeKey,
  description,
  variant = 'error',
}: QueuedToastOptions): void => {
  if (!description) return;
  const now = Date.now();
  if (shouldSuppress(dedupeKey, now)) return;

  const isSticky = description.length >= STICKY_LENGTH_THRESHOLD;
  if (isSticky && stickyCount >= STICKY_MAX) {
    logger.debug('toastQueue.sticky-cap-reached', { dedupeKey });
    return;
  }

  const toastOpts: { duration?: number; onAutoClose?: () => void; onDismiss?: () => void } = {};
  if (isSticky) {
    stickyCount += 1;
    toastOpts.duration = Infinity;
    const release = () => {
      stickyCount = Math.max(0, stickyCount - 1);
    };
    toastOpts.onAutoClose = release;
    toastOpts.onDismiss = release;
  }

  switch (variant) {
    case 'success':
      toast.success(description, toastOpts);
      break;
    case 'info':
      toast.info(description, toastOpts);
      break;
    case 'warning':
      toast.warning(description, toastOpts);
      break;
    default:
      toast.error(description, toastOpts);
  }
};

export const resetToastQueue = (): void => {
  lastShownAt.clear();
  stickyCount = 0;
};
