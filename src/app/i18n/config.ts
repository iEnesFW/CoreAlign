import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

import en from './locales/en.json';
import tr from './locales/tr.json';

export const defaultNS = 'translation';
export const resources = {
    en: {
        translation: en,
    },
    tr: {
        translation: tr,
    },
} as const;

i18n.use(initReactI18next).init({
    lng: 'en', // default language
    fallbackLng: 'en',
    ns: ['translation'],
    defaultNS,
    resources,
    interpolation: {
        escapeValue: false, // react already safes from xss
    },
});

export default i18n;
