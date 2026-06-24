import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';

export const useRelativeTime = () => {
  const { i18n } = useTranslation();
  const locale = useFormatLocale();

  const formatter = useMemo(() => {
    try {
      return new Intl.RelativeTimeFormat(locale, { numeric: 'auto' });
    } catch {
      return new Intl.RelativeTimeFormat(i18n.language || 'en', { numeric: 'auto' });
    }
  }, [locale, i18n.language]);

  return (iso: string): string => {
    const parsed = Date.parse(iso);
    if (Number.isNaN(parsed)) return iso;
    const diffSeconds = Math.round((parsed - Date.now()) / 1000);
    const abs = Math.abs(diffSeconds);
    if (abs < 60) return formatter.format(diffSeconds, 'second');
    if (abs < 3600) return formatter.format(Math.round(diffSeconds / 60), 'minute');
    if (abs < 86400) return formatter.format(Math.round(diffSeconds / 3600), 'hour');
    if (abs < 604800) return formatter.format(Math.round(diffSeconds / 86400), 'day');
    if (abs < 2592000) return formatter.format(Math.round(diffSeconds / 604800), 'week');
    if (abs < 31536000) return formatter.format(Math.round(diffSeconds / 2592000), 'month');
    return formatter.format(Math.round(diffSeconds / 31536000), 'year');
  };
};
