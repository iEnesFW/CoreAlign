import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  AlarmClock,
  ArrowRight,
  CheckCircle2,
  CreditCard,
  Package,
  ReceiptText,
} from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { Skeleton } from '@/shared/ui/Skeleton/Skeleton';
import { formatCurrency, formatDate, formatDateTime } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { SubscriptionStatusBadge } from '@/features/billing/ui/SubscriptionStatusBadge';
import {
  useActiveModulesQuery,
  useSubscriptionOrdersQuery,
} from '@/features/billing/hooks/useBilling';
import type { TenantModuleDto } from '@/features/billing/model/billing.types';

const MS_PER_DAY = 1000 * 60 * 60 * 24;
const EXPIRING_WINDOW_DAYS = 14;

const daysUntil = (endUtc: string | null | undefined): number | null => {
  if (!endUtc) return null;
  const end = new Date(endUtc).getTime();
  if (Number.isNaN(end)) return null;
  return Math.ceil((end - Date.now()) / MS_PER_DAY);
};

const sortByEnd = (a: TenantModuleDto, b: TenantModuleDto) => {
  const ae = a.endUtc ? new Date(a.endUtc).getTime() : Number.POSITIVE_INFINITY;
  const be = b.endUtc ? new Date(b.endUtc).getTime() : Number.POSITIVE_INFINITY;
  return ae - be;
};

