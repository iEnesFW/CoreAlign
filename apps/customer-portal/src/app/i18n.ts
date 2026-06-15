import i18n from 'i18next';
import LanguageDetector from 'i18next-browser-languagedetector';
import { initReactI18next } from 'react-i18next';
import { countryCodeFromTimezone, detectTimezone } from '@/shared/lib/geo';
import en from './locales/en.json';
import tr from './locales/tr.json';

const LANG_STORAGE_KEY = 'corealign.lang';

const REGION_TO_LANG: Record<string, string> = { tr: 'tr' };

const SUPPORTED = ['en', 'tr'] as const;
type SupportedLang = (typeof SUPPORTED)[number];

const resolveInitialLng = (): SupportedLang => {
  const stored =
    typeof window !== 'undefined' ? window.localStorage.getItem(LANG_STORAGE_KEY) : null;
  if (stored && (SUPPORTED as readonly string[]).includes(stored)) return stored as SupportedLang;

  const region = countryCodeFromTimezone(detectTimezone());
  const fromRegion = region ? REGION_TO_LANG[region] : undefined;
  if (fromRegion && (SUPPORTED as readonly string[]).includes(fromRegion)) {
    return fromRegion as SupportedLang;
  }

  const nav = typeof navigator !== 'undefined' ? navigator.language?.slice(0, 2) : undefined;
  return nav && (SUPPORTED as readonly string[]).includes(nav) ? (nav as SupportedLang) : 'en';
};

void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      en: { translation: en },
      tr: { translation: tr },
    },
    lng: resolveInitialLng(),
    fallbackLng: 'en',
    supportedLngs: [...SUPPORTED],
    nonExplicitSupportedLngs: true,
    interpolation: { escapeValue: false },
    detection: {
      order: ['localStorage', 'navigator', 'htmlTag'],
      caches: ['localStorage'],
      lookupLocalStorage: LANG_STORAGE_KEY,
    },
  });

export default i18n;
