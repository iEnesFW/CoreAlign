import { refreshLockSlot, type RefreshLockShape } from '@/shared/storage/slots';
import { logger } from '@/shared/lib/logger';

const LOCK_TTL_MS = 15_000;
const POLL_FALLBACK_MS = 250;

export const acquireRefreshLock = (): boolean => {
  const now = Date.now();
  const current = refreshLockSlot.get();
  if (current && current.until > now) return false;
  const lock: RefreshLockShape = { until: now + LOCK_TTL_MS };
  return refreshLockSlot.set(lock);
};

export const releaseRefreshLock = (): void => {
  refreshLockSlot.remove();
};

export const waitForRefreshLock = (timeoutMs = LOCK_TTL_MS): Promise<void> =>
  new Promise((resolve) => {
    const startedAt = Date.now();

    const finish = () => {
      window.removeEventListener('storage', onStorage);
      clearInterval(poll);
      resolve();
    };

    const isReleased = (): boolean => {
      const current = refreshLockSlot.get();
      return !current || current.until <= Date.now();
    };

    const onStorage = (e: StorageEvent) => {
      if (!e.key || !e.key.endsWith('auth:refresh-lock')) return;
      if (e.newValue === null) {
        finish();
        return;
      }
      if (isReleased()) finish();
    };

    const poll = window.setInterval(() => {
      if (isReleased()) {
        finish();
        return;
      }
      if (Date.now() - startedAt > timeoutMs) {
        logger.warn('refreshLock.timeout — proceeding anyway');
        finish();
      }
    }, POLL_FALLBACK_MS);

    window.addEventListener('storage', onStorage);
    if (isReleased()) finish();
  });