export const BillingHomePage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();

  const activeQuery = useActiveModulesQuery();
  const ordersQuery = useSubscriptionOrdersQuery({ page: 1, pageSize: 5 });

  const modules = useMemo(() => activeQuery.data?.data ?? [], [activeQuery.data]);
  const orders = useMemo(() => ordersQuery.data?.data?.items ?? [], [ordersQuery.data]);

  const activeCount = modules.filter((m) => m.isCurrentlyActive).length;
  const expiringSoon = useMemo(
    () =>
      modules
        .filter((m) => {
          if (!m.isCurrentlyActive) return false;
          const days = daysUntil(m.endUtc);
          return days !== null && days >= 0 && days <= EXPIRING_WINDOW_DAYS;
        })
        .sort(sortByEnd),
    [modules],
  );

  return (
    <div className="space-y-4 p-4">
      <PageHeader
        icon={<CreditCard size={20} />}
        eyebrow={t('billing.eyebrow')}
        title={t('billing.home.title')}
        subtitle={t('billing.home.subtitle')}
        tone="indigo"
        actions={
          <Link
            to="/dashboard/billing/modules"
            className="inline-flex items-center gap-1.5 rounded-lg bg-primary-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-primary-700"
          >
            <Package size={13} />
            {t('billing.home.browse')}
          </Link>
        }
      />

      {activeQuery.isError && (
        <QueryError
          onRetry={() => activeQuery.refetch()}
          isRetrying={activeQuery.isFetching}
          title={t('billing.errors.activeTitle')}
        />
      )}

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <SummaryCard
          icon={<CheckCircle2 size={16} />}
          label={t('billing.home.activeCount')}
          tone="emerald"
          loading={activeQuery.isPending}
          value={String(activeCount)}
          hint={t('billing.home.activeHint', { count: modules.length })}
        />
        <SummaryCard
          icon={<AlarmClock size={16} />}
          label={t('billing.home.expiringSoon', { days: EXPIRING_WINDOW_DAYS })}
          tone="amber"
          loading={activeQuery.isPending}
          value={String(expiringSoon.length)}
          hint={
            expiringSoon[0]?.endUtc
              ? t('billing.home.nextExpiry', {
                  date: formatDate(expiringSoon[0].endUtc, locale),
                })
              : t('billing.home.noExpiring')
          }
        />
        <SummaryCard
          icon={<ReceiptText size={16} />}
          label={t('billing.home.recentOrders')}
          tone="indigo"
          loading={ordersQuery.isPending}
          value={String(orders.length)}
          hint={
            orders[0]
              ? t('billing.home.lastOrder', {
                  number: orders[0].orderNumber,
                })
              : t('billing.home.noOrders')
          }
        />
      </div>

      <section className="grid grid-cols-1 gap-3 lg:grid-cols-2">
        <div className="rounded-xl border border-slate-200/70 bg-white p-4 shadow-sm dark:border-slate-800/70 dark:bg-slate-900">
          <div className="mb-2 flex items-center justify-between">
            <h3 className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              {t('billing.home.expiringSoonTitle')}
            </h3>
            <Link
              to="/dashboard/billing/modules"
              className="text-[11px] font-medium text-primary-600 hover:underline dark:text-primary-300"
            >
              {t('billing.home.renew')} <ArrowRight size={10} className="inline" />
            </Link>
          </div>
          {activeQuery.isPending ? (
            <Skeleton className="h-16 w-full" />
          ) : expiringSoon.length === 0 ? (
            <p className="text-xs text-slate-500 dark:text-slate-400">
              {t('billing.home.noExpiring')}
            </p>
          ) : (
            <ul className="space-y-1.5">
              {expiringSoon.map((m) => {
                const days = daysUntil(m.endUtc);
                return (
                  <li
                    key={m.id}
                    className="flex items-center justify-between gap-2 rounded-md border border-warning-100 bg-warning-50/60 px-2 py-1.5 text-xs dark:border-warning-500/30 dark:bg-warning-500/10"
                  >
                    <span className="font-medium text-warning-900 dark:text-warning-200">
                      {m.name}
                    </span>
                    <span className="text-[11px] text-warning-700 dark:text-warning-200/80">
                      {days !== null
                        ? t('billing.modules.daysLeft', { count: days })
                        : t('billing.modules.activeUntil', {
                            date: m.endUtc ? formatDate(m.endUtc, locale) : '—',
                          })}
                    </span>
                  </li>
                );
              })}
            </ul>
          )}
        </div>

        <div className="rounded-xl border border-slate-200/70 bg-white p-4 shadow-sm dark:border-slate-800/70 dark:bg-slate-900">
          <div className="mb-2 flex items-center justify-between">
            <h3 className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              {t('billing.home.recentOrdersTitle')}
            </h3>
            <Link
              to="/dashboard/billing/orders"
              className="text-[11px] font-medium text-primary-600 hover:underline dark:text-primary-300"
            >
              {t('billing.home.viewAll')} <ArrowRight size={10} className="inline" />
            </Link>
          </div>
          {ordersQuery.isPending ? (
            <Skeleton className="h-16 w-full" />
          ) : orders.length === 0 ? (
            <p className="text-xs text-slate-500 dark:text-slate-400">
              {t('billing.home.noOrders')}
            </p>
          ) : (
            <ul className="space-y-1.5">
              {orders.map((o) => (
                <li key={o.id}>
                  <Link
                    to={`/dashboard/billing/orders/${o.id}`}
                    className="flex items-center justify-between gap-2 rounded-md border border-slate-100 bg-slate-50/40 px-2 py-1.5 text-xs hover:border-primary-200 hover:bg-primary-50/40 dark:border-slate-800 dark:bg-slate-800/40 dark:hover:border-primary-500/30"
                  >
                    <div className="min-w-0">
                      <div className="flex items-center gap-1.5">
                        <span className="truncate font-medium text-slate-800 dark:text-slate-200">
                          {o.orderNumber}
                        </span>
                        <SubscriptionStatusBadge status={o.status} />
                      </div>
                      <p className="text-[10px] text-slate-500 dark:text-slate-400">
                        {formatDateTime(o.createdAtUtc, locale)}
                      </p>
                    </div>
                    <span className="shrink-0 text-xs font-semibold tabular-nums text-slate-900 dark:text-slate-100">
                      {formatCurrency(o.totalAmount, locale, o.currency)}
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>
    </div>
  );
};

interface SummaryCardProps {
  icon: React.ReactNode;
  label: string;
  value: string;
  hint?: string;
  tone: 'emerald' | 'amber' | 'indigo';
  loading?: boolean;
}

const TONE_BG: Record<SummaryCardProps['tone'], string> = {
  emerald: 'from-success-500 to-teal-600',
  amber: 'from-warning-500 to-warning-600',
  indigo: 'from-primary-500 to-purple-600',
};

const SummaryCard = ({ icon, label, value, hint, tone, loading }: SummaryCardProps) => (
  <div className="rounded-xl border border-slate-200/70 bg-white p-3 shadow-sm dark:border-slate-800/70 dark:bg-slate-900">
    <div className="flex items-start gap-3">
      <div
        className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-gradient-to-br text-white shadow-md ${TONE_BG[tone]}`}
      >
        {icon}
      </div>
      <div className="min-w-0">
        <p className="text-[10px] font-semibold uppercase tracking-wider text-slate-400">{label}</p>
        {loading ? (
          <Skeleton className="mt-1 h-6 w-12" />
        ) : (
          <p className="text-xl font-bold tabular-nums text-slate-900 dark:text-slate-100">
            {value}
          </p>
        )}
        {hint && <p className="mt-0.5 text-[11px] text-slate-500 dark:text-slate-400">{hint}</p>}
      </div>
    </div>
  </div>
);

export default BillingHomePage;
