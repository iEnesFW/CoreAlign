import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { resolveFormatLocale } from './locale';

export const useFormatLocale = (): string => {
  const { i18n } = useTranslation();
  return useMemo(() => resolveFormatLocale(i18n.language), [i18n.language]);
};
