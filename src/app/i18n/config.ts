import i18n from 'i18next';
import LanguageDetector from 'i18next-browser-languagedetector';
import { initReactI18next } from 'react-i18next';
import { registerRtlListener } from './rtl';
import { SUPPORTED_LOCALE_CODES, resolveLocale } from './supportedLocales';

export const defaultNS = 'translation';

const localeLoaders: Record<string, () => Promise<{ default: Record<string, unknown> }>> = {
  en: () => import('./locales/en.json'),
  tr: () => import('./locales/tr.json'),
  ar: () => import('./locales/ar.json'),
  de: () => import('./locales/de.json'),
  ru: () => import('./locales/ru.json'),
};

const loadLocale = async (lng: string): Promise<Record<string, unknown>> => {
  const loader = localeLoaders[lng] ?? localeLoaders.en;
  const mod = await loader();
  return mod.default;
};

const detector = new LanguageDetector();
const detected =
  (typeof window !== 'undefined' && window.localStorage.getItem('corealign.lang')) ||
  (typeof navigator !== 'undefined' && navigator.language?.slice(0, 2)) ||
  'en';
const initialPath = typeof window !== 'undefined' ? window.location.pathname : '';
const landingRouteLanguage =
  initialPath === '/en' || initialPath.startsWith('/en/')
    ? 'en'
    : initialPath === '/'
      ? 'tr'
      : undefined;
const initialLng = resolveLocale(landingRouteLanguage ?? detected);

const initialResources = await (async () => ({
  [initialLng]: { translation: await loadLocale(initialLng) },
}))();

i18n
  .use(detector)
  .use(initReactI18next)
  .init({
    fallbackLng: 'en',
    supportedLngs: [...SUPPORTED_LOCALE_CODES],
    nonExplicitSupportedLngs: true,
    ns: ['translation'],
    defaultNS,
    resources: initialResources,
    lng: initialLng,
    detection: {
      order: ['localStorage', 'navigator', 'htmlTag'],
      caches: ['localStorage'],
      lookupLocalStorage: 'corealign.lang',
    },
    interpolation: {
      escapeValue: false,
    },
  });

registerRtlListener(i18n);

export const changeI18nLanguage = async (lng: string) => {
  const resolved = resolveLocale(lng);
  if (!i18n.hasResourceBundle(resolved, defaultNS)) {
    const bundle = await loadLocale(resolved);
    i18n.addResourceBundle(resolved, defaultNS, bundle, true, true);
  }
  await i18n.changeLanguage(resolved);
};

if (typeof document !== 'undefined') {
  document.documentElement.lang = initialLng;
}

i18n.on('languageChanged', (lng) => {
  if (typeof document !== 'undefined') {
    document.documentElement.lang = lng;
  }
});

export default i18n;
