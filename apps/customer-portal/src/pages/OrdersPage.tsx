import { ChevronLeft, ChevronRight, PlusCircle, Search } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Card } from '@/shared/ui/Card';
import { Input } from '@/shared/ui/Input';
import { PageHeader } from '@/shared/ui/PageHeader';
import { Button } from '@/shared/ui/Button';
import { Spinner } from '@/shared/ui/Spinner';
import { OrderStatusBadge } from '@/shared/ui/StatusBadge';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { usePortalOrders } from '@/features/portal/hooks';

const ORDER_STATUSES = [
  'Draft',
  'Submitted',
  'Approved',
  'Confirmed',
  'Allocated',
  'Picking',
  'Packed',
  'PartiallyShipped',
  'Shipped',
  'Delivered',
  'Closed',
  'Returned',
  'Cancelled',
] as const;

export const OrdersPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  const status = searchParams.get('status') ?? '';
  const page = Math.max(1, Number(searchParams.get('page') ?? '1'));
  const [search, setSearch] = useState('');

  const { data, isLoading } = usePortalOrders({
    status: status || undefined,
    page,
    pageSize: 20,
  });

  const filtered = useMemo(() => {
    if (!data) return [];
    const needle = search.trim().toLowerCase();
    if (!needle) return data.items;
    return data.items.filter((o) => o.orderNumber.toLowerCase().includes(needle));
  }, [data, search]);

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
      <PageHeader
        title={t('orders.title')}
        action={
          <Button onClick={() => navigate('/orders/new')}>
            <PlusCircle size={16} />
            {t('orders.create.newOrder')}
          </Button>
        }
      />

      <Card className="overflow-hidden">
        <div className="flex flex-wrap items-center gap-3 border-b border-slate-100 px-5 py-4 dark:border-slate-800">
          <div className="relative w-full max-w-xs">
            <Search
              size={14}
              className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"
            />
            <Input
              type="search"
              placeholder={t('common.search')}
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              className="pl-9"
            />
          </div>
          <label className="ml-auto flex items-center gap-2 text-sm text-slate-500">
            {t('orders.filterByStatus')}
            <select
              value={status}
              onChange={(event) => setQuery({ status: event.target.value || null, page: '1' })}
              className="h-10 rounded-xl border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-900"
            >
              <option value="">{t('orders.allStatuses')}</option>
              {ORDER_STATUSES.map((s) => (
                <option key={s} value={s}>
                  {t(`orderStatus.${s}`, s)}
                </option>
              ))}
            </select>
          </label>
        </div>

        {isLoading ? (
          <div className="flex items-center gap-2 px-6 py-10 text-sm text-slate-500">
            <Spinner /> {t('common.loading')}
          </div>
        ) : filtered.length === 0 ? (
          <p className="px-6 py-10 text-sm text-slate-400">{t('common.noData')}</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-100 text-sm dark:divide-slate-800">
              <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500 dark:bg-slate-900 dark:text-slate-400">
                <tr>
                  <th scope="col" className="px-6 py-3 font-medium">
                    {t('orders.number')}
                  </th>
                  <th scope="col" className="px-6 py-3 font-medium">
                    {t('orders.date')}
                  </th>
                  <th scope="col" className="px-6 py-3 font-medium">
                    {t('orders.status')}
                  </th>
                  <th scope="col" className="px-6 py-3 text-right font-medium">
                    {t('orders.total')}
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 bg-white dark:divide-slate-800 dark:bg-slate-950">
                {filtered.map((o) => (
                  <tr
                    key={o.id}
                    onClick={() => navigate(`/orders/${o.id}`)}
                    className="cursor-pointer transition hover:bg-slate-50 dark:hover:bg-slate-900"
                  >
                    <td className="px-6 py-3 font-medium text-slate-900 dark:text-slate-100">
                      {o.orderNumber}
                    </td>
                    <td className="px-6 py-3 text-slate-600 dark:text-slate-300">
                      {formatDate(o.orderDate, locale)}
                    </td>
                    <td className="px-6 py-3">
                      <OrderStatusBadge status={o.status} />
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
              {t('common.page')} {data.page} {t('common.of')} {data.totalPages}
            </span>
            <div className="flex gap-2">
              <Button
                size="sm"
                variant="ghost"
                onClick={() => setQuery({ page: String(Math.max(1, page - 1)) })}
                disabled={page <= 1}
              >
                <ChevronLeft size={14} />
                {t('common.previous')}
              </Button>
              <Button
                size="sm"
                variant="ghost"
                onClick={() => setQuery({ page: String(Math.min(data.totalPages, page + 1)) })}
                disabled={page >= data.totalPages}
              >
                {t('common.next')}
                <ChevronRight size={14} />
              </Button>
            </div>
          </div>
        ) : null}
      </Card>
    </div>
  );
};
