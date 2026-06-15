import {
  CheckSquare,
  ClipboardList,
  FileText,
  Package,
  ReceiptText,
  Store,
  Wallet,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { Card, CardBody, CardHeader } from '@/shared/ui/Card';
import { PageHeader } from '@/shared/ui/PageHeader';
import { Spinner } from '@/shared/ui/Spinner';
import { InvoiceStatusBadge, OrderStatusBadge } from '@/shared/ui/StatusBadge';
import { cn } from '@/shared/lib/cn';
import { formatCurrency, formatDate, formatNumber } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useDashboard } from '@/features/portal/hooks';
import { useApprovalsPendingCount } from '@/features/approvals/hooks';

export const DashboardPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const { data, isLoading } = useDashboard();
  const pendingApprovals = useApprovalsPendingCount();

  if (isLoading || !data) {
    return (
      <div className="flex items-center gap-2 text-sm text-slate-500">
        <Spinner /> {t('common.loading')}
      </div>
    );
  }

  const recentFive = data.recentOrders.slice(0, 5);
  const invoicedLast30Currency =
    data.invoicedLast30DaysCurrency || data.openInvoiceCurrency || 'TRY';
  const pendingCount = pendingApprovals.data ?? 0;

  return (
    <div className="space-y-8">
      <PageHeader title={t('dashboard.title')} subtitle={t('dashboard.subtitle')} />

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard
          label={t('dashboard.openOrders')}
          value={formatNumber(data.totalActiveOrders, locale, 0)}
          icon={<Package size={18} />}
          tone="from-sky-500 to-indigo-500"
        />
        <StatCard
          label={t('dashboard.openInvoices')}
          value={formatNumber(data.totalOpenInvoices, locale, 0)}
          icon={<FileText size={18} />}
          tone="from-amber-500 to-orange-500"
        />
        <StatCard
          label={t('dashboard.openInvoiceTotal')}
          value={formatCurrency(
            data.openInvoiceTotalAmount,
            locale,
            data.openInvoiceCurrency || 'TRY',
          )}
          icon={<Wallet size={18} />}
          tone="from-emerald-500 to-teal-500"
        />
        <StatCard
          label={t('dashboard.activeDealers')}
          value={formatNumber(data.totalActiveDealers, locale, 0)}
          icon={<Store size={18} />}
          tone="from-violet-500 to-fuchsia-500"
        />
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
        <Card className="overflow-hidden">
          <div
            className="flex items-start justify-between gap-3 p-5"
            role="group"
            aria-label={t('dashboard.widgets.pendingApprovals')}
          >
            <div>
              <p className="text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('dashboard.widgets.pendingApprovals')}
              </p>
              <p
                className="mt-2 text-3xl font-bold text-slate-900 dark:text-slate-100"
                data-testid="pending-approvals-value"
              >
                {formatNumber(pendingCount, locale, 0)}
              </p>
              <Link
                to="/approvals"
                className="mt-3 inline-flex items-center gap-1 text-xs font-medium text-amber-600 hover:underline dark:text-amber-400"
              >
                {t('dashboard.widgets.pendingApprovalsCta')}
              </Link>
            </div>
            <span className="inline-flex h-11 w-11 items-center justify-center rounded-xl bg-gradient-to-br from-amber-500 to-orange-500 text-white shadow-md">
              <CheckSquare size={18} />
            </span>
          </div>
        </Card>

        <Card className="overflow-hidden">
          <div className="flex h-full flex-col p-5">
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">
                  {t('dashboard.widgets.recentFive')}
                </p>
                <p className="mt-2 text-3xl font-bold text-slate-900 dark:text-slate-100">
                  {formatNumber(recentFive.length, locale, 0)}
                </p>
              </div>
              <span className="inline-flex h-11 w-11 items-center justify-center rounded-xl bg-gradient-to-br from-sky-500 to-blue-500 text-white shadow-md">
                <ClipboardList size={18} />
              </span>
            </div>
            {recentFive.length > 0 ? (
              <ul className="mt-3 divide-y divide-slate-100 text-xs dark:divide-slate-800">
                {recentFive.map((o) => (
                  <li key={o.id} className="py-1.5">
                    <Link
                      to={`/orders/${o.id}`}
                      className="flex items-center justify-between gap-2 hover:underline"
                    >
                      <span className="truncate font-medium text-slate-700 dark:text-slate-200">
                        {o.orderNumber}
                      </span>
                      <span className="text-slate-500">{formatDate(o.orderDate, locale)}</span>
                    </Link>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="mt-3 text-xs text-slate-400">{t('dashboard.empty')}</p>
            )}
          </div>
        </Card>

        <Card className="overflow-hidden">
          <div className="flex items-start justify-between gap-3 p-5">
            <div>
              <p className="text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('dashboard.widgets.invoicedLast30')}
              </p>
              <p className="mt-2 text-3xl font-bold text-slate-900 dark:text-slate-100">
                {formatCurrency(data.invoicedLast30DaysAmount, locale, invoicedLast30Currency)}
              </p>
              <p className="mt-3 text-xs text-slate-500 dark:text-slate-400">
                {t('dashboard.widgets.invoicedLast30Subtitle')}
              </p>
            </div>
            <span className="inline-flex h-11 w-11 items-center justify-center rounded-xl bg-gradient-to-br from-emerald-500 to-teal-500 text-white shadow-md">
              <ReceiptText size={18} />
            </span>
          </div>
        </Card>
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
        <Card>
          <CardHeader
            title={t('dashboard.recentOrders')}
            action={
              <Link
                to="/orders"
                className="text-xs font-medium text-sky-600 hover:underline dark:text-sky-400"
              >
                {t('common.viewAll')}
              </Link>
            }
          />
          <CardBody className="px-0 py-0">
            {data.recentOrders.length === 0 ? (
              <p className="px-6 py-6 text-sm text-slate-400">{t('dashboard.empty')}</p>
            ) : (
              <ul className="divide-y divide-slate-100 dark:divide-slate-800">
                {data.recentOrders.map((o) => (
                  <li key={o.id}>
                    <Link
                      to={`/orders/${o.id}`}
                      className="flex flex-wrap items-center justify-between gap-2 px-6 py-3 transition hover:bg-slate-50 dark:hover:bg-slate-900"
                    >
                      <div>
                        <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
                          {o.orderNumber}
                        </p>
                        <p className="text-xs text-slate-500">{formatDate(o.orderDate, locale)}</p>
                      </div>
                      <div className="flex items-center gap-3">
                        <OrderStatusBadge status={o.status} />
                        <span className="text-sm font-semibold text-slate-700 dark:text-slate-200">
                          {formatCurrency(o.total, locale, o.currency)}
                        </span>
                      </div>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>

        <Card>
          <CardHeader
            title={t('dashboard.recentInvoices')}
            action={
              <Link
                to="/invoices"
                className="text-xs font-medium text-sky-600 hover:underline dark:text-sky-400"
              >
                {t('common.viewAll')}
              </Link>
            }
          />
          <CardBody className="px-0 py-0">
            {data.recentInvoices.length === 0 ? (
              <p className="px-6 py-6 text-sm text-slate-400">{t('dashboard.empty')}</p>
            ) : (
              <ul className="divide-y divide-slate-100 dark:divide-slate-800">
                {data.recentInvoices.map((i) => (
                  <li key={i.id}>
                    <Link
                      to={`/invoices/${i.id}`}
                      className="flex flex-wrap items-center justify-between gap-2 px-6 py-3 transition hover:bg-slate-50 dark:hover:bg-slate-900"
                    >
                      <div>
                        <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
                          {i.invoiceNumber}
                        </p>
                        <p className="text-xs text-slate-500">{formatDate(i.issueDate, locale)}</p>
                      </div>
                      <div className="flex items-center gap-3">
                        <InvoiceStatusBadge status={i.status} isOverdue={i.isOverdue} />
                        <span className="text-sm font-semibold text-slate-700 dark:text-slate-200">
                          {formatCurrency(i.total, locale, i.currency)}
                        </span>
                      </div>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>
      </div>
    </div>
  );
};

const StatCard = ({
  label,
  value,
  icon,
  tone,
}: {
  label: string;
  value: string;
  icon: React.ReactNode;
  tone: string;
}) => (
  <Card className="overflow-hidden">
    <div className="flex items-center gap-4 p-5" role="group" aria-label={label}>
      <span
        className={cn(
          'inline-flex h-11 w-11 items-center justify-center rounded-xl bg-gradient-to-br text-white shadow-md',
          tone,
        )}
        aria-hidden="true"
      >
        {icon}
      </span>
      <div>
        <p className="text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">
          {label}
        </p>
        <p
          className="mt-1 text-xl font-semibold text-slate-900 dark:text-slate-100"
          data-testid="stat-value"
        >
          {value}
        </p>
      </div>
    </div>
  </Card>
);
