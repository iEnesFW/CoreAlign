import i18n from 'i18next';
import LanguageDetector from 'i18next-browser-languagedetector';
import { initReactI18next } from 'react-i18next';

export const defaultNS = 'translation';

const localeLoaders: Record<string, () => Promise<{ default: Record<string, unknown> }>> = {
  en: () => import('./locales/en.json'),
  tr: () => import('./locales/tr.json'),
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
const initialLng = localeLoaders[detected] ? detected : 'en';

const initialResources = await (async () => ({
  [initialLng]: { translation: await loadLocale(initialLng) },
}))();

i18n
  .use(detector)
  .use(initReactI18next)
  .init({
    fallbackLng: 'en',
    supportedLngs: ['en', 'tr'],
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

// Lazily fetch the other language on demand so the user only pays for the
// locale they actually use; switching languages downloads ~halve the i18n
// bundle in a single request.
i18n.on('languageChanged', async (lng) => {
  if (i18n.hasResourceBundle(lng, 'translation')) return;
  const bundle = await loadLocale(lng);
  i18n.addResourceBundle(lng, 'translation', bundle, true, true);
});

export default i18n;
