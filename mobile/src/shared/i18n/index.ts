import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import * as Localization from 'expo-localization';
import en from './locales/en.json';
import tr from './locales/tr.json';
import de from './locales/de.json';
import ru from './locales/ru.json';
import ar from './locales/ar.json';

export interface LocaleDescriptor {
  code: string;
  label: string;
  nativeLabel: string;
  dir: 'ltr' | 'rtl';
}

export const SUPPORTED_LOCALES: readonly LocaleDescriptor[] = Object.freeze([
  { code: 'en', label: 'English', nativeLabel: 'English', dir: 'ltr' },
  { code: 'tr', label: 'Turkish', nativeLabel: 'Türkçe', dir: 'ltr' },
  { code: 'de', label: 'German', nativeLabel: 'Deutsch', dir: 'ltr' },
  { code: 'ru', label: 'Russian', nativeLabel: 'Русский', dir: 'ltr' },
  { code: 'ar', label: 'Arabic', nativeLabel: 'العربية', dir: 'rtl' },
]);

const RTL_CODES = new Set(['ar', 'fa', 'he', 'ur']);

export const isRtlLocale = (code: string): boolean => RTL_CODES.has(code.slice(0, 2).toLowerCase());

const SUPPORTED_CODES = SUPPORTED_LOCALES.map((l) => l.code);

export const resolveLocale = (candidate: string | null | undefined): string => {
  if (!candidate) return 'en';
  const base = candidate.slice(0, 2).toLowerCase();
  return SUPPORTED_CODES.includes(base) ? base : 'en';
};

const detectInitialLocale = (): string => {
  const locales = Localization.getLocales();
  const first = locales[0]?.languageCode ?? 'en';
  return resolveLocale(first);
};

void i18n.use(initReactI18next).init({
  resources: {
    en: { translation: en },
    tr: { translation: tr },
    de: { translation: de },
    ru: { translation: ru },
    ar: { translation: ar },
  },
  lng: detectInitialLocale(),
  fallbackLng: 'en',
  interpolation: { escapeValue: false },
  returnNull: false,
  compatibilityJSON: 'v4',
});

export default i18n;
