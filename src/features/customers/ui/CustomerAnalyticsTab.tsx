import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Award,
  BarChart3,
  Calendar,
  Clock,
  Coins,
  DollarSign,
  FileText,
  ShoppingCart,
  Trophy,
} from 'lucide-react';
import { Badge } from '@/shared/ui/Badge/Badge';
import { useCustomerAnalyticsQuery } from '@/features/customers/hooks/useCustomerQueries';
import type {
  CustomerAnalytics,
  MonthlyRevenuePoint,
  StatusBreakdown,
  TopProduct,
} from '@/features/customers/model/customer.types';

interface Props {
  customerId: string;
  locale: string;
  monthsBack?: number;
  onOpenProduct?: (productId: string) => void;
}

const fmtCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

const fmtNumber = (value: number, locale: string, decimals = 0) =>
  new Intl.NumberFormat(locale, {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(value);

const fmtPercent = (value: number, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { maximumFractionDigits: 1 }).format(value) + '%';
  } catch {
    return `${value.toFixed(1)}%`;
  }
};

export const CustomerAnalyticsTab = ({
  customerId,
  locale,
  monthsBack = 12,
  onOpenProduct,
}: Props) => {
  const { t } = useTranslation();
  const query = useCustomerAnalyticsQuery(customerId, monthsBack);
  const analytics = query.data?.data ?? null;

  if (query.isPending && !analytics) {
    return <div className="text-sm italic text-slate-500">{t('common.loading')}</div>;
  }
  if (!analytics) {
    return (
      <div className="rounded border border-slate-200 p-4 text-center text-sm text-slate-500 dark:border-slate-800">
        {t('customers.analytics.noData')}
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <KpiStrip analytics={analytics} locale={locale} />
      <PaymentBehaviorCard analytics={analytics} locale={locale} />
      <MonthlyRevenueChart
        points={analytics.monthlyRevenue}
        currency={analytics.currency}
        locale={locale}
      />
      <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
        <StatusBreakdownCard
          title={t('customers.analytics.orderStatusBreakdown')}
          icon={<ShoppingCart size={12} />}
          items={analytics.orderStatusBreakdown}
          currency={analytics.currency}
          locale={locale}
          statusPrefix="orders.status"
        />
        <StatusBreakdownCard
          title={t('customers.analytics.invoiceStatusBreakdown')}
          icon={<FileText size={12} />}
          items={analytics.invoiceStatusBreakdown}
          currency={analytics.currency}
          locale={locale}
          statusPrefix="invoices.status"
        />
      </div>
      <TopProductsCard
        products={analytics.topProducts}
        currency={analytics.currency}
        locale={locale}
        onOpenProduct={onOpenProduct}
      />
    </div>
  );
};

const KpiStrip = ({ analytics, locale }: { analytics: CustomerAnalytics; locale: string }) => {
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
  indigo: 'border-indigo-200 dark:border-indigo-500/30',
  blue: 'border-blue-200 dark:border-blue-500/30',
  emerald: 'border-emerald-200 dark:border-emerald-500/30',
  amber: 'border-amber-200 dark:border-amber-500/30',
  red: 'border-red-200 dark:border-red-500/30',
};

const Kpi = ({
  icon,
  label,
  value,
  sub,
  tone,
}: {
  icon: React.ReactNode;
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

const PaymentBehaviorCard = ({
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
              ? 'bg-emerald-500'
              : tone === 'amber'
                ? 'bg-amber-500'
                : tone === 'red'
                  ? 'bg-red-500'
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
  emerald: 'text-emerald-600 dark:text-emerald-400',
  amber: 'text-amber-600 dark:text-amber-400',
  red: 'text-red-600 dark:text-red-400',
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

const MonthlyRevenueChart = ({
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
                  className="w-full rounded-t-sm bg-indigo-500 transition-all group-hover:bg-indigo-600"
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

const TopProductsCard = ({
  products,
  currency,
  locale,
  onOpenProduct,
}: {
  products: TopProduct[];
  currency: string;
  locale: string;
  onOpenProduct?: (productId: string) => void;
}) => {
  const { t } = useTranslation();
  if (products.length === 0) return null;
  const maxRev = Math.max(1, ...products.map((p) => p.revenue));
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <Trophy size={12} />
          {t('customers.analytics.topProducts')}
        </span>
        <span className="text-slate-400">{products.length}</span>
      </header>
      <ol className="mt-2 space-y-1.5">
        {products.map((p, idx) => {
          const pct = (p.revenue / maxRev) * 100;
          const clickable = !!onOpenProduct && !!p.productId;
          return (
            <li key={`${p.productSku}-${idx}`}>
              <button
                type="button"
                onClick={clickable ? () => onOpenProduct?.(p.productId as string) : undefined}
                disabled={!clickable}
                className={`flex w-full items-center gap-2 rounded border border-slate-200 p-1.5 text-left text-[11px] transition dark:border-slate-800 ${clickable ? 'hover:bg-slate-50 dark:hover:bg-slate-800/50' : 'cursor-default'}`}
              >
                <span className="inline-flex h-5 w-5 shrink-0 items-center justify-center rounded bg-indigo-100 text-[10px] font-bold text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-300">
                  {idx + 1}
                </span>
                <div className="min-w-0 flex-1">
                  <div className="flex items-center justify-between gap-2">
                    <div className="min-w-0 truncate font-medium text-slate-900 dark:text-slate-100">
                      {p.productName}
                    </div>
                    <div className="shrink-0 font-mono tabular-nums text-slate-900 dark:text-slate-100">
                      {fmtCurrency(p.revenue, currency, locale)}
                    </div>
                  </div>
                  <div className="mt-0.5 flex items-center justify-between gap-2 text-[9px] text-slate-500 dark:text-slate-400">
                    <span className="font-mono">{p.productSku}</span>
                    <span>
                      {fmtNumber(p.quantity, locale)} {t('inventory.fields.onHand').toLowerCase()} ·{' '}
                      {p.invoiceCount} {t('customers.detail.metrics.invoiceCount')}
                    </span>
                  </div>
                  <div className="mt-1 h-0.5 w-full overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
                    <div
                      className="h-full rounded-full bg-indigo-500"
                      style={{ width: `${pct}%` }}
                    />
                  </div>
                </div>
              </button>
            </li>
          );
        })}
      </ol>
    </section>
  );
};

const statusToneByKind: Record<string, string> = {
  Draft: 'bg-slate-200 dark:bg-slate-700',
  Submitted: 'bg-sky-400',
  Approved: 'bg-indigo-500',
  Confirmed: 'bg-blue-500',
  Allocated: 'bg-violet-500',
  Picking: 'bg-fuchsia-500',
  Packed: 'bg-purple-500',
  PartiallyShipped: 'bg-amber-400',
  Shipped: 'bg-amber-500',
  Delivered: 'bg-teal-500',
  Closed: 'bg-emerald-500',
  Cancelled: 'bg-red-500',
  Returned: 'bg-rose-500',
  Issued: 'bg-blue-500',
  Sent: 'bg-sky-500',
  PartiallyPaid: 'bg-amber-500',
  Paid: 'bg-emerald-500',
  Overdue: 'bg-red-500',
  Void: 'bg-rose-500',
};

const StatusBreakdownCard = ({
  title,
  icon,
  items,
  currency,
  locale,
  statusPrefix,
}: {
  title: string;
  icon: React.ReactNode;
  items: StatusBreakdown[];
  currency: string;
  locale: string;
  statusPrefix: string;
}) => {
  const { t } = useTranslation();
  if (items.length === 0) return null;
  const totalCount = items.reduce((s, i) => s + i.count, 0);
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          {icon}
          {title}
        </span>
        <span className="text-slate-400">{totalCount}</span>
      </header>
      <div className="mt-2 flex h-1.5 overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
        {items.map((item) => {
          const pct = (item.count / Math.max(1, totalCount)) * 100;
          if (pct <= 0) return null;
          return (
            <div
              key={item.status}
              className={statusToneByKind[item.status] ?? 'bg-slate-400'}
              style={{ width: `${pct}%` }}
              title={`${item.status}: ${item.count}`}
            />
          );
        })}
      </div>
      <ul className="mt-2 space-y-1 text-[11px]">
        {items.map((item) => (
          <li key={item.status} className="flex items-center justify-between gap-2">
            <div className="flex min-w-0 items-center gap-1.5">
              <span
                className={`inline-block h-2 w-2 shrink-0 rounded-full ${statusToneByKind[item.status] ?? 'bg-slate-400'}`}
              />
              <span className="truncate text-slate-700 dark:text-slate-200">
                {t(`${statusPrefix}.${item.status}` as never, { defaultValue: item.status })}
              </span>
            </div>
            <div className="shrink-0 text-right text-slate-500 dark:text-slate-400">
              <span className="tabular-nums">{item.count}</span>
              <span className="ml-1 font-mono text-[10px]">
                {fmtCurrency(item.total, currency, locale)}
              </span>
            </div>
          </li>
        ))}
      </ul>
    </section>
  );
};
