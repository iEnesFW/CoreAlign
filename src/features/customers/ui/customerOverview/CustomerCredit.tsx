import { useTranslation } from 'react-i18next';
import { CreditCard, Wallet } from 'lucide-react';
import { Badge } from '@/shared/ui/Badge/Badge';
import type { CustomerAging } from '@/features/payments/model/payment.types';
import { fmtCurrency, fmtPercent } from './format';

export const AgingMiniCard = ({ aging, locale }: { aging: CustomerAging; locale: string }) => {
  const { t } = useTranslation();
  const segments: { label: string; amount: number; color: string }[] = [
    {
      label: t('payments.aging.current', { defaultValue: 'Current' }),
      amount: aging.current,
      color: 'bg-success-500',
    },
    { label: '1-30', amount: aging.days1To30, color: 'bg-warning-500' },
    { label: '31-60', amount: aging.days31To60, color: 'bg-warning-500' },
    { label: '61-90', amount: aging.days61To90, color: 'bg-warning-500' },
    { label: '90+', amount: aging.daysOver90, color: 'bg-danger-500' },
  ];
  const total = aging.totalOutstanding || 1;
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <Wallet size={12} />
          {t('payments.aging.title', { defaultValue: 'Aging analysis' })}
        </span>
        <span className="font-bold tabular-nums text-slate-900 dark:text-slate-100">
          {fmtCurrency(aging.totalOutstanding, aging.currency, locale)}
        </span>
      </header>
      <div className="mt-2 flex h-1.5 overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
        {segments.map((seg) => {
          const pct = (seg.amount / total) * 100;
          if (pct <= 0) return null;
          return (
            <div
              key={seg.label}
              className={seg.color}
              style={{ width: `${pct}%` }}
              title={`${seg.label}: ${fmtCurrency(seg.amount, aging.currency, locale)}`}
            />
          );
        })}
      </div>
      <div className="mt-1.5 grid grid-cols-5 gap-1 text-[9px]">
        {segments.map((seg) => (
          <div
            key={`legend-${seg.label}`}
            className="rounded border border-slate-200 px-1 py-0.5 text-center dark:border-slate-800"
          >
            <div className="flex items-center justify-center gap-0.5">
              <span className={`h-1 w-1 rounded-full ${seg.color}`} />
              <span className="font-semibold text-slate-600 dark:text-slate-300">{seg.label}</span>
            </div>
            <div className="text-[10px] tabular-nums text-slate-700 dark:text-slate-200">
              {fmtCurrency(seg.amount, aging.currency, locale)}
            </div>
          </div>
        ))}
      </div>
    </section>
  );
};

export const CreditGaugeCard = ({
  currentBalance,
  creditLimit,
  outstanding,
  overdue,
  creditUsedPercent,
  isOverCreditLimit,
  currency,
  locale,
  loading,
}: {
  currentBalance: number;
  creditLimit: number;
  outstanding: number;
  overdue: number;
  creditUsedPercent: number;
  isOverCreditLimit: boolean;
  currency: string;
  locale: string;
  loading: boolean;
}) => {
  const { t } = useTranslation();
  const noLimit = creditLimit <= 0;
  const pct = noLimit ? 0 : Math.min(Math.max(creditUsedPercent, 0), 120);
  const barColor = isOverCreditLimit
    ? 'bg-danger-500'
    : pct >= 85
      ? 'bg-warning-500'
      : pct >= 60
        ? 'bg-warning-500'
        : 'bg-success-500';
  const available = Math.max(0, creditLimit - currentBalance);

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between">
        <div className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
          <CreditCard size={12} />
          {t('customers.detail.metrics.creditLine')}
        </div>
        {isOverCreditLimit && (
          <Badge variant="error" pill>
            {t('customers.detail.metrics.overLimit')}
          </Badge>
        )}
      </header>

      <div className="mt-2 grid grid-cols-3 gap-2">
        <GaugeStat
          label={t('customers.detail.metrics.balance')}
          value={fmtCurrency(currentBalance, currency, locale)}
          tone={currentBalance > 0 ? 'amber' : currentBalance < 0 ? 'emerald' : 'slate'}
        />
        <GaugeStat
          label={t('customers.detail.metrics.creditLimit')}
          value={noLimit ? '—' : fmtCurrency(creditLimit, currency, locale)}
          tone="slate"
        />
        <GaugeStat
          label={t('customers.detail.metrics.available')}
          value={noLimit ? '—' : fmtCurrency(available, currency, locale)}
          tone={noLimit ? 'slate' : available > 0 ? 'emerald' : 'red'}
        />
      </div>

      <div className="mt-3">
        <div className="flex items-center justify-between text-[10px] text-slate-500 dark:text-slate-400">
          <span>{t('customers.detail.metrics.creditUsed')}</span>
          <span className="tabular-nums">
            {noLimit ? '—' : fmtPercent(Math.min(creditUsedPercent, 999.9), locale)}
          </span>
        </div>
        <div className="mt-1 h-1.5 w-full overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
          <div
            className={`h-full rounded-full transition-all ${barColor}`}
            style={{ width: `${Math.min(pct, 100)}%` }}
          />
        </div>
      </div>

      <div className="mt-2 grid grid-cols-2 gap-2 text-[11px]">
        <div className="flex items-center justify-between rounded border border-slate-200 px-2 py-1 dark:border-slate-800">
          <span className="text-slate-500 dark:text-slate-400">
            {t('customers.detail.metrics.outstanding')}
          </span>
          <span className="font-semibold tabular-nums text-slate-900 dark:text-slate-100">
            {fmtCurrency(outstanding, currency, locale)}
          </span>
        </div>
        <div className="flex items-center justify-between rounded border border-slate-200 px-2 py-1 dark:border-slate-800">
          <span className="text-slate-500 dark:text-slate-400">
            {t('customers.detail.metrics.overdue')}
          </span>
          <span
            className={`font-semibold tabular-nums ${
              overdue > 0
                ? 'text-danger-600 dark:text-danger-400'
                : 'text-slate-900 dark:text-slate-100'
            }`}
          >
            {fmtCurrency(overdue, currency, locale)}
          </span>
        </div>
      </div>

      {loading && (
        <div className="mt-2 text-[10px] italic text-slate-400 dark:text-slate-500">
          {t('common.loading')}
        </div>
      )}
    </section>
  );
};

const toneClasses: Record<'slate' | 'amber' | 'emerald' | 'red', string> = {
  slate: 'text-slate-900 dark:text-slate-100',
  amber: 'text-warning-600 dark:text-warning-400',
  emerald: 'text-success-600 dark:text-success-400',
  red: 'text-danger-600 dark:text-danger-400',
};

const GaugeStat = ({
  label,
  value,
  tone,
}: {
  label: string;
  value: string;
  tone: keyof typeof toneClasses;
}) => (
  <div className="rounded border border-slate-200 px-2 py-1.5 dark:border-slate-800">
    <div className="text-[9px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {label}
    </div>
    <div className={`mt-0.5 text-sm font-bold tabular-nums ${toneClasses[tone]}`}>{value}</div>
  </div>
);
