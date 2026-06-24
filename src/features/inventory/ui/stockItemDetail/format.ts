import type { StockMovementType } from '@/features/inventory/model/inventory.types';

export const fmtNumber = (n: number, locale: string, decimals = 2) =>
  new Intl.NumberFormat(locale, {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(n);

export const fmtInt = (n: number, locale: string) => new Intl.NumberFormat(locale).format(n);

export const fmtCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
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

export const fmtDateTime = (iso: string | null, locale: string) => {
  if (!iso) return '—';
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'short', timeStyle: 'short' }).format(
      new Date(iso),
    );
  } catch {
    return iso;
  }
};

const inboundTypes: StockMovementType[] = [
  'OpeningBalance',
  'Receipt',
  'TransferIn',
  'AdjustmentPositive',
  'CountVariancePositive',
  'UnReservation',
];

export const isInbound = (type: StockMovementType) => inboundTypes.includes(type);
