import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { BarChart3 } from 'lucide-react';
import type { MonthlyRevenuePoint } from '@/features/customers/model/customer.types';
import { fmtCurrency } from './format';

export const MonthlyRevenueChart = ({
  points,
  currency,
  locale,
}: {
  points: MonthlyRevenuePoint[];
  currency: string;
  locale: string;
}) => {
  const { t } = useTranslation();
  const max = useMemo(() => Math.max(1, ...points.map((p) => p.revenue)), [points]);
  const total = useMemo(() => points.reduce((acc, p) => acc + p.revenue, 0), [points]);
  const avg = points.length > 0 ? total / points.length : 0;

  if (points.length === 0) return null;

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <BarChart3 size={12} />
          {t('customers.analytics.monthlyRevenue', { count: points.length })}
        </span>
        <span className="text-[10px] normal-case">
          <span className="text-slate-500 dark:text-slate-400">
            {t('customers.analytics.avgMonth')}:{' '}
          </span>
          <span className="font-semibold text-slate-900 dark:text-slate-100">
            {fmtCurrency(avg, currency, locale)}
          </span>
        </span>
      </header>
      <div className="mt-3 flex h-24 items-end gap-px">
        {points.map((p) => {
          const h = (p.revenue / max) * 100;
          return (
            <div key={`${p.year}-${p.month}`} className="group flex flex-1 flex-col items-stretch">
              <div className="relative flex-1 flex items-end">
                <div
                  className="w-full rounded-t-sm bg-primary-500 transition-all group-hover:bg-primary-600"
                  style={{ height: `${Math.max(2, h)}%` }}
                  title={`${p.label}: ${fmtCurrency(p.revenue, currency, locale)} (${p.invoiceCount})`}
                />
              </div>
            </div>
          );
        })}
      </div>
      <div className="mt-1 flex items-center justify-between text-[9px] text-slate-500 dark:text-slate-400">
        <span>{points[0]?.label}</span>
        <span>{points[points.length - 1]?.label}</span>
      </div>
      <div className="mt-2 grid grid-cols-3 gap-1.5 text-[10px]">
        <div className="rounded border border-slate-200 px-2 py-1 text-center dark:border-slate-800">
          <div className="text-slate-500 dark:text-slate-400">
            {t('customers.analytics.peakMonth')}
          </div>
          <div className="font-semibold text-slate-900 dark:text-slate-100">
            {points.reduce((a, b) => (a.revenue >= b.revenue ? a : b)).label}
          </div>
        </div>
        <div className="rounded border border-slate-200 px-2 py-1 text-center dark:border-slate-800">
          <div className="text-slate-500 dark:text-slate-400">
            {t('customers.analytics.totalPeriod')}
          </div>
          <div className="font-semibold tabular-nums text-slate-900 dark:text-slate-100">
            {fmtCurrency(total, currency, locale)}
          </div>
        </div>
        <div className="rounded border border-slate-200 px-2 py-1 text-center dark:border-slate-800">
          <div className="text-slate-500 dark:text-slate-400">
            {t('customers.analytics.invoicesPeriod')}
          </div>
          <div className="font-semibold tabular-nums text-slate-900 dark:text-slate-100">
            {points.reduce((acc, p) => acc + p.invoiceCount, 0)}
          </div>
        </div>
      </div>
    </section>
  );
};
