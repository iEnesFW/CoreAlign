export const fmtCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

export const fmtPercent = (value: number, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { maximumFractionDigits: 1 }).format(value) + '%';
  } catch {
    return `${value.toFixed(1)}%`;
  }
};

export const fmtDate = (iso: string | null, locale: string) => {
  if (!iso) return '—';
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(new Date(iso));
  } catch {
    return iso.slice(0, 10);
  }
};

export const fmtRelative = (iso: string | null, locale: string) => {
  if (!iso) return null;
  try {
    const target = new Date(iso).getTime();
    const diffMs = Date.now() - target;
    const dayMs = 1000 * 60 * 60 * 24;
    const days = Math.floor(diffMs / dayMs);
    const rtf = new Intl.RelativeTimeFormat(locale, { numeric: 'auto' });
    if (days < 1) {
      const hours = Math.floor(diffMs / (1000 * 60 * 60));
      if (hours < 1) return rtf.format(-Math.max(1, Math.floor(diffMs / (1000 * 60))), 'minute');
      return rtf.format(-hours, 'hour');
    }
    if (days < 30) return rtf.format(-days, 'day');
    if (days < 365) return rtf.format(-Math.floor(days / 30), 'month');
    return rtf.format(-Math.floor(days / 365), 'year');
  } catch {
    return null;
  }
};
