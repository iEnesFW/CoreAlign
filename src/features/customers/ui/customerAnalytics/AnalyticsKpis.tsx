import { type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Calendar, Coins, DollarSign, ShoppingCart } from 'lucide-react';
import type { CustomerAnalytics } from '@/features/customers/model/customer.types';
import { fmtCurrency, fmtNumber } from './format';

export const KpiStrip = ({
  analytics,
  locale,
}: {
  analytics: CustomerAnalytics;
  locale: string;
}) => {
  const { t } = useTranslation();
  return (
    <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
      <Kpi
        icon={<DollarSign size={11} />}
        label={t('customers.analytics.lifetimeValue')}
        value={fmtCurrency(analytics.lifetimeValue, analytics.currency, locale)}
        sub={`${analytics.invoiceCount} ${t('customers.detail.metrics.invoiceCount')}`}
        tone="indigo"
      />
      <Kpi
        icon={<Coins size={11} />}
        label={t('customers.analytics.totalPaid')}
        value={fmtCurrency(analytics.totalPaid, analytics.currency, locale)}
        sub={`${analytics.paymentCount} ${t('customers.analytics.paymentsLabel')}`}
        tone="emerald"
      />
      <Kpi
        icon={<ShoppingCart size={11} />}
        label={t('customers.analytics.avgOrderValue')}
        value={fmtCurrency(analytics.avgOrderValue, analytics.currency, locale)}
        sub={`${analytics.orderCount} ${t('customers.detail.metrics.orders').toLowerCase()}`}
        tone="blue"
      />
      <Kpi
        icon={<Calendar size={11} />}
        label={t('customers.analytics.relationshipMonths')}
        value={analytics.lifetimeMonths > 0 ? fmtNumber(analytics.lifetimeMonths, locale) : '—'}
        sub={
          analytics.firstOrderAtUtc
            ? new Intl.DateTimeFormat(locale, { year: 'numeric', month: 'short' }).format(
                new Date(analytics.firstOrderAtUtc),
              )
            : undefined
        }
        tone="slate"
      />
    </div>
  );
};

const kpiTones: Record<'slate' | 'indigo' | 'blue' | 'emerald' | 'amber' | 'red', string> = {
  slate: 'border-slate-200 dark:border-slate-800',
  indigo: 'border-primary-200 dark:border-primary-500/30',
  blue: 'border-primary-200 dark:border-primary-500/30',
  emerald: 'border-success-200 dark:border-success-500/30',
  amber: 'border-warning-200 dark:border-warning-500/30',
  red: 'border-danger-200 dark:border-danger-500/30',
};

const Kpi = ({
  icon,
  label,
  value,
  sub,
  tone,
}: {
  icon: ReactNode;
  label: string;
  value: string;
  sub?: string;
  tone: keyof typeof kpiTones;
}) => (
  <div className={`rounded-lg border bg-white p-2 dark:bg-slate-900 ${kpiTones[tone]}`}>
    <div className="flex items-center gap-1 text-[9px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {icon}
      <span>{label}</span>
    </div>
    <div className="mt-0.5 text-sm font-bold tabular-nums text-slate-900 dark:text-slate-100">
      {value}
    </div>
    {sub && <div className="text-[9px] text-slate-500 dark:text-slate-400">{sub}</div>}
  </div>
);
