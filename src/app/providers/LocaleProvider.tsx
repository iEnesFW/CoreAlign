import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';

const RTL_LANGS = new Set(['ar', 'fa', 'he', 'ur']);

const resolveDir = (lng: string): 'rtl' | 'ltr' =>
  RTL_LANGS.has(lng.slice(0, 2).toLowerCase()) ? 'rtl' : 'ltr';

export const LocaleProvider = ({ children }: { children: React.ReactNode }) => {
  const { i18n } = useTranslation();

  useEffect(() => {
    const apply = (lng: string) => {
      const root = document.documentElement;
      root.lang = lng;
      root.dir = resolveDir(lng);
    };
    apply(i18n.language);
    i18n.on('languageChanged', apply);
    return () => {
      i18n.off('languageChanged', apply);
    };
  }, [i18n]);

  return <>{children}</>;
};
