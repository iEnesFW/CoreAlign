import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Banknote } from 'lucide-react';
import type { Order } from '@/features/orders/model/order.types';
import { fmtCurrency } from './format';

export const FinancialBreakdown = ({ order, locale }: { order: Order; locale: string }) => {
  const { t } = useTranslation();
  const rows = useMemo(
    () =>
      [
        { label: t('orders.detail.financial.subtotal'), value: order.subtotal, bold: false },
        order.lineDiscountTotal > 0 && {
          label: t('orders.detail.financial.lineDiscount'),
          value: -order.lineDiscountTotal,
          bold: false,
          tone: 'discount' as const,
        },
        order.headerDiscountAmount > 0 && {
          label: t('orders.detail.financial.headerDiscount', {
            pct: order.headerDiscountPercent,
          }),
          value: -order.headerDiscountAmount,
          bold: false,
          tone: 'discount' as const,
        },
        order.taxableTotal !== order.subtotal && {
          label: t('orders.detail.financial.taxable'),
          value: order.taxableTotal,
          bold: false,
        },
        order.taxTotal > 0 && {
          label: t('orders.detail.financial.tax'),
          value: order.taxTotal,
          bold: false,
        },
        order.withholdingTotal > 0 && {
          label: t('orders.detail.financial.withholding'),
          value: -order.withholdingTotal,
          bold: false,
          tone: 'discount' as const,
        },
        order.shippingCost > 0 && {
          label: t('orders.detail.financial.shipping'),
          value: order.shippingCost,
          bold: false,
        },
        order.roundingAdjustment !== 0 && {
          label: t('orders.detail.financial.rounding'),
          value: order.roundingAdjustment,
          bold: false,
        },
        { label: t('orders.detail.financial.total'), value: order.total, bold: true },
      ].filter(Boolean) as {
        label: string;
        value: number;
        bold: boolean;
        tone?: 'discount';
      }[],
    [order, t],
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
              {fmtCurrency(row.value, order.currency, locale)}
            </dd>
          </div>
        ))}
      </dl>
    </section>
  );
};
