import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  AlertTriangle,
  CalendarClock,
  CircleDollarSign,
  Download,
  Plus,
  ShoppingCart,
  Truck,
} from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { downloadCsv } from '@/shared/lib/exportCsv';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { StatStrip, type StatStripItem } from '@/shared/ui/StatStrip/StatStrip';
import { DataToolbar } from '@/shared/ui/DataToolbar/DataToolbar';
import { FilterChip } from '@/shared/ui/FilterChip/FilterChip';
import { SegmentedControl } from '@/shared/ui/SegmentedControl/SegmentedControl';
import { CollapsibleSection } from '@/shared/ui/CollapsibleSection/CollapsibleSection';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { Pagination } from '@/shared/ui/Pagination/Pagination';
import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue';
import { OrderDetailPanel } from '@/features/orders/ui/OrderDetailPanel';
import { OrderInlineCard } from '@/features/orders/ui/OrderInlineCard';
import { OrderFormModal } from '@/features/orders/ui/OrderFormModal';
import { OrderList } from '@/features/orders/ui/OrderList';
import {
  useAllocateOrder,
  useApproveOrder,
  useCancelOrder,
  useCloseOrder,
  useDeleteOrder,
  useDeliverOrder,
  useOrderQuery,
  useOrdersQuery,
  useSubmitOrder,
} from '@/features/orders/hooks/useOrderQueries';
import { useGenerateInvoiceFromOrder } from '@/features/invoices/hooks/useInvoiceQueries';
import type { OrderStatus, OrderSummary } from '@/features/orders/model/order.types';

const PAGE_SIZE = 10;

type StatusBucket = 'all' | 'open' | 'fulfilled' | 'closed' | 'cancelled';

const matchesBucket = (status: OrderStatus, bucket: StatusBucket): boolean => {
  switch (bucket) {
    case 'all':
      return true;
    case 'open':
      return [
        'Draft',
        'Submitted',
        'Approved',
        'Confirmed',
        'Allocated',
        'Picking',
        'Packed',
      ].includes(status);
    case 'fulfilled':
      return ['PartiallyShipped', 'Shipped', 'Delivered'].includes(status);
    case 'closed':
      return status === 'Closed';
    case 'cancelled':
      return status === 'Cancelled' || status === 'Returned';
    default:
      return true;
  }
};

const exportOrdersCsv = (rows: OrderSummary[]) =>
  downloadCsv({
    filename: 'orders',
    rows,
    columns: [
      { header: 'OrderNumber', value: (o) => o.orderNumber },
      { header: 'Customer', value: (o) => o.customerName },
      { header: 'OrderDate', value: (o) => o.orderDate },
      { header: 'Status', value: (o) => o.status },
      { header: 'Currency', value: (o) => o.currency },
      { header: 'Total', value: (o) => o.total },
    ],
  });

