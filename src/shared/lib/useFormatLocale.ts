import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { resolveFormatLocale } from './locale';

/**
 * Region-aware BCP-47 locale for date/number/currency formatting. Pass the
 * result to the helpers in `format.ts` instead of `i18n.language` so amounts and
 * dates follow the user's region, not just the UI language.
 */
export const useFormatLocale = (): string => {
  const { i18n } = useTranslation();
  return useMemo(() => resolveFormatLocale(i18n.language), [i18n.language]);
};
