import { logger } from './logger';

const TENANT_SCOPED_CACHES = ['api-glass-enclosure', 'api-installation'];

export const invalidateTenantScopedCaches = async (): Promise<void> => {
  if (typeof caches === 'undefined') return;
  const [, error] = await (async (): Promise<[void, Error | null]> => {
    try {
      const keys = await caches.keys();
      const targets = keys.filter((key) => TENANT_SCOPED_CACHES.includes(key));
      await Promise.all(targets.map((key) => caches.delete(key)));
      return [undefined, null];
    } catch (e) {
      return [undefined, e as Error];
    }
  })();
  if (error) {
    logger.warn('pwa-cache-invalidate-failed', { error: error.message });
  }
};
