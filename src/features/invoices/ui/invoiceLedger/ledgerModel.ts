import type { Invoice } from '@/features/invoices/model/invoice.types';

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

const daysFromNow = (iso: string | null) => {
  if (!iso) return null;
  const target = new Date(iso).getTime();
  if (Number.isNaN(target)) return null;
  const dayMs = 1000 * 60 * 60 * 24;
  return Math.round((target - Date.now()) / dayMs);
};

export interface GlEntry {
  account: string;
  description: string;
  debit: number;
  credit: number;
}

export const buildGlEntries = (invoice: Invoice): GlEntry[] => {
  const entries: GlEntry[] = [];
  const customerAcct = '120 — Accounts Receivable';
  const revenueAcct = '600 — Sales Revenue';
  const discountAcct = '611 — Sales Discounts';
  const taxAcct = '391 — VAT Output';
  const withholdingAcct = '193 — Withholding Tax';
  const shippingAcct = '623 — Shipping Revenue';

  entries.push({
    account: customerAcct,
    description: `${invoice.invoiceNumber} — ${invoice.customerName}`,
    debit: invoice.total,
    credit: 0,
  });

  const netRevenue =
    invoice.taxableTotal ||
    invoice.subtotal - invoice.lineDiscountTotal - invoice.headerDiscountAmount;
  if (netRevenue > 0) {
    entries.push({
      account: revenueAcct,
      description: 'Net of discounts',
      debit: 0,
      credit: netRevenue,
    });
  }

  if (invoice.lineDiscountTotal + invoice.headerDiscountAmount > 0) {
    entries.push({
      account: discountAcct,
      description: 'Discounts granted',
      debit: invoice.lineDiscountTotal + invoice.headerDiscountAmount,
      credit: 0,
    });
  }

  if (invoice.taxTotal > 0) {
    entries.push({
      account: taxAcct,
      description: 'Output VAT',
      debit: 0,
      credit: invoice.taxTotal,
    });
  }

  if (invoice.withholdingTotal > 0) {
    entries.push({
      account: withholdingAcct,
      description: 'Withholding tax',
      debit: invoice.withholdingTotal,
      credit: 0,
    });
  }

  if (invoice.shippingCost > 0) {
    entries.push({
      account: shippingAcct,
      description: 'Shipping revenue',
      debit: 0,
      credit: invoice.shippingCost,
    });
  }

  return entries;
};

export interface DunningLevel {
  level: 0 | 1 | 2 | 3;
  daysPastDue: number;
  tone: 'slate' | 'amber' | 'orange' | 'red';
  label: string;
}

export const computeDunningLevel = (invoice: Invoice): DunningLevel => {
  if (
    invoice.amountDue <= 0 ||
    invoice.status === 'Paid' ||
    invoice.status === 'Cancelled' ||
    invoice.status === 'Void'
  ) {
    return { level: 0, daysPastDue: 0, tone: 'slate', label: 'Clear' };
  }
  const days = -(daysFromNow(invoice.dueDate) ?? 0);
  if (days <= 0) return { level: 0, daysPastDue: 0, tone: 'slate', label: 'Current' };
  if (days <= 14) return { level: 1, daysPastDue: days, tone: 'amber', label: 'Friendly reminder' };
  if (days <= 30) return { level: 2, daysPastDue: days, tone: 'orange', label: 'Second notice' };
  return { level: 3, daysPastDue: days, tone: 'red', label: 'Final notice / Collection' };
};

export const dunningToneBg: Record<DunningLevel['tone'], string> = {
  slate: 'border-slate-200 dark:border-slate-800',
  amber: 'border-warning-300 dark:border-warning-500/40',
  orange: 'border-warning-300 dark:border-warning-500/40',
  red: 'border-danger-300 dark:border-danger-500/40',
};

export const dunningToneText: Record<DunningLevel['tone'], string> = {
  slate: 'text-slate-700 dark:text-slate-200',
  amber: 'text-warning-700 dark:text-warning-400',
  orange: 'text-warning-700 dark:text-warning-400',
  red: 'text-danger-700 dark:text-danger-400',
};

export const dunningToneBadge: Record<DunningLevel['tone'], 'neutral' | 'warning' | 'error'> = {
  slate: 'neutral',
  amber: 'warning',
  orange: 'warning',
  red: 'error',
};
