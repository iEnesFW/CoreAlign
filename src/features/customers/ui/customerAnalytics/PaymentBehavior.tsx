import { useTranslation } from 'react-i18next';
import { Award, Clock } from 'lucide-react';
import { Badge } from '@/shared/ui/Badge/Badge';
import type { CustomerAnalytics } from '@/features/customers/model/customer.types';
import { fmtNumber, fmtPercent } from './format';

export const PaymentBehaviorCard = ({
  analytics,
  locale,
}: {
  analytics: CustomerAnalytics;
  locale: string;
}) => {
  const { t } = useTranslation();
  const totalPaid = analytics.onTimePayments + analytics.latePayments;
  const onTimePct = totalPaid > 0 ? (analytics.onTimePayments / totalPaid) * 100 : 0;
  const tone =
    totalPaid === 0 ? 'slate' : onTimePct >= 90 ? 'emerald' : onTimePct >= 60 ? 'amber' : 'red';
  const tier =
    totalPaid === 0
      ? null
      : onTimePct >= 90
        ? t('customers.analytics.tier.excellent')
        : onTimePct >= 60
          ? t('customers.analytics.tier.fair')
          : t('customers.analytics.tier.poor');

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <Clock size={12} />
          {t('customers.analytics.paymentBehavior')}
        </span>
        {tier && (
          <Badge
            variant={
              tone === 'emerald'
                ? 'success'
                : tone === 'amber'
                  ? 'warning'
                  : tone === 'red'
                    ? 'error'
                    : 'neutral'
            }
            pill
          >
            <Award size={9} className="mr-1" />
            {tier}
          </Badge>
        )}
      </header>
      <div className="mt-2 grid grid-cols-1 gap-2 sm:grid-cols-3">
        <PaymentStat
          label={t('customers.analytics.onTimePayments')}
          value={`${analytics.onTimePayments} / ${totalPaid}`}
          sub={fmtPercent(onTimePct, locale)}
          tone="emerald"
        />
        <PaymentStat
          label={t('customers.analytics.latePayments')}
          value={String(analytics.latePayments)}
          sub={fmtPercent(totalPaid > 0 ? (analytics.latePayments / totalPaid) * 100 : 0, locale)}
          tone={analytics.latePayments > 0 ? 'red' : 'slate'}
        />
        <PaymentStat
          label={t('customers.analytics.avgDaysToPay')}
          value={
            analytics.avgDaysToPayment >= 0
              ? `${fmtNumber(analytics.avgDaysToPayment, locale, 1)} ${t('customers.analytics.daysShort')}`
              : `${fmtNumber(analytics.avgDaysToPayment, locale, 1)} ${t('customers.analytics.daysShort')}`
          }
          sub={
            analytics.avgDaysToPayment <= 0
              ? t('customers.analytics.aheadOfDue')
              : t('customers.analytics.pastDue')
          }
          tone={analytics.avgDaysToPayment <= 0 ? 'emerald' : 'amber'}
        />
      </div>
      <div className="mt-2 h-2 w-full overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
        <div
          className={`h-full rounded-full transition-all ${
            tone === 'emerald'
              ? 'bg-success-500'
              : tone === 'amber'
                ? 'bg-warning-500'
                : tone === 'red'
                  ? 'bg-danger-500'
                  : 'bg-slate-400'
          }`}
          style={{ width: `${onTimePct}%` }}
        />
      </div>
    </section>
  );
};

const paymentToneClasses: Record<'slate' | 'emerald' | 'amber' | 'red', string> = {
  slate: 'text-slate-900 dark:text-slate-100',
  emerald: 'text-success-600 dark:text-success-400',
  amber: 'text-warning-600 dark:text-warning-400',
  red: 'text-danger-600 dark:text-danger-400',
};

const PaymentStat = ({
  label,
  value,
  sub,
  tone,
}: {
  label: string;
  value: string;
  sub?: string;
  tone: keyof typeof paymentToneClasses;
}) => (
  <div className="rounded border border-slate-200 px-2 py-1.5 dark:border-slate-800">
    <div className="text-[9px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {label}
    </div>
    <div className={`mt-0.5 text-sm font-bold tabular-nums ${paymentToneClasses[tone]}`}>
      {value}
    </div>
    {sub && <div className="text-[9px] text-slate-500 dark:text-slate-400">{sub}</div>}
  </div>
);
