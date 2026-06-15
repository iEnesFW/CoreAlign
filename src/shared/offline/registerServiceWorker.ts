import { registerSW } from 'virtual:pwa-register';
import { logger } from '@/shared/lib/logger';

export const registerServiceWorker = (): void => {
  if (typeof window === 'undefined') return;
  if (!('serviceWorker' in navigator)) return;

  try {
    registerSW({
      immediate: true,
      onRegisteredSW(_swUrl, registration) {
        logger.info('pwa.sw.registered', { scope: registration?.scope });
      },
      onRegisterError(error) {
        logger.error('pwa.sw.register.failed', error);
      },
      onOfflineReady() {
        logger.info('pwa.sw.offline-ready');
      },
      onNeedRefresh() {
        logger.info('pwa.sw.update-available');
      },
    });
  } catch (err) {
    logger.error('pwa.sw.bootstrap.failed', err);
  }
};
