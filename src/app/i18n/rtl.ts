import type { i18n as I18n } from 'i18next';
import { isRtlLocale } from './supportedLocales';

export const applyDocumentDirection = (locale: string): void => {
  if (typeof document === 'undefined') return;
  const code = locale.slice(0, 2).toLowerCase();
  const dir = isRtlLocale(code) ? 'rtl' : 'ltr';
  document.documentElement.dir = dir;
  document.documentElement.lang = code;
  document.documentElement.dataset.dir = dir;
};

export const registerRtlListener = (instance: I18n): void => {
  applyDocumentDirection(instance.language ?? 'en');
  instance.on('languageChanged', (lng: string) => {
    applyDocumentDirection(lng ?? 'en');
  });
};
