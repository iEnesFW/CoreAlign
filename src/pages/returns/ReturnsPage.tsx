import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router-dom';
import { Eye, RotateCcw } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { Pagination } from '@/shared/ui/Pagination/Pagination';
import { DataToolbar } from '@/shared/ui/DataToolbar/DataToolbar';
import { Badge, type BadgeVariant } from '@/shared/ui/Badge/Badge';
import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useReturnRequestsQuery } from '@/features/returns/hooks/useReturnQueries';
import {
  RETURN_REQUEST_STATUSES,
  type ReturnRequestStatus,
  type ReturnRequestSummary,
} from '@/features/returns/model/return.types';

const statusVariant: Record<ReturnRequestStatus, BadgeVariant> = {
  Requested: 'default',
  Approved: 'default',
  Rejected: 'error',
  Received: 'warning',
  CreditNoted: 'success',
  Refunded: 'success',
  Cancelled: 'neutral',
};

export const ReturnsPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const [params, setParams] = useSearchParams();

  const page = Number(params.get('page') ?? '1');
  const pageSize = Number(params.get('pageSize') ?? '20');
  const search = params.get('search') ?? '';
  const status = (params.get('status') as ReturnRequestStatus | null) ?? undefined;

  const [searchInput, setSearchInput] = useState(search);
  const debouncedSearch = useDebouncedValue(searchInput, 300);

  const query = useReturnRequestsQuery({
    page,
    pageSize,
    search: debouncedSearch || undefined,
    status: status ?? undefined,
  });

  const items = useMemo(() => query.data?.data?.items ?? [], [query.data]);
  const total = query.data?.data?.total ?? 0;

  const updateParam = (key: string, value: string | null) => {
    const next = new URLSearchParams(params);
    if (value === null || value === '') next.delete(key);
    else next.set(key, value);
    next.set('page', '1');
    setParams(next, { replace: true });
  };

  const goToPage = (next: number) => {
    const updated = new URLSearchParams(params);
    updated.set('page', String(next));
    setParams(updated, { replace: true });
  };

  return (
    <div className="flex flex-col gap-4 p-4">
      <PageHeader
        title={t('Returns.title')}
        subtitle={t('Returns.subtitle')}
        icon={<RotateCcw size={18} />}
        tone="rose"
      />

      <DataToolbar
        search={{
          value: searchInput,
          placeholder: t('Returns.searchPlaceholder'),
          onChange: (v) => {
            setSearchInput(v);
            updateParam('search', v);
          },
        }}
        filters={
          <select
            value={status ?? ''}
            onChange={(e) => updateParam('status', e.target.value || null)}
            className="rounded border border-slate-300 px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900"
            aria-label={t('Returns.filters.status')}
          >
            <option value="">{t('Returns.filters.allStatuses')}</option>
            {RETURN_REQUEST_STATUSES.map((s) => (
              <option key={s} value={s}>
                {t(`Returns.status.${s}`)}
              </option>
            ))}
          </select>
        }
      />

      {query.isError && <QueryError onRetry={() => query.refetch()} />}

      <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
        <table className="w-full text-left text-xs">
          <thead className="bg-slate-50 dark:bg-slate-800/50">
            <tr>
              <th className="px-3 py-2 font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('Returns.fields.number')}
              </th>
              <th className="px-3 py-2 font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('Returns.fields.order')}
              </th>
              <th className="px-3 py-2 font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('Returns.fields.customer')}
              </th>
              <th className="px-3 py-2 font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('Returns.fields.status')}
              </th>
              <th className="px-3 py-2 text-right font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('Returns.fields.total')}
              </th>
              <th className="px-3 py-2 font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('Returns.fields.requestedAt')}
              </th>
              <th className="w-12 px-3 py-2"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
            {items.map((row) => (
              <Row key={row.id} row={row} locale={locale} />
            ))}
            {!query.isLoading && items.length === 0 && (
              <tr>
                <td colSpan={7} className="px-3 py-8 text-center text-slate-500">
                  {t('Returns.empty')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <Pagination page={page} pageSize={pageSize} total={total} onPageChange={goToPage} />
    </div>
  );
};

interface RowProps {
  row: ReturnRequestSummary;
  locale: string;
}

const Row = ({ row, locale }: RowProps) => {
  const { t } = useTranslation();
  return (
    <tr className="hover:bg-slate-50/60 dark:hover:bg-slate-800/40">
      <td className="px-3 py-2">
        <Link
          to={`/dashboard/returns/${row.id}`}
          className="font-mono text-primary-600 hover:underline dark:text-primary-400"
        >
          {row.returnNumber}
        </Link>
      </td>
      <td className="px-3 py-2 font-mono text-slate-700 dark:text-slate-300">{row.orderNumber}</td>
      <td className="px-3 py-2">{row.customerName}</td>
      <td className="px-3 py-2">
        <Badge variant={statusVariant[row.status]}>{t(`Returns.status.${row.status}`)}</Badge>
      </td>
      <td className="px-3 py-2 text-right tabular-nums">
        {formatCurrency(row.total, locale, row.currency)}
      </td>
      <td className="px-3 py-2 text-slate-600 dark:text-slate-400">
        {formatDate(row.requestedAtUtc, locale)}
      </td>
      <td className="px-3 py-2 text-right">
        <Link
          to={`/dashboard/returns/${row.id}`}
          className="text-slate-400 hover:text-primary-500"
          aria-label={t('Returns.actions.view')}
        >
          <Eye size={14} />
        </Link>
      </td>
    </tr>
  );
};

export default ReturnsPage;
