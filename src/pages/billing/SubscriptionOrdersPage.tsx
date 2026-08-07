import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Ban, Eye, ReceiptText } from 'lucide-react';
import { toast } from 'sonner';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { DataToolbar } from '@/shared/ui/DataToolbar/DataToolbar';
import { SegmentedControl } from '@/shared/ui/SegmentedControl/SegmentedControl';
import { Pagination } from '@/shared/ui/Pagination/Pagination';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { DataTable, RowActionButton, type DataTableColumn } from '@/shared/ui/DataTable/DataTable';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatCurrency, formatDateTime } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { SubscriptionStatusBadge } from '@/features/billing/ui/SubscriptionStatusBadge';
import {
  useCancelSubscriptionOrder,
  useSubscriptionOrdersQuery,
} from '@/features/billing/hooks/useBilling';
import { useIsTenantAdmin } from '@/shared/lib/auth/useIsTenantAdmin';
import type {
  SubscriptionOrderDto,
  SubscriptionOrderStatus,
} from '@/features/billing/model/billing.types';

type StatusBucket = 'all' | SubscriptionOrderStatus;

const STATUS_OPTIONS: SubscriptionOrderStatus[] = [
  'Draft',
  'PendingPayment',
  'Paid',
  'Failed',
  'Cancelled',
  'Expired',
];

const PAGE_SIZE = 20;

export const SubscriptionOrdersPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const navigate = useNavigate();
  const confirm = useConfirm();
  const isAdmin = useIsTenantAdmin();

  const [page, setPage] = useState(1);
  const [bucket, setBucket] = useState<StatusBucket>('all');

  const queryParams = useMemo(
    () => ({
      page,
      pageSize: PAGE_SIZE,
      status: bucket === 'all' ? undefined : bucket,
    }),
    [page, bucket],
  );
  const ordersQuery = useSubscriptionOrdersQuery(queryParams);
  const cancel = useCancelSubscriptionOrder();

  const paged = ordersQuery.data?.data;
  const rows = paged?.items ?? [];
  const total = paged?.total ?? 0;

  const handleCancel = async (order: SubscriptionOrderDto) => {
    const ok = await confirm({
      title: t('billing.cancel.confirmTitle'),
      message: t('billing.cancel.confirmMessage', { number: order.orderNumber }),
      confirmLabel: t('billing.cancel.confirm'),
      cancelLabel: t('common.cancel', { defaultValue: 'Cancel' }),
      tone: 'danger',
    });
    if (!ok) return;
    cancel.mutate(
      { id: order.id },
      {
        onSuccess: () => toast.success(t('billing.toast.cancelled')),
        onError: (err) => toastApiError(err, t('billing.toast.failed')),
      },
    );
  };

  const columns: DataTableColumn<SubscriptionOrderDto>[] = useMemo(
    () => [
      {
        key: 'orderNumber',
        label: t('billing.order.col.orderNumber'),
        sortable: true,
        sortValue: (r) => r.orderNumber,
        cell: (r) => (
          <span className="font-medium text-slate-900 dark:text-slate-100">{r.orderNumber}</span>
        ),
      },
      {
        key: 'createdAt',
        label: t('billing.order.col.createdAt'),
        sortable: true,
        sortValue: (r) => r.createdAtUtc,
        cell: (r) => (
          <span className="text-[11px] text-slate-600 dark:text-slate-300">
            {formatDateTime(r.createdAtUtc, locale)}
          </span>
        ),
      },
      {
        key: 'status',
        label: t('billing.order.col.status'),
        cell: (r) => <SubscriptionStatusBadge status={r.status} />,
      },
      {
        key: 'items',
        label: t('billing.order.col.itemsCount'),
        align: 'right',
        cell: (r) => (
          <span className="tabular-nums text-slate-700 dark:text-slate-300">{r.items.length}</span>
        ),
      },
      {
        key: 'total',
        label: t('billing.order.col.total'),
        align: 'right',
        sortable: true,
        sortValue: (r) => r.totalAmount,
        cell: (r) => (
          <span className="font-semibold tabular-nums text-slate-900 dark:text-slate-100">
            {formatCurrency(r.totalAmount, locale, r.currency)}
          </span>
        ),
      },
    ],
    [locale, t],
  );

  return (
    <div className="space-y-4 p-4">
      <PageHeader
        icon={<ReceiptText size={20} />}
        eyebrow={t('billing.eyebrow')}
        title={t('billing.orders.title')}
        subtitle={t('billing.orders.subtitle')}
        tone="indigo"
        crumbs={[
          { label: t('billing.crumbs.billing'), to: '/dashboard/billing' },
          { label: t('billing.crumbs.orders') },
        ]}
      />

      <DataToolbar
        leading={
          <SegmentedControl<StatusBucket>
            value={bucket}
            onChange={(v) => {
              setBucket(v);
              setPage(1);
            }}
            ariaLabel={t('billing.orders.statusFilter')}
            options={[
              { value: 'all', label: t('billing.orders.bucket.all') },
              ...STATUS_OPTIONS.map((s) => ({
                value: s,
                label: t(`billing.order.status.${s}`),
              })),
            ]}
            size="sm"
          />
        }
        resultCount={{ count: total, label: t('billing.orders.count') }}
      />

      {ordersQuery.isError ? (
        <QueryError
          onRetry={() => ordersQuery.refetch()}
          isRetrying={ordersQuery.isFetching}
          title={t('billing.errors.ordersTitle')}
        />
      ) : (
        <DataTable<SubscriptionOrderDto>
          rows={rows}
          columns={columns}
          getRowId={(r) => r.id}
          isLoading={ordersQuery.isPending}
          onRowClick={(r) => navigate(`/dashboard/billing/orders/${r.id}`)}
          emptyIcon={<ReceiptText size={22} />}
          emptyTitle={t('billing.orders.emptyTitle')}
          emptyDescription={t('billing.orders.emptyDescription')}
          rowActions={(r) => (
            <>
              <RowActionButton
                icon={<Eye size={13} />}
                label={t('billing.orders.view')}
                onClick={() => navigate(`/dashboard/billing/orders/${r.id}`)}
              />
              {isAdmin && (r.status === 'Draft' || r.status === 'PendingPayment') && (
                <RowActionButton
                  icon={<Ban size={13} />}
                  label={t('billing.cancel.action')}
                  tone="danger"
                  onClick={() => handleCancel(r)}
                />
              )}
            </>
          )}
        />
      )}

      <Pagination
        page={page}
        pageSize={PAGE_SIZE}
        total={total}
        onPageChange={setPage}
        itemLabel={t('billing.orders.count')}
      />
    </div>
  );
};

export default SubscriptionOrdersPage;
