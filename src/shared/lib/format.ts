/**
 * Centralized locale-aware formatting. Replaces the per-page `Intl.NumberFormat`
 * / `Intl.DateTimeFormat` helpers that were copy-pasted across Vendors,
 * Products, Accounting, etc. Formatters are memoized per (locale, options) so
 * repeated calls in a render loop don't re-instantiate the (expensive) Intl
 * objects.
 */

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
      // Unknown ISO code → fall back to a plain number; caller appends the code.
      f = new Intl.NumberFormat(locale, {
        minimumFractionDigits: fractionDigits,
        maximumFractionDigits: fractionDigits,
      });
    }
    currencyFormatters.set(key, f);
  }
  return f;
};

/** Format a number with a fixed number of fraction digits (default 2). */
export const formatNumber = (value: number, locale: string, fractionDigits = 2): string =>
  getNumberFormatter(locale, fractionDigits).format(value);

/** Format a monetary amount in the given ISO currency. Falls back gracefully. */
export const formatCurrency = (
  value: number,
  locale: string,
  currency = 'TRY',
  fractionDigits = 2,
): string => {
  const formatter = getCurrencyFormatter(locale, currency, fractionDigits);
  const formatted = formatter.format(value);
  // When the currency style failed we appended nothing, so add the code.
  return formatter.resolvedOptions().style === 'currency' ? formatted : `${formatted} ${currency}`;
};

/** Medium date (e.g. "20 May 2026"). Accepts ISO string or Date. */
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

/** Medium date + short time. */
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
