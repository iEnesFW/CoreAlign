import { CalendarCheck, Hourglass, Package, PlusCircle, Users } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { Button } from '@/shared/ui/Button';
import { Card, CardBody, CardHeader } from '@/shared/ui/Card';
import { PageHeader } from '@/shared/ui/PageHeader';
import { Spinner } from '@/shared/ui/Spinner';
import { ApprovalStatusBadge, OrderStatusBadge } from '@/shared/ui/StatusBadge';
import { cn } from '@/shared/lib/cn';
import { formatCurrency, formatDate, formatNumber } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useDealerDashboard } from '@/features/portal/hooks';

export const DashboardPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const { data, isLoading } = useDealerDashboard();

  if (isLoading || !data) {
    return (
      <div className="flex items-center gap-2 text-sm text-slate-500">
        <Spinner /> {t('b2b.common.loading')}
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <PageHeader
        title={t('b2b.dashboard.title')}
        subtitle={t('b2b.dashboard.subtitle')}
        action={
          <Link to="/orders/new">
            <Button>
              <PlusCircle size={16} />
              {t('b2b.dashboard.newOrder')}
            </Button>
          </Link>
        }
      />

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard
          label={t('b2b.dashboard.allowedCustomers')}
          value={formatNumber(data.allowedCustomerCount, locale, 0)}
          icon={<Users size={18} />}
          tone="from-amber-500 to-rose-500"
        />
        <StatCard
          label={t('b2b.dashboard.openOrders')}
          value={formatNumber(data.totalOpenOrders, locale, 0)}
          icon={<Package size={18} />}
          tone="from-sky-500 to-indigo-500"
        />
        <StatCard
          label={t('b2b.dashboard.pendingApprovals')}
          value={formatNumber(data.pendingApprovalCount, locale, 0)}
          icon={<Hourglass size={18} />}
          tone="from-emerald-500 to-teal-500"
        />
        <StatCard
          label={t('b2b.dashboard.completedThisMonth')}
          value={formatNumber(data.ordersCompletedThisMonth, locale, 0)}
          icon={<CalendarCheck size={18} />}
          tone="from-violet-500 to-purple-500"
        />
      </div>

      <Card>
        <CardHeader
          title={t('b2b.dashboard.recentOrders')}
          action={
            <Link
              to="/orders"
              className="text-xs font-medium text-amber-600 hover:underline dark:text-amber-400"
            >
              {t('b2b.common.viewAll')}
            </Link>
          }
        />
        <CardBody className="px-0 py-0">
          {data.recentOrders.length === 0 ? (
            <p className="px-6 py-6 text-sm text-slate-400">{t('b2b.dashboard.empty')}</p>
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
                      <p className="text-xs text-slate-500">
                        {o.customerName} • {formatDate(o.orderDate, locale)}
                      </p>
                    </div>
                    <div className="flex flex-wrap items-center gap-2">
                      <OrderStatusBadge status={o.status} />
                      <ApprovalStatusBadge status={o.dealerApprovalStatus} />
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
