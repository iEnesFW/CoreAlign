import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Card } from '@/shared/ui/Card';
import { PageHeader } from '@/shared/ui/PageHeader';
import { Button } from '@/shared/ui/Button';
import { Spinner } from '@/shared/ui/Spinner';
import { ApprovalStatusBadge, OrderStatusBadge } from '@/shared/ui/StatusBadge';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useDealerOrders } from '@/features/portal/hooks';

const ORDER_STATUSES = [
  'Draft',
  'Submitted',
  'Confirmed',
  'Approved',
  'Allocated',
  'Picking',
  'Packed',
  'PartiallyShipped',
  'Shipped',
  'Delivered',
  'Closed',
  'Cancelled',
] as const;

const APPROVAL_STATUSES = ['PendingCustomerApproval', 'Approved', 'Rejected'] as const;

export const OrdersPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  const status = searchParams.get('status') ?? '';
  const approvalStatus = searchParams.get('approvalStatus') ?? '';
  const page = Math.max(1, Number(searchParams.get('page') ?? '1'));

  const { data, isLoading } = useDealerOrders({
    status: status || undefined,
    approvalStatus: approvalStatus || undefined,
    page,
    pageSize: 20,
  });

  const setQuery = (next: Record<string, string | null>) => {
    const merged = new URLSearchParams(searchParams);
    Object.entries(next).forEach(([key, value]) => {
      if (value === null || value === '') merged.delete(key);
      else merged.set(key, value);
    });
    setSearchParams(merged, { replace: true });
  };

  return (
    <div className="space-y-6">
      <PageHeader title={t('b2b.orders.title')} subtitle={t('b2b.orders.subtitle')} />

      <Card className="overflow-hidden">
        <div className="flex flex-wrap items-center gap-3 border-b border-slate-100 px-5 py-4 dark:border-slate-800">
          <label className="flex items-center gap-2 text-sm text-slate-500">
            {t('b2b.orders.filterByStatus')}
            <select
              value={status}
              onChange={(e) => setQuery({ status: e.target.value || null, page: '1' })}
              className="h-10 rounded-xl border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-900"
            >
              <option value="">{t('b2b.orders.allStatuses')}</option>
              {ORDER_STATUSES.map((s) => (
                <option key={s} value={s}>
                  {t(`b2b.orderStatus.${s}`, s)}
                </option>
              ))}
            </select>
          </label>
          <label className="flex items-center gap-2 text-sm text-slate-500">
            {t('b2b.orders.filterByApproval')}
            <select
              value={approvalStatus}
              onChange={(e) => setQuery({ approvalStatus: e.target.value || null, page: '1' })}
              className="h-10 rounded-xl border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-900"
            >
              <option value="">{t('b2b.orders.allStatuses')}</option>
              {APPROVAL_STATUSES.map((s) => (
                <option key={s} value={s}>
                  {t(`b2b.approvalStatus.${s}`, s)}
                </option>
              ))}
            </select>
          </label>
        </div>

        {isLoading ? (
          <div className="flex items-center gap-2 px-6 py-10 text-sm text-slate-500">
            <Spinner /> {t('b2b.common.loading')}
          </div>
        ) : (data?.items.length ?? 0) === 0 ? (
          <p className="px-6 py-10 text-sm text-slate-400">{t('b2b.common.noData')}</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-100 text-sm dark:divide-slate-800">
              <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500 dark:bg-slate-900 dark:text-slate-400">
                <tr>
                  <th className="px-6 py-3 font-medium">{t('b2b.orders.number')}</th>
                  <th className="px-6 py-3 font-medium">{t('b2b.orders.customer')}</th>
                  <th className="px-6 py-3 font-medium">{t('b2b.orders.date')}</th>
                  <th className="px-6 py-3 font-medium">{t('b2b.orders.status')}</th>
                  <th className="px-6 py-3 font-medium">{t('b2b.orders.approval')}</th>
                  <th className="px-6 py-3 text-right font-medium">{t('b2b.orders.total')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 bg-white dark:divide-slate-800 dark:bg-slate-950">
                {data!.items.map((o) => (
                  <tr
                    key={o.id}
                    onClick={() => navigate(`/orders/${o.id}`)}
                    className="cursor-pointer transition hover:bg-slate-50 dark:hover:bg-slate-900"
                  >
                    <td className="px-6 py-3 font-medium text-slate-900 dark:text-slate-100">
                      {o.orderNumber}
                    </td>
                    <td className="px-6 py-3 text-slate-600 dark:text-slate-300">
                      {o.customerName}
                    </td>
                    <td className="px-6 py-3 text-slate-600 dark:text-slate-300">
                      {formatDate(o.orderDate, locale)}
                    </td>
                    <td className="px-6 py-3">
                      <OrderStatusBadge status={o.status} />
                    </td>
                    <td className="px-6 py-3">
                      <ApprovalStatusBadge status={o.dealerApprovalStatus} />
                    </td>
                    <td className="px-6 py-3 text-right font-semibold text-slate-900 dark:text-slate-100">
                      {formatCurrency(o.total, locale, o.currency)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {data && data.totalPages > 1 ? (
          <div className="flex items-center justify-between gap-3 border-t border-slate-100 px-6 py-4 text-sm dark:border-slate-800">
            <span className="text-slate-500">
              {t('b2b.common.page')} {data.page} {t('b2b.common.of')} {data.totalPages}
            </span>
            <div className="flex gap-2">
              <Button
                size="sm"
                variant="ghost"
                onClick={() => setQuery({ page: String(Math.max(1, page - 1)) })}
                disabled={page <= 1}
              >
                <ChevronLeft size={14} />
                {t('b2b.common.previous')}
              </Button>
              <Button
                size="sm"
                variant="ghost"
                onClick={() => setQuery({ page: String(Math.min(data.totalPages, page + 1)) })}
                disabled={page >= data.totalPages}
              >
                {t('b2b.common.next')}
                <ChevronRight size={14} />
              </Button>
            </div>
          </div>
        ) : null}
      </Card>
    </div>
  );
};
