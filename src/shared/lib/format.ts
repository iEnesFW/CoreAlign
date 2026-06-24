const numberFormatters = new Map<string, Intl.NumberFormat>();
const currencyFormatters = new Map<string, Intl.NumberFormat>();
const dateFormatters = new Map<string, Intl.DateTimeFormat>();
const dateTimeFormatters = new Map<string, Intl.DateTimeFormat>();

const getNumberFormatter = (locale: string, fractionDigits: number): Intl.NumberFormat => {
  const key = `${locale}|${fractionDigits}`;
  let f = numberFormatters.get(key);
  if (!f) {
    f = new Intl.NumberFormat(locale, {
      minimumFractionDigits: fractionDigits,
      maximumFractionDigits: fractionDigits,
    });
    numberFormatters.set(key, f);
  }
  return f;
};

const getCurrencyFormatter = (
  locale: string,
  currency: string,
  fractionDigits: number,
): Intl.NumberFormat => {
  const key = `${locale}|${currency}|${fractionDigits}`;
  let f = currencyFormatters.get(key);
  if (!f) {
    try {
      f = new Intl.NumberFormat(locale, {
        style: 'currency',
        currency,
        minimumFractionDigits: fractionDigits,
        maximumFractionDigits: fractionDigits,
      });
    } catch {
      f = new Intl.NumberFormat(locale, {
        minimumFractionDigits: fractionDigits,
        maximumFractionDigits: fractionDigits,
      });
    }
    currencyFormatters.set(key, f);
  }
  return f;
};

export const formatNumber = (value: number, locale: string, fractionDigits = 2): string =>
  getNumberFormatter(locale, fractionDigits).format(value);

export const formatCurrency = (
  value: number,
  locale: string,
  currency = 'TRY',
  fractionDigits = 2,
): string => {
  const formatter = getCurrencyFormatter(locale, currency, fractionDigits);
  const formatted = formatter.format(value);
  return formatter.resolvedOptions().style === 'currency' ? formatted : `${formatted} ${currency}`;
};

export const formatDate = (value: string | Date | null | undefined, locale: string): string => {
  if (!value) return '—';
  const key = locale;
  let f = dateFormatters.get(key);
  if (!f) {
    f = new Intl.DateTimeFormat(locale, { dateStyle: 'medium' });
    dateFormatters.set(key, f);
  }
  try {
    return f.format(typeof value === 'string' ? new Date(value) : value);
  } catch {
    return typeof value === 'string' ? value.slice(0, 10) : '—';
  }
};

export const formatDateTime = (value: string | Date | null | undefined, locale: string): string => {
  if (!value) return '—';
  let f = dateTimeFormatters.get(locale);
  if (!f) {
    f = new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' });
    dateTimeFormatters.set(locale, f);
  }
  try {
    return f.format(typeof value === 'string' ? new Date(value) : value);
  } catch {
    return typeof value === 'string' ? value.slice(0, 16) : '—';
  }
};
