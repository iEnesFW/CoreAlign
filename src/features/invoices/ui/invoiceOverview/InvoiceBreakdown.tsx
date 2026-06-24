import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Banknote, CheckCircle2, CircleDot, FileBadge } from 'lucide-react';
import type { Invoice, TaxBreakdownItem } from '@/features/invoices/model/invoice.types';
import { fmtCurrency, fmtDate } from './format';

export const FinancialBreakdown = ({ invoice, locale }: { invoice: Invoice; locale: string }) => {
  const { t } = useTranslation();
  const rows = useMemo(
    () =>
      [
        { label: t('orders.detail.financial.subtotal'), value: invoice.subtotal, bold: false },
        invoice.lineDiscountTotal > 0 && {
          label: t('orders.detail.financial.lineDiscount'),
          value: -invoice.lineDiscountTotal,
          bold: false,
          tone: 'discount' as const,
        },
        invoice.headerDiscountAmount > 0 && {
          label: t('orders.detail.financial.headerDiscount', {
            pct: invoice.headerDiscountPercent,
          }),
          value: -invoice.headerDiscountAmount,
          bold: false,
          tone: 'discount' as const,
        },
        invoice.taxableTotal !== invoice.subtotal && {
          label: t('orders.detail.financial.taxable'),
          value: invoice.taxableTotal,
          bold: false,
        },
        invoice.taxTotal > 0 && {
          label: t('orders.detail.financial.tax'),
          value: invoice.taxTotal,
          bold: false,
        },
        invoice.withholdingTotal > 0 && {
          label: t('orders.detail.financial.withholding'),
          value: -invoice.withholdingTotal,
          bold: false,
          tone: 'discount' as const,
        },
        invoice.shippingCost > 0 && {
          label: t('orders.detail.financial.shipping'),
          value: invoice.shippingCost,
          bold: false,
        },
        invoice.roundingAdjustment !== 0 && {
          label: t('orders.detail.financial.rounding'),
          value: invoice.roundingAdjustment,
          bold: false,
        },
        { label: t('orders.detail.financial.total'), value: invoice.total, bold: true },
      ].filter(Boolean) as {
        label: string;
        value: number;
        bold: boolean;
        tone?: 'discount';
      }[],
    [invoice, t],
  );

  return (
    <section className="rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center gap-1.5 px-3 py-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <Banknote size={12} />
        {t('orders.detail.financial.title')}
      </header>
      <dl className="divide-y divide-slate-100 dark:divide-slate-800">
        {rows.map((row, i) => (
          <div key={`${row.label}-${i}`} className="flex items-center justify-between px-3 py-1.5">
            <dt
              className={`text-[11px] ${row.bold ? 'font-semibold text-slate-900 dark:text-slate-100' : 'text-slate-600 dark:text-slate-300'}`}
            >
              {row.label}
            </dt>
            <dd
              className={`text-[11px] tabular-nums ${
                row.bold
                  ? 'text-base font-bold text-slate-900 dark:text-slate-100'
                  : row.tone === 'discount'
                    ? 'font-medium text-success-600 dark:text-success-400'
                    : 'font-medium text-slate-700 dark:text-slate-200'
              }`}
            >
              {fmtCurrency(row.value, invoice.currency, locale)}
            </dd>
          </div>
        ))}
      </dl>
    </section>
  );
};

export const TaxBreakdownCard = ({
  items,
  currency,
  locale,
}: {
  items: TaxBreakdownItem[];
  currency: string;
  locale: string;
}) => {
  const { t } = useTranslation();
  const totalBase = items.reduce((s, i) => s + i.base, 0);
  const totalTax = items.reduce((s, i) => s + i.amount, 0);
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <CircleDot size={12} />
        {t('invoices.detail.taxBreakdown')}
      </header>
      <div className="mt-2 overflow-hidden rounded border border-slate-100 dark:border-slate-800">
        <table className="w-full text-left text-[11px]">
          <thead className="bg-slate-50 dark:bg-slate-800/50">
            <tr>
              <th className="px-2 py-1 font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('invoices.detail.tax.rate')}
              </th>
              <th className="px-2 py-1 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('invoices.detail.tax.base')}
              </th>
              <th className="px-2 py-1 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('invoices.detail.tax.amount')}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {items.map((item, idx) => (
              <tr key={`${item.rate}-${idx}`}>
                <td className="px-2 py-1 font-mono">{item.rate}%</td>
                <td className="px-2 py-1 text-right tabular-nums">
                  {fmtCurrency(item.base, currency, locale)}
                </td>
                <td className="px-2 py-1 text-right font-medium tabular-nums">
                  {fmtCurrency(item.amount, currency, locale)}
                </td>
              </tr>
            ))}
          </tbody>
          <tfoot className="bg-slate-50 dark:bg-slate-800/50">
            <tr>
              <td className="px-2 py-1 text-right text-[10px] font-semibold uppercase text-slate-500 dark:text-slate-400">
                {t('orders.detail.financial.total')}
              </td>
              <td className="px-2 py-1 text-right font-semibold tabular-nums">
                {fmtCurrency(totalBase, currency, locale)}
              </td>
              <td className="px-2 py-1 text-right font-semibold tabular-nums">
                {fmtCurrency(totalTax, currency, locale)}
              </td>
            </tr>
          </tfoot>
        </table>
      </div>
    </section>
  );
};

export const EInvoicePanel = ({ invoice, locale }: { invoice: Invoice; locale: string }) => {
  const { t } = useTranslation();
  if (!invoice.eInvoiceUuid && !invoice.eInvoiceStatus && !invoice.isPostedToLedger) return null;
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-2.5 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <FileBadge size={12} />
        {t('invoices.detail.eInvoice')}
      </header>
      <dl className="mt-1.5 grid grid-cols-2 gap-x-3 gap-y-1 text-[11px]">
        {invoice.eInvoiceUuid && (
          <div className="col-span-2 flex items-center justify-between gap-2">
            <dt className="text-slate-500 dark:text-slate-400">
              {t('invoices.detail.eInvoiceUuid')}
            </dt>
            <dd className="min-w-0 truncate font-mono text-slate-900 dark:text-slate-100">
              {invoice.eInvoiceUuid}
            </dd>
          </div>
        )}
        {invoice.eInvoiceStatus && (
          <div className="flex items-center justify-between gap-2">
            <dt className="text-slate-500 dark:text-slate-400">
              {t('invoices.detail.eInvoiceStatus')}
            </dt>
            <dd className="font-medium text-slate-900 dark:text-slate-100">
              {invoice.eInvoiceStatus}
            </dd>
          </div>
        )}
        <div className="flex items-center justify-between gap-2">
          <dt className="text-slate-500 dark:text-slate-400">
            {t('invoices.detail.postedToLedger')}
          </dt>
          <dd className="font-medium">
            {invoice.isPostedToLedger ? (
              <span className="inline-flex items-center gap-1 text-success-600 dark:text-success-400">
                <CheckCircle2 size={11} /> {t('common.active')}
              </span>
            ) : (
              <span className="text-slate-500">—</span>
            )}
          </dd>
        </div>
        {invoice.issuedAtUtc && (
          <div className="col-span-2 flex items-center justify-between gap-2">
            <dt className="text-slate-500 dark:text-slate-400">{t('invoices.detail.issuedAt')}</dt>
            <dd className="text-slate-700 dark:text-slate-200">
              {fmtDate(invoice.issuedAtUtc, locale)}
            </dd>
          </div>
        )}
      </dl>
    </section>
  );
};