export const OrdersPage = () => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE);
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebouncedValue(search, 300);
  const [statusBucket, setStatusBucket] = useState<StatusBucket>('all');
  const [highValueOnly, setHighValueOnly] = useState(false);
  const [draftOnly, setDraftOnly] = useState(false);

  const [editingId, setEditingId] = useState<string | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [searchParams, setSearchParams] = useSearchParams();
  const focusFromUrl = searchParams.get('focus') ?? searchParams.get('selected');
  const [selectedId, setSelectedId] = useState<string | null>(focusFromUrl);
  const [panelOpen, setPanelOpen] = useState<boolean>(!!focusFromUrl);

  useEffect(() => {
    if (focusFromUrl) {
      const next = new URLSearchParams(searchParams);
      next.delete('focus');
      next.delete('selected');
      setSearchParams(next, { replace: true });
    }
  }, [focusFromUrl, searchParams, setSearchParams]);

  const queryParams = useMemo(
    () => ({
      page,
      pageSize,
      search: debouncedSearch.trim() || undefined,
    }),
    [page, pageSize, debouncedSearch],
  );

  const ordersQuery = useOrdersQuery(queryParams);
  const editingQuery = useOrderQuery(editingId);
  const deleteMutation = useDeleteOrder();
  const generateInvoiceMutation = useGenerateInvoiceFromOrder();
  const confirm = useConfirm();

  const submitOrder = useSubmitOrder();
  const approveOrder = useApproveOrder();
  const allocateOrder = useAllocateOrder();
  const deliverOrder = useDeliverOrder();
  const closeOrder = useCloseOrder();
  const cancelOrder = useCancelOrder();
  const [statusBusyId, setStatusBusyId] = useState<string | null>(null);

  const handleStatusTransition = async (order: OrderSummary, action: string) => {
    setStatusBusyId(order.id);
    try {
      if (action === 'submit') await submitOrder.mutateAsync(order.id);
      else if (action === 'approve') await approveOrder.mutateAsync({ id: order.id });
      else if (action === 'allocate') await allocateOrder.mutateAsync({ id: order.id });
      else if (action === 'deliver') await deliverOrder.mutateAsync({ id: order.id });
      else if (action === 'close') await closeOrder.mutateAsync(order.id);
      else if (action === 'cancel') await cancelOrder.mutateAsync({ id: order.id, reason: null });
      toast.success(t(`orders.actions.${action}` as never));
    } catch (err) {
      toastApiError(err, t('auth.common.unexpectedError'));
    } finally {
      setStatusBusyId(null);
    }
  };

  const result = ordersQuery.data?.data;
  const orders = useMemo(() => result?.items ?? [], [result?.items]);
  const total = result?.total ?? 0;

  const stats = useMemo(() => {
    const bucketCounts: Record<StatusBucket, number> = {
      all: orders.length,
      open: 0,
      fulfilled: 0,
      closed: 0,
      cancelled: 0,
    };
    let pageTotal = 0;
    let draftCount = 0;
    let openTotal = 0;
    orders.forEach((o) => {
      pageTotal += o.total;
      (['open', 'fulfilled', 'closed', 'cancelled'] as StatusBucket[]).forEach((b) => {
        if (matchesBucket(o.status, b)) bucketCounts[b] += 1;
      });
      if (o.status === 'Draft') draftCount += 1;
      if (matchesBucket(o.status, 'open')) openTotal += o.total;
    });
    const avgValue = orders.length > 0 ? pageTotal / orders.length : 0;
    return { bucketCounts, pageTotal, draftCount, openTotal, avgValue };
  }, [orders]);

  const filteredOrders = useMemo(() => {
    return orders.filter((o) => {
      if (!matchesBucket(o.status, statusBucket)) return false;
      if (draftOnly && o.status !== 'Draft') return false;
      if (highValueOnly && o.total < stats.avgValue * 1.5) return false;
      return true;
    });
  }, [orders, statusBucket, draftOnly, highValueOnly, stats.avgValue]);

  const hasActiveFilters =
    statusBucket !== 'all' || draftOnly || highValueOnly || debouncedSearch.trim() !== '';

  const clearFilters = () => {
    setSearch('');
    setStatusBucket('all');
    setDraftOnly(false);
    setHighValueOnly(false);
    setPage(1);
  };

  const handleCreate = () => {
    setEditingId(null);
    setModalOpen(true);
  };

  const handleEdit = (order: OrderSummary) => {
    setEditingId(order.id);
    setModalOpen(true);
  };

  const handleDelete = async (order: OrderSummary) => {
    const confirmed = await confirm({
      title: t('common.confirmDelete'),
      message: t('orders.confirmDelete', { number: order.orderNumber }),
      confirmLabel: t('common.delete'),
      tone: 'danger',
    });
    if (!confirmed) return;

    deleteMutation.mutate(order.id, {
      onSuccess: (response) => {
        if (response.isSuccess) {
          toast.success(t('orders.toast.deleted'));
          return;
        }
        toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
      },
      onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
    });
  };

  const handleGenerateInvoice = async (order: OrderSummary) => {
    const confirmed = await confirm({
      title: t('orders.actions.generateInvoice'),
      message: t('orders.confirmGenerateInvoice', { number: order.orderNumber }),
      confirmLabel: t('common.confirm'),
    });
    if (!confirmed) return;

    generateInvoiceMutation.mutate(
      { orderId: order.id },
      {
        onSuccess: (response) => {
          if (response.isSuccess) {
            toast.success(t('orders.toast.invoiceGenerated'));
            return;
          }
          toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
        },
        onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
      },
    );
  };

  const editingOrder = editingId ? (editingQuery.data?.data ?? null) : null;

  const fmtCurrency = (value: number, currency = 'TRY') => {
    try {
      return new Intl.NumberFormat(locale, {
        style: 'currency',
        currency,
        maximumFractionDigits: 0,
      }).format(value);
    } catch {
      return `${value.toFixed(0)} ${currency}`;
    }
  };

  const statItems: StatStripItem[] = [
    {
      id: 'total',
      label: t('orders.stats.total', { defaultValue: 'Orders (page)' }),
      value: orders.length,
      format: (v) => Math.round(v).toLocaleString(locale),
      icon: <ShoppingCart size={14} />,
      sub: t('orders.stats.totalHint', {
        defaultValue: '{{count}} of {{all}}',
        count: orders.length,
        all: total,
      }),
      tone: 'indigo',
    },
    {
      id: 'pageVolume',
      label: t('orders.stats.pageVolume', { defaultValue: 'Page volume' }),
      value: stats.pageTotal,
      format: (v) => fmtCurrency(v),
      icon: <CircleDollarSign size={14} />,
      sub: `${t('orders.stats.avg', { defaultValue: 'Avg' })}: ${fmtCurrency(stats.avgValue)}`,
      tone: 'violet',
    },
    {
      id: 'open',
      label: t('orders.stats.open', { defaultValue: 'Open pipeline' }),
      value: stats.bucketCounts.open,
      format: (v) => Math.round(v).toLocaleString(locale),
      icon: <Truck size={14} />,
      sub: fmtCurrency(stats.openTotal),
      tone: 'amber',
      onClick: () => setStatusBucket('open'),
    },
    {
      id: 'drafts',
      label: t('orders.stats.drafts', { defaultValue: 'Drafts pending' }),
      value: stats.draftCount,
      format: (v) => Math.round(v).toLocaleString(locale),
      icon: <AlertTriangle size={14} />,
      sub: t('orders.stats.draftsHint', { defaultValue: 'Submit or discard' }),
      tone: stats.draftCount > 0 ? 'rose' : 'slate',
      onClick: () => setDraftOnly(true),
    },
  ];

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <PageHeader
        icon={<ShoppingCart size={20} />}
        eyebrow={t('orders.eyebrow', { defaultValue: 'Sales · Fulfillment' })}
        title={t('orders.title')}
        subtitle={t('orders.subtitle')}
        crumbs={[
          { label: t('navigation.dashboard', { defaultValue: 'Dashboard' }), to: '/dashboard' },
          { label: t('orders.title') },
        ]}
        tone="violet"
        actions={
          <>
            <button
              type="button"
              onClick={() => exportOrdersCsv(filteredOrders)}
              disabled={filteredOrders.length === 0}
              className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              <Download size={13} />
              {t('common.exportCsv', { defaultValue: 'Export CSV' })}
            </button>
            <button
              type="button"
              onClick={handleCreate}
              className="inline-flex items-center gap-1.5 rounded-lg bg-gradient-to-r from-primary-600 to-purple-600 px-3 py-1.5 text-xs font-medium text-white shadow-md shadow-primary-500/20 transition hover:-translate-y-px hover:shadow-lg hover:shadow-primary-500/30"
            >
              <Plus size={13} />
              {t('orders.addNew')}
            </button>
          </>
        }
      />

      <CollapsibleSection
        storageKey="orders.stats"
        label={t('Common.SummaryCards', { defaultValue: 'Özet kartları' })}
      >
        <StatStrip items={statItems} />
      </CollapsibleSection>

      <DataToolbar
        search={{
          value: search,
          onChange: (v) => {
            setPage(1);
            setSearch(v);
          },
          placeholder: t('orders.searchPlaceholder'),
        }}
        viewMode={
          <SegmentedControl
            value={statusBucket}
            onChange={(v) => {
              setPage(1);
              setStatusBucket(v);
            }}
            options={[
              {
                value: 'all',
                label: t('orders.filter.all', { defaultValue: 'All' }),
                count: stats.bucketCounts.all,
              },
              {
                value: 'open',
                label: t('orders.filter.open', { defaultValue: 'Open' }),
                count: stats.bucketCounts.open,
              },
              {
                value: 'fulfilled',
                label: t('orders.filter.fulfilled', { defaultValue: 'Fulfilled' }),
                count: stats.bucketCounts.fulfilled,
                icon: <Truck size={11} />,
              },
              {
                value: 'closed',
                label: t('orders.filter.closed', { defaultValue: 'Closed' }),
                count: stats.bucketCounts.closed,
              },
              {
                value: 'cancelled',
                label: t('orders.filter.cancelled', { defaultValue: 'Cancelled' }),
                count: stats.bucketCounts.cancelled,
              },
            ]}
          />
        }
        filters={
          <>
            <FilterChip
              label={t('orders.filter.draftsOnly', { defaultValue: 'Drafts only' })}
              icon={<AlertTriangle size={10} />}
              active={draftOnly}
              count={stats.draftCount}
              tone="rose"
              onClick={() => {
                setPage(1);
                setDraftOnly((v) => !v);
              }}
            />
            <FilterChip
              label={t('orders.filter.highValue', { defaultValue: 'High value (≥1.5× avg)' })}
              icon={<CalendarClock size={10} />}
              active={highValueOnly}
              tone="violet"
              onClick={() => {
                setPage(1);
                setHighValueOnly((v) => !v);
              }}
            />
          </>
        }
        resultCount={{
          count: filteredOrders.length,
          label: t('orders.resultCountLabel', { defaultValue: 'orders' }),
        }}
        hasActiveFilters={hasActiveFilters}
        onClearFilters={clearFilters}
      />

      {ordersQuery.isError ? (
        <QueryError onRetry={() => ordersQuery.refetch()} isRetrying={ordersQuery.isFetching} />
      ) : (
        <OrderList
          orders={filteredOrders}
          isLoading={ordersQuery.isPending}
          selectedId={selectedId}
          onSelect={(o) => {
            setSelectedId((curr) => (curr === o.id ? null : o.id));
            setPanelOpen(false);
          }}
          onOpenDetails={(o) => {
            setSelectedId(o.id);
            setPanelOpen(true);
          }}
          onEdit={handleEdit}
          onDelete={handleDelete}
          onGenerateInvoice={handleGenerateInvoice}
          onCreate={handleCreate}
          onStatusTransition={handleStatusTransition}
          statusBusyId={statusBusyId}
        />
      )}

      {selectedId && !panelOpen && (
        <OrderInlineCard
          orderId={selectedId}
          onClose={() => setSelectedId(null)}
          onOpenPanel={() => setPanelOpen(true)}
        />
      )}

      {!ordersQuery.isError && total > 0 && (
        <div className="rounded-xl border border-slate-200/70 bg-white/60 px-3 py-2 dark:border-slate-800/70 dark:bg-slate-900/40">
          <Pagination
            page={page}
            pageSize={pageSize}
            total={total}
            onPageChange={setPage}
            pageSizeOptions={[10, 25, 50, 100]}
            onPageSizeChange={(size) => {
              setPageSize(size);
              setPage(1);
            }}
          />
        </div>
      )}

      {modalOpen && (
        <OrderFormModal
          open={modalOpen}
          order={editingOrder}
          onClose={() => {
            setModalOpen(false);
            setEditingId(null);
          }}
        />
      )}

      <OrderDetailPanel
        orderId={panelOpen ? selectedId : null}
        onClose={() => setPanelOpen(false)}
        onEdit={(id) => {
          setEditingId(id);
          setModalOpen(true);
          setPanelOpen(false);
        }}
        onGenerateInvoice={(id) => {
          const found = orders.find((o) => o.id === id);
          if (found) handleGenerateInvoice(found);
        }}
      />
    </div>
  );
};
